namespace AsistenciaQR.Modelos
{
    public enum TipoAviso
    {
        Tardanza,
        Ausencia
    }

    /// <summary>Un aviso por mandar (o ya mandado) al encargado de un estudiante.</summary>
    public class AvisoAsistencia
    {
        public Estudiante Estudiante { get; init; } = new();
        public TipoAviso Tipo { get; init; }
        public DateTime Fecha { get; init; }
        public TimeSpan? HoraLlegada { get; init; }
    }

    /// <summary>Fila del historial de avisos.</summary>
    public class RegistroAviso
    {
        public DateTime Fecha { get; init; }
        public string Estudiante { get; init; } = string.Empty;
        public string Tipo { get; init; } = string.Empty;
        public string Correo { get; init; } = string.Empty;
        public bool Enviado { get; init; }
        public string? Error { get; init; }
        public DateTime FechaIntento { get; init; }
    }
}
