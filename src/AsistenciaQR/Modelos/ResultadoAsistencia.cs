namespace AsistenciaQR.Modelos
{
   
    public class ResultadoAsistencia
    {
        public ResultadoEscaneo Estado { get; set; }
        public Estudiante? Estudiante { get; set; }
        public DateTime? HoraRegistro { get; set; }
        public bool EsTarde { get; set; }
    }
}
