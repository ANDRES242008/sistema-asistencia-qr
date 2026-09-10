namespace AsistenciaQR.Modelos
{
    public class ResumenAsistencia
    {
        public DateTime Fecha { get; set; }
        public int TotalActivos { get; set; }
        public int Presentes { get; set; }
        public int Tardanzas { get; set; }

        public int Faltantes => TotalActivos - Presentes;

        public double Porcentaje => TotalActivos == 0
            ? 0
            : (double)Presentes / TotalActivos * 100;

        public List<AsistenciaDetalle> Detalle { get; set; } = new();
    }
}
