namespace AsistenciaQR.Modelos
{
    
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
