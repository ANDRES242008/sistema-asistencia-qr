namespace AsistenciaQR.Modelos
{
    public class Asistencia
    {
        public int AsistenciaId { get; set; }
        public int EstudianteId { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string? Observacion { get; set; }
    }
}
