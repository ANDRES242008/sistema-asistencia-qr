namespace AsistenciaQR.Modelos
{
   
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
