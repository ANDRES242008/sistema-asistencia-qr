using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Genera los carnets de muchos estudiantes de una vez: crea el QR
    /// a quien todavia no tiene, dibuja cada carnet y los une en un
    /// solo PDF listo para imprimir. A quien ya tiene QR se le respeta
    /// el que tiene, para no invalidar carnets ya entregados.
    /// </summary>
    public class CarnetsEnLoteService
    {
        private readonly QRRepository _qrRepository = new();
        private readonly QRService _qrService = new();
        private readonly CarnetService _carnetService = new();

        /// <param name="progreso">Recibe cuantos carnets van listos.</param>
        /// <returns>Cuantos QR nuevos hubo que generar.</returns>
        public int Generar(IReadOnlyList<Estudiante> estudiantes, string rutaPdf, IProgress<int>? progreso = null)
        {
            var rutasImagenes = new List<string>(estudiantes.Count);
            int qrNuevos = 0;

            foreach (var estudiante in estudiantes)
            {
                var qr = _qrRepository.ObtenerQrActivo(estudiante.EstudianteId);
                if (qr is null || string.IsNullOrEmpty(qr.RutaImagen) || !File.Exists(qr.RutaImagen))
                {
                    if (qr is null) qrNuevos++;
                    qr = qr is null ? _qrService.GenerarQrParaEstudiante(estudiante) : RegenerarImagen(qr);
                }

                rutasImagenes.Add(_carnetService.GenerarImagenCarnet(estudiante, qr));
                progreso?.Report(rutasImagenes.Count);
            }

            CarnetService.ExportarHojaDeCarnets(rutasImagenes, rutaPdf);
            return qrNuevos;
        }

        /// <summary>
        /// El QR existe en la base pero su imagen se borro del disco
        /// (otra computadora, limpieza): se vuelve a dibujar con el
        /// MISMO codigo, asi el carnet impreso antes sigue sirviendo.
        /// </summary>
        private Qr RegenerarImagen(Qr qr)
        {
            qr.RutaImagen = _qrService.DibujarImagen(qr.CodigoQR);
            return qr;
        }
    }
}
