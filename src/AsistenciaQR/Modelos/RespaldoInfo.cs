namespace AsistenciaQR.Modelos
{
    public enum TipoRespaldo
    {
        Automatico,
        Manual,
        AntesDeRestaurar
    }

    /// <summary>Un archivo .bak de la carpeta de respaldos.</summary>
    public class RespaldoInfo
    {
        public string Ruta { get; init; } = string.Empty;
        public DateTime Fecha { get; init; }
        public TipoRespaldo Tipo { get; init; }
        public long TamanoBytes { get; init; }

        public string NombreArchivo => Path.GetFileName(Ruta);

        public string TipoTexto => Tipo switch
        {
            TipoRespaldo.Automatico => "Automatico",
            TipoRespaldo.Manual => "Manual",
            _ => "Antes de restaurar"
        };
    }
}
