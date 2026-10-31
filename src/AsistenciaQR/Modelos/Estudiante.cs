namespace AsistenciaQR.Modelos
{
  
    public class Estudiante
    {
        public int EstudianteId { get; set; }
        public string NIE { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public string? FotoRuta { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
