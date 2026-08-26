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
        // Tiempo minimo antes de volver a aceptar el MISMO codigo,
        // para no procesar 20 veces el mismo QR mientras el
        // estudiante todavia tiene el carnet frente a la camara.
        private const int SegundosCooldown = 4;

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

            while (!token.IsCancellationRequested && _captura is not null)
            {
                _captura.Read(frame);
                if (frame.Empty()) continue;

                // Muestra el frame en pantalla, tenga QR o no.
                // OJO: se clona el bitmap porque el original se libera
                // apenas termina este bloque "using" - sin el clon,
                // la imagen llegaria danada o vacia a la pantalla.
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
        }

        public void Dispose()
        {
            Detener();
            _tokenCancelacion?.Dispose();
        }
    }
}
