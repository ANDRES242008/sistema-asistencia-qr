namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Correo del colegio para los avisos a padres. Contrasena va en
    /// texto solo en memoria; en la base se guarda cifrada.
    /// </summary>
    public class ConfiguracionCorreo
    {
        public bool Activo { get; set; }
        public TimeSpan HoraEnvio { get; set; } = new(9, 0, 0);
        public bool AvisarTardanzas { get; set; } = true;
        public bool AvisarAusencias { get; set; } = true;
        public string ServidorSmtp { get; set; } = "smtp.gmail.com";
        public int Puerto { get; set; } = 587;
        public bool UsarSsl { get; set; } = true;
        public string Usuario { get; set; } = string.Empty;
        public string? Contrasena { get; set; }
        public string EquipoQueEnvia { get; set; } = string.Empty;
        public DateTime? UltimoEnvioAutomatico { get; set; }
    }
}
