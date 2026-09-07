using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Logica de negocio del Dashboard: junta el conteo de estudiantes
    /// activos, el conteo de presentes, y el detalle del dia, en un
    /// solo paquete listo para mostrar en pantalla.
    /// </summary>
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
                Detalle = _asistenciaRepository.ListarDetalleDelDia(fecha, seccion)
            };
        }

        public List<string> ObtenerSecciones() => _estudianteRepository.ObtenerSeccionesDisponibles();
    }
}
