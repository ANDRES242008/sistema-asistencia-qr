using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ZXing;
using ZXing.Windows.Compatibility;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Maneja la captura de video de la webcam y la deteccion de
    /// codigos QR en tiempo real. OpenCvSharp4 se encarga de abrir
    /// la camara y capturar los frames; ZXing.Net se encarga de
    /// detectar el QR dentro de cada frame (mas tolerante a
    /// desenfoque, angulos y mala iluminacion que el detector nativo
    /// de OpenCV). Corre en un hilo separado para no congelar la
    /// interfaz.
    /// </summary>
    public class CamaraQRService : IDisposable
    {
        // Tiempo minimo antes de volver a aceptar el MISMO codigo,
        // para no procesar 20 veces el mismo QR mientras el
        // estudiante todavia tiene el carnet frente a la camara.
        private const int SegundosCooldown = 4;

        // Lector ZXing configurado para QR solamente.
        private readonly BarcodeReader _lector = new()
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                TryHarder = true
            }
        };

        private VideoCapture? _captura;
        private CancellationTokenSource? _tokenCancelacion;

        private string? _ultimoCodigoDetectado;
        private DateTime _ultimaDeteccion = DateTime.MinValue;

        public event Action<Bitmap>? FrameCapturado;
        public event Action<string>? CodigoDetectado;

        public void Iniciar(int indiceCamara = 0)
        {
            _captura = new VideoCapture(indiceCamara);

            if (!_captura.IsOpened())
            {
                throw new InvalidOperationException(
                    "No se pudo abrir la camara. Revisa que este conectada por USB " +
                    "y que ninguna otra aplicacion (Zoom, Teams, etc.) la este usando.");
            }

            _tokenCancelacion = new CancellationTokenSource();
            Task.Run(() => CapturarContinuamente(_tokenCancelacion.Token));
        }

        public void Detener()
        {
            _tokenCancelacion?.Cancel();
            _captura?.Release();
            _captura?.Dispose();
            _captura = null;
        }

        private void CapturarContinuamente(CancellationToken token)
        {
            using var frame = new Mat();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Se toma una copia local de la referencia: si
                    // Detener() cambia _captura a null justo en este
                    // instante desde otro hilo, este ciclo sigue
                    // trabajando con la copia local sin explotar.
                    var captura = _captura;
                    if (captura is null) break;

                    captura.Read(frame);
                    if (frame.Empty()) continue;

                    // Convertir el frame de OpenCV a Bitmap para
                    // enviarlo a la pantalla Y para que ZXing lo analice.
                    using var bitmap = BitmapConverter.ToBitmap(frame);

                    // Clonar para el hilo de la UI (se muestra en pantalla).
                    FrameCapturado?.Invoke((Bitmap)bitmap.Clone());

                    // ZXing analiza el Bitmap original buscando un QR.
                    var resultadoQr = _lector.Decode(bitmap);
                    if (resultadoQr is null) continue;

                    string textoDetectado = resultadoQr.Text;
                    if (string.IsNullOrEmpty(textoDetectado)) continue;

                    bool esElMismoCodigoReciente =
                        textoDetectado == _ultimoCodigoDetectado &&
                        (DateTime.Now - _ultimaDeteccion).TotalSeconds < SegundosCooldown;

                    if (esElMismoCodigoReciente) continue;

                    _ultimoCodigoDetectado = textoDetectado;
                    _ultimaDeteccion = DateTime.Now;

                    CodigoDetectado?.Invoke(textoDetectado);
                }
                catch (Exception)
                {
                    // Si la camara se libero justo en este instante
                    // (ej. se cerro la ventana del Kiosco a mitad de
                    // una lectura), se detiene el hilo en silencio en
                    // vez de tumbar toda la aplicacion.
                    break;
                }
            }
        }

        public void Dispose()
        {
            Detener();
            _tokenCancelacion?.Dispose();
        }
    }
}
