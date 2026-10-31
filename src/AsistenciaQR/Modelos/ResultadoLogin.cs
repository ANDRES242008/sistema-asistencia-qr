namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Resultado de un intento de inicio de sesion. Si Usuario es null,
    /// Mensaje explica por que no se pudo entrar (datos incorrectos o
    /// cuenta bloqueada).
    /// </summary>
    public class ResultadoLogin
    {
        public Usuario? Usuario { get; init; }
        public string Mensaje { get; init; } = string.Empty;

        public bool Exitoso => Usuario is not null;
    }
}
