using QRCoder;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Logica de negocio para generar codigos QR.
    /// Si el estudiante ya tenia un QR activo, lo inactiva
    /// antes de crear el nuevo (ej. carnet perdido).
    /// </summary>
    public class QRService
    {
        private readonly QRRepository _qrRepository = new();
        private readonly string _carpetaQR;

        public QRService()
        {
            // Carpeta fuera del repositorio de Git, para no subir
            // imagenes de QR de estudiantes reales a un repo publico.
            _carpetaQR = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AsistenciaQR", "QR");

            Directory.CreateDirectory(_carpetaQR);
        }

        public Qr GenerarQrParaEstudiante(Estudiante estudiante)
        {
            var qrActivo = _qrRepository.ObtenerQrActivo(estudiante.EstudianteId);
            if (qrActivo is not null)
            {
                // Regenerar invalida el carnet anterior: queda en la
                // bitacora por si alguien reclama que su carnet no sirve.
                _qrRepository.InactivarQr(qrActivo.QRId);
                BitacoraService.Registrar(BitacoraService.QrRegenerado,
                    $"{BitacoraService.DescribirEstudiante(estudiante)} · el carnet anterior ya no funciona");
            }

            // El contenido codificado en el QR es un identificador unico
            // (GUID), no el NIE directamente, para no exponer el NIE
            // en texto plano si alguien mas escanea o fotografia el carnet.
            string codigoUnico = Guid.NewGuid().ToString("N");

            string nombreArchivo = $"{estudiante.NIE}_{codigoUnico}.png";
            string rutaCompleta = Path.Combine(_carpetaQR, nombreArchivo);

            GenerarImagenQr(codigoUnico, rutaCompleta);

            var nuevoQr = new Qr
            {
                EstudianteId = estudiante.EstudianteId,
                CodigoQR = codigoUnico,
                RutaImagen = rutaCompleta,
                FechaGeneracion = DateTime.Now,
                Activo = true
            };

            _qrRepository.Insertar(nuevoQr);

            return nuevoQr;
        }

        /// <summary>
        /// Vuelve a dibujar la imagen de un codigo que ya existe (sin
        /// cambiarlo) y devuelve la ruta. No toca la base de datos.
        /// </summary>
        public string DibujarImagen(string codigoQr)
        {
            string ruta = Path.Combine(_carpetaQR, $"{codigoQr}.png");
            GenerarImagenQr(codigoQr, ruta);
            return ruta;
        }

        private static void GenerarImagenQr(string contenido, string rutaDestino)
        {
            using var generador = new QRCodeGenerator();
            using var datosQr = generador.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
            using var qrPng = new PngByteQRCode(datosQr);

            byte[] bytesImagen = qrPng.GetGraphic(20);
            File.WriteAllBytes(rutaDestino, bytesImagen);
        }
    }
}
