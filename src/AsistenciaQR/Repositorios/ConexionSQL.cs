using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// Entrega conexiones a SQL Server. La cadena de conexion ya no
    /// vive en el codigo: se busca, en este orden, en la variable de
    /// entorno ASISTENCIAQR_CONEXION y en appsettings.json (junto al
    /// .exe). Si ninguna existe, se usa la instancia local SQLEXPRESS.
    /// </summary>
    public static class ConexionSQL
    {
        public const string VariableEntorno = "ASISTENCIAQR_CONEXION";
        public const string NombreArchivo = "appsettings.json";

        private const string CadenaPorDefecto =
            @"Server=.\SQLEXPRESS;Database=AsistenciaQR;Trusted_Connection=True;TrustServerCertificate=True;";

        private static readonly Lazy<string> _cadenaConexion = new(() => ResolverCadena(
            Environment.GetEnvironmentVariable(VariableEntorno),
            LeerArchivoSiExiste(RutaArchivo)));

        public static string RutaArchivo => Path.Combine(AppContext.BaseDirectory, NombreArchivo);

        public static SqlConnection ObtenerConexion()
        {
            return new SqlConnection(_cadenaConexion.Value);
        }

        /// <summary>
        /// Decide que cadena usar. Separado de la lectura de archivos
        /// para poder probarlo sin depender del disco ni del entorno.
        /// </summary>
        internal static string ResolverCadena(string? variableEntorno, string? jsonConfiguracion)
        {
            if (!string.IsNullOrWhiteSpace(variableEntorno)) return variableEntorno;

            string? desdeJson = LeerCadenaDeJson(jsonConfiguracion);
            if (!string.IsNullOrWhiteSpace(desdeJson)) return desdeJson;

            return CadenaPorDefecto;
        }

        internal static string? LeerCadenaDeJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                using var documento = JsonDocument.Parse(json);
                if (documento.RootElement.TryGetProperty("ConnectionStrings", out var cadenas) &&
                    cadenas.TryGetProperty("AsistenciaQR", out var cadena) &&
                    cadena.ValueKind == JsonValueKind.String)
                {
                    return cadena.GetString();
                }
            }
            catch (JsonException)
            {
                // Un appsettings.json mal escrito no debe tumbar la app:
                // se cae a la cadena por defecto.
            }

            return null;
        }

        private static string? LeerArchivoSiExiste(string ruta)
        {
            try
            {
                return File.Exists(ruta) ? File.ReadAllText(ruta) : null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
