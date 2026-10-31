using System.Drawing;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Maneja la captura de video de la webcam y la deteccion de
    /// codigos QR en tiempo real usando OpenCvSharp4.
    ///
    /// Version 4: el video y la busqueda del QR van separados (el video
    /// nunca se frena), la camara trabaja a 640x480 (la Argom CAM45 se
    /// traba a 1280x720) y la lectura usa dos motores: OpenCV y ZXing.
    /// </summary>
    public class CamaraQRService : IDisposable
    {
        // Se puede ajustar desde la pantalla de Configuracion del Sistema.
        public int SegundosCooldown { get; set; } = 4;

        // BLINDAJE 3: tolerancia a micro-cortes de la camara USB.
        private const int EsperaTrasFalloMs = 100;
        private const int FallosAntesDeReconectar = 30; // ~3 segundos

        // Resolucion pedida a la camara. Se probo 1280x720 y el video se
        // trababa con esta camara; 640x480 queda fluido.
        private const int AnchoDeseado = 640;
        private const int AltoDeseado = 480;

        // La vista previa no necesita 1280 de ancho: se reduce solo para
        // mostrarla. El QR se sigue buscando en la imagen completa.
        private const int AnchoMaximoVista = 640;

        // Los intentos extra (gris con contraste y blanco/negro) solo se
        // hacen cada 3 busquedas sin lectura, para no gastar CPU.
        private const int IntentoExtraCada = 3;

        private readonly QRCodeDetector _detector = new();

        // Segundo motor de lectura. ZXing suele leer mejor QR pequenos,
        // algo borrosos o con poca luz. Solo hay una busqueda a la vez,
        // asi que un solo lector compartido es seguro.
        private readonly ZXing.BarcodeReaderGeneric _lectorZxing = new()
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE },
                TryHarder = true
            }
        };
        private readonly object _bloqueo = new();
        private VideoCapture? _captura;
        private CancellationTokenSource? _tokenCancelacion;
        private int _indiceCamara;

        private string? _ultimoCodigoDetectado;
        private DateTime _ultimaDeteccion = DateTime.MinValue;
        private int _busquedasSinLectura;

        // 0 = libre, 1 = buscando un QR. Solo una busqueda a la vez.
        private int _buscando;

        public event Action<Bitmap>? FrameCapturado;
        public event Action<string>? CodigoDetectado;

        public void Iniciar(int indiceCamara = 0)
        {
            _indiceCamara = indiceCamara;
            _captura = AbrirCamara(indiceCamara);

            if (_captura is null || !_captura.IsOpened())
            {
                _captura?.Dispose();
                _captura = null;
                throw new InvalidOperationException(
                    "No se pudo abrir la camara. Revisa que este conectada por USB, que " +
                    "ninguna otra aplicacion la este usando, y prueba otro indice de camara " +
                    "en la pantalla de Configuracion del Sistema.");
            }

            _tokenCancelacion = new CancellationTokenSource();
            Task.Run(() => CapturarContinuamente(_tokenCancelacion.Token));
        }

        public void Detener()
        {
            lock (_bloqueo)
            {
                _tokenCancelacion?.Cancel();
                _captura?.Release();
                _captura?.Dispose();
                _captura = null;
            }
        }

        private void CapturarContinuamente(CancellationToken token)
        {
            using var frame = new Mat();
            int fallosSeguidos = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var captura = _captura;
                    if (captura is null) break;

                    bool leyo = captura.Read(frame);
                    if (!leyo || frame.Empty())
                    {
                        fallosSeguidos++;
                        if (fallosSeguidos >= FallosAntesDeReconectar)
                        {
                            IntentarReconectar(token);
                            fallosSeguidos = 0;
                        }
                        Thread.Sleep(EsperaTrasFalloMs);
                        continue;
                    }

                    fallosSeguidos = 0;

                    // 1. Video: se muestra en cada imagen, reducido.
                    MostrarVistaPrevia(frame);

                    // 2. QR: si no hay otra busqueda en curso, se lanza una
                    //    aparte con una copia de la imagen. Si ya hay una,
                    //    esta imagen se salta y el video sigue fluido.
                    if (Interlocked.CompareExchange(ref _buscando, 1, 0) == 0)
                    {
                        var copia = frame.Clone();
                        Task.Run(() =>
                        {
                            try
                            {
                                BuscarQr(copia, token);
                            }
                            catch
                            {
                                // Una busqueda fallida nunca detiene la camara.
                            }
                            finally
                            {
                                copia.Dispose();
                                Interlocked.Exchange(ref _buscando, 0);
                            }
                        });
                    }
                }
                catch (Exception)
                {
                    if (token.IsCancellationRequested) break;

                    fallosSeguidos++;
                    if (fallosSeguidos >= FallosAntesDeReconectar)
                    {
                        IntentarReconectar(token);
                        fallosSeguidos = 0;
                    }
                    Thread.Sleep(EsperaTrasFalloMs);
                }
            }
        }

        private void MostrarVistaPrevia(Mat frame)
        {
            if (FrameCapturado is null) return;

            if (frame.Width > AnchoMaximoVista)
            {
                int alto = frame.Height * AnchoMaximoVista / frame.Width;
                using var reducida = new Mat();
                Cv2.Resize(frame, reducida, new OpenCvSharp.Size(AnchoMaximoVista, alto),
                    0, 0, InterpolationFlags.Area);
                using var bitmap = BitmapConverter.ToBitmap(reducida);
                FrameCapturado?.Invoke((Bitmap)bitmap.Clone());
            }
            else
            {
                using var bitmap = BitmapConverter.ToBitmap(frame);
                FrameCapturado?.Invoke((Bitmap)bitmap.Clone());
            }
        }

        private void BuscarQr(Mat imagen, CancellationToken token)
        {
            string texto = IntentarDecodificar(imagen);
            if (string.IsNullOrEmpty(texto) || token.IsCancellationRequested) return;

            bool esElMismoCodigoReciente =
                texto == _ultimoCodigoDetectado &&
                (DateTime.Now - _ultimaDeteccion).TotalSeconds < SegundosCooldown;

            if (esElMismoCodigoReciente) return;

            _ultimoCodigoDetectado = texto;
            _ultimaDeteccion = DateTime.Now;

            CodigoDetectado?.Invoke(texto);
        }

        /// <summary>
        /// Intenta leer el QR en cuatro pasos:
        /// 1. OpenCV con la imagen normal.
        /// 2. ZXing con la imagen en gris.
        /// 3 y 4. Cada pocas busquedas: OpenCV en gris con contraste
        ///    mejorado y en blanco y negro puro.
        /// </summary>
        private string IntentarDecodificar(Mat imagen)
        {
            string texto = string.Empty;
            try
            {
                texto = _detector.DetectAndDecode(imagen, out _);
            }
            catch
            {
                // Si OpenCV falla, ZXing todavia puede leerlo.
            }

            if (!string.IsNullOrEmpty(texto))
            {
                _busquedasSinLectura = 0;
                return texto;
            }

            try
            {
                using var gris = new Mat();
                if (imagen.Channels() == 1)
                    imagen.CopyTo(gris);
                else
                    Cv2.CvtColor(imagen, gris, ColorConversionCodes.BGR2GRAY);

                texto = LeerConZxing(gris);
                if (!string.IsNullOrEmpty(texto))
                {
                    _busquedasSinLectura = 0;
                    return texto;
                }

                _busquedasSinLectura++;
                if (_busquedasSinLectura % IntentoExtraCada != 0) return string.Empty;

                using (var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8)))
                using (var contrastada = new Mat())
                {
                    clahe.Apply(gris, contrastada);
                    texto = _detector.DetectAndDecode(contrastada, out _);
                    if (!string.IsNullOrEmpty(texto)) return texto;
                }

                using var binaria = new Mat();
                Cv2.AdaptiveThreshold(gris, binaria, 255,
                    AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 31, 10);
                texto = _detector.DetectAndDecode(binaria, out _);
                return texto ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Pasa la imagen en gris a ZXing y devuelve el texto del QR,
        /// o vacio si no encontro ninguno.
        /// </summary>
        private string LeerConZxing(Mat gris)
        {
            try
            {
                // Clone garantiza memoria continua para copiar los bytes.
                using var continua = gris.Clone();
                int ancho = continua.Width;
                int alto = continua.Height;
                var bytes = new byte[ancho * alto];
                Marshal.Copy(continua.Data, bytes, 0, bytes.Length);

                var fuente = new ZXing.RGBLuminanceSource(bytes, ancho, alto,
                    ZXing.RGBLuminanceSource.BitmapFormat.Gray8);
                var resultado = _lectorZxing.Decode(fuente);
                return resultado?.Text ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Abre la camara con DirectShow, que en Windows permite pedir
        /// MJPG: el formato comprimido con el que las webcams dan 30
        /// imagenes por segundo a 1280x720. Si DirectShow no abre, usa
        /// el metodo normal.
        /// </summary>
        private static VideoCapture? AbrirCamara(int indice)
        {
            VideoCapture? camara = null;
            try
            {
                camara = new VideoCapture(indice, VideoCaptureAPIs.DSHOW);
                if (!camara.IsOpened())
                {
                    camara.Dispose();
                    camara = new VideoCapture(indice);
                }
            }
            catch
            {
                camara?.Dispose();
                camara = new VideoCapture(indice);
            }

            if (camara.IsOpened()) ConfigurarCamara(camara);
            return camara;
        }

        /// <summary>
        /// Pide MJPG, mas resolucion, buffer minimo y enfoque automatico.
        /// Lo que la camara no soporte, OpenCV lo ignora.
        /// El formato MJPG se pide ANTES que la resolucion.
        /// </summary>
        private static void ConfigurarCamara(VideoCapture camara)
        {
            try { camara.Set(VideoCaptureProperties.FourCC, VideoWriter.FourCC('M', 'J', 'P', 'G')); } catch { }
            try { camara.Set(VideoCaptureProperties.FrameWidth, AnchoDeseado); } catch { }
            try { camara.Set(VideoCaptureProperties.FrameHeight, AltoDeseado); } catch { }
            try { camara.Set(VideoCaptureProperties.BufferSize, 1); } catch { }
            try { camara.Set(VideoCaptureProperties.AutoFocus, 1); } catch { }
        }

        private void IntentarReconectar(CancellationToken token)
        {
            VideoCapture? nueva = null;
            try
            {
                nueva = AbrirCamara(_indiceCamara);
                if (nueva is null || !nueva.IsOpened())
                {
                    nueva?.Dispose();
                    return;
                }

                lock (_bloqueo)
                {
                    if (token.IsCancellationRequested || _captura is null)
                    {
                        nueva.Dispose();
                        return;
                    }

                    var vieja = _captura;
                    _captura = nueva;
                    nueva = null;

                    vieja.Release();
                    vieja.Dispose();
                }
            }
            catch
            {
                nueva?.Dispose();
            }
        }

        public void Dispose()
        {
            Detener();
            _tokenCancelacion?.Dispose();
        }
    }
}
