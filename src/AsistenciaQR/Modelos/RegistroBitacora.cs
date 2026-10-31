namespace AsistenciaQR.Modelos
{
    public class RegistroBitacora
    {
        public int BitacoraId { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
    }
}
