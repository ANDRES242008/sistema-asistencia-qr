namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Representa un estudiante registrado en el sistema.
    /// Esta clase no contiene logica, solo los datos (molde de la tabla Estudiantes).
    /// </summary>
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
