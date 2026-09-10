using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Maneja la captura de video de la webcam y la deteccion de
    /// codigos QR en tiempo real usando OpenCvSharp4. Corre en un
    /// hilo separado para no congelar la interfaz mientras lee la camara.
    /// </summary>
    public class CamaraQRService : IDisposable
    {
        // Antes era un valor fijo. Ahora se puede ajustar desde la
        // pantalla de Configuracion del Sistema.
        public int SegundosCooldown { get; set; } = 4;

        private readonly QRCodeDetector _detector = new();
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
                    "No se pudo abrir la camara. Revisa que este conectada por USB, que " +
                    "ninguna otra aplicacion la este usando, y prueba otro indice de camara " +
                    "en la pantalla de Configuracion del Sistema.");
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
                    var captura = _captura;
                    if (captura is null) break;

                    captura.Read(frame);
                    if (frame.Empty()) continue;

                    using (var bitmap = BitmapConverter.ToBitmap(frame))
                    {
                        FrameCapturado?.Invoke((Bitmap)bitmap.Clone());
                    }

                    string textoDetectado = _detector.DetectAndDecode(frame, out _);
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
