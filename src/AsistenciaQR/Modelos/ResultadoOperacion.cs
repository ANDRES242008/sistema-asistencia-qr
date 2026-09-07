namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Resultado generico de una operacion (registrar, editar, dar de
    /// baja, etc.), para que la pantalla sepa si salio bien y que
    /// mensaje mostrarle al usuario. Se reutiliza en todo el CRUD para
    /// no repetir esta estructura en cada metodo.
    /// </summary>
    public class ResultadoOperacion
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        public static ResultadoOperacion Ok(string mensaje = "Operacion exitosa.") =>
            new() { Exitoso = true, Mensaje = mensaje };

        public static ResultadoOperacion Error(string mensaje) =>
            new() { Exitoso = false, Mensaje = mensaje };
    }
}
