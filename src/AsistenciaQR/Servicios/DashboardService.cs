using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    public class DashboardService
    {
        private readonly AsistenciaRepository _asistenciaRepository = new();
        private readonly EstudianteRepository _estudianteRepository = new();

        public ResumenAsistencia ObtenerResumen(DateTime fecha, string? seccion)
        {
            return new ResumenAsistencia
            {
                Fecha = fecha,
                TotalActivos = _estudianteRepository.ContarActivos(seccion),
                Presentes = _asistenciaRepository.ContarPresentesDelDia(fecha, seccion),
                Tardanzas = _asistenciaRepository.ContarTardanzasDelDia(fecha, seccion),
                Detalle = _asistenciaRepository.ListarDetalleDelDia(fecha, seccion)
            };
        }

        public List<string> ObtenerSecciones() => _estudianteRepository.ObtenerSeccionesDisponibles();
    }
}
