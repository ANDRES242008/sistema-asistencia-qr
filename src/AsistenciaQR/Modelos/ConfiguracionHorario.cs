namespace AsistenciaQR.Modelos
{
    public class ConfiguracionHorario
    {
        public int ConfiguracionHorarioId { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public int MinutosTolerancia { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}
