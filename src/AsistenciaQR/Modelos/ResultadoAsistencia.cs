namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Paquete completo de datos que la pantalla del Kiosco necesita
    /// para dibujar la alerta correcta (verde/naranja/azul/rojo) y la
    /// tarjeta con los datos del estudiante.
    /// </summary>
    public class ResultadoAsistencia
    {
        public ResultadoEscaneo Estado { get; set; }
        public Estudiante? Estudiante { get; set; }
        public DateTime? HoraRegistro { get; set; }
        public bool EsTarde { get; set; }
    }
}
