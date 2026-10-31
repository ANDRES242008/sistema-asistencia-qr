namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Una fila del Excel de importacion, ya convertida en estudiante
    /// y revisada. Error es null si la fila se puede importar.
    /// </summary>
    public class FilaImportacion
    {
        public int NumeroFila { get; init; }
        public Estudiante Estudiante { get; init; } = new();
        public string? Error { get; set; }

        public bool EsValida => Error is null;
    }
}
