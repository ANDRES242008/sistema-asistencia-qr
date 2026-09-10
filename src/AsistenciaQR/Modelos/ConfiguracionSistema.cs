namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Configuracion especifica de esta computadora. No va en la
    /// base de datos porque cada integrante del equipo puede tener
    /// su camara conectada en un indice distinto.
    /// </summary>
    public class ConfiguracionSistema
    {
        public int IndiceCamara { get; set; } = 0;
        public int SegundosCooldown { get; set; } = 4;
    }
}
