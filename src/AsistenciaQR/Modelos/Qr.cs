namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Representa un codigo QR asignado a un estudiante.
    /// Se guarda en tabla separada de Estudiantes para poder
    /// regenerar el codigo (carnet perdido) sin perder el historial.
    /// </summary>
    public class Qr
    {
        public int QRId { get; set; }
        public int EstudianteId { get; set; }
        public string CodigoQR { get; set; } = string.Empty;
        public string? RutaImagen { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public bool Activo { get; set; }
    }
}
