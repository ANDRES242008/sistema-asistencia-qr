namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Una fila de asistencia combinada con los datos del estudiante.
    /// Se usa tanto en el Dashboard (un solo dia) como en Reportes
    /// (rango de fechas), por eso incluye Fecha ademas de Hora.
    /// </summary>
    public class AsistenciaDetalle
    {
        public string NIE { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
    }
}
