namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Paquete completo de datos para el Dashboard: cuantos estudiantes
    /// hay en total, cuantos marcaron asistencia, y el detalle de cada uno.
    /// Faltantes y Porcentaje se calculan solos, no hay que guardarlos.
    /// </summary>
    public class ResumenAsistencia
    {
        public DateTime Fecha { get; set; }
        public int TotalActivos { get; set; }
        public int Presentes { get; set; }

        public int Faltantes => TotalActivos - Presentes;

        public double Porcentaje => TotalActivos == 0
            ? 0
            : (double)Presentes / TotalActivos * 100;

        public List<AsistenciaDetalle> Detalle { get; set; } = new();
    }
}
