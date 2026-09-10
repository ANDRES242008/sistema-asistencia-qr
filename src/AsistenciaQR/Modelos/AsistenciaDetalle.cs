namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Una fila de asistencia combinada con los datos del estudiante.
    /// EstadoPuntualidad queda vacio para registros anteriores a esta
    /// funcion (no se reinterpretan retroactivamente).
    /// </summary>
    public class AsistenciaDetalle
    {
        public string NIE { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string? Observacion { get; set; }
        public string? EstadoPuntualidad { get; set; }
    }
}
