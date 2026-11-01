namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Identidad de la institucion que usa el sistema: aparece en el
    /// carnet, los reportes y el menu. Logo es un PNG ya reducido.
    /// ColorPrincipal es "#RRGGBB" o null para el color original.
    /// </summary>
    public class ConfiguracionInstitucion
    {
        public string Nombre { get; set; } = string.Empty;
        public byte[]? Logo { get; set; }
        public string? ColorPrincipal { get; set; }
    }
}
