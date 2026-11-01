using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// Entrega conexiones a SQL Server. La cadena de conexion ya no
    /// vive en el codigo: se busca, en este orden, en la variable de
    /// entorno ASISTENCIAQR_CONEXION, en el appsettings.json de esta
    /// computadora (C:\ProgramData\AsistenciaQR, lo escribe el asistente
    /// de configuracion) y en el appsettings.json junto al .exe. Si
    /// ninguna existe, se usa la instancia local SQLEXPRESS.
    /// </summary>
    public static class ConexionSQL
    {
        public const string VariableEntorno = "ASISTENCIAQR_CONEXION";
        public const string NombreArchivo = "appsettings.json";
        public const string NombreBaseDatos = "AsistenciaQR";

        private const string CadenaPorDefecto =
            @"Server=.\SQLEXPRESS;Database=AsistenciaQR;Trusted_Connection=True;TrustServerCertificate=True;";

        private static string? _cadenaConexion;

        /// <summary>appsettings.json junto al .exe (viene con el programa).</summary>
        public static string RutaArchivo => Path.Combine(AppContext.BaseDirectory, NombreArchivo);

        /// <summary>
        /// appsettings.json de esta computadora. Archivos de programa no se
        /// puede modificar sin permisos de administrador; ProgramData si.
        /// </summary>
        public static string RutaArchivoEquipo => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AsistenciaQR", NombreArchivo);

        public static string CadenaActual => _cadenaConexion ??= ResolverCadena(
            Environment.GetEnvironmentVariable(VariableEntorno),
            LeerArchivoSiExiste(RutaArchivoEquipo),
            LeerArchivoSiExiste(RutaArchivo));

        public static SqlConnection ObtenerConexion()
        {
            return new SqlConnection(CadenaActual);
        }

        /// <summary>
        /// Guarda la conexion elegida en el asistente para esta computadora
        /// y la empieza a usar de inmediato.
        /// </summary>
        public static void GuardarParaEsteEquipo(string cadena)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivoEquipo)!);
            string json = JsonSerializer.Serialize(
                new { ConnectionStrings = new { AsistenciaQR = cadena } },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RutaArchivoEquipo, json);
            _cadenaConexion = null;
        }

        /// <summary>Cadena de conexion con Windows a una instancia y base de datos.</summary>
        public static string CrearCadena(string servidor, string baseDatos = NombreBaseDatos) =>
            new SqlConnectionStringBuilder
            {
                DataSource = servidor,
                InitialCatalog = baseDatos,
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                ConnectTimeout = 8
            }.ConnectionString;

        /// <summary>
        /// Decide que cadena usar: la variable de entorno y luego el primer
        /// archivo que tenga una. Separado de la lectura de archivos para
        /// poder probarlo sin depender del disco ni del entorno.
        /// </summary>
        internal static string ResolverCadena(string? variableEntorno, params string?[] jsonsEnOrden)
        {
            if (!string.IsNullOrWhiteSpace(variableEntorno)) return variableEntorno;

            foreach (string? json in jsonsEnOrden)
            {
                string? desdeJson = LeerCadenaDeJson(json);
                if (!string.IsNullOrWhiteSpace(desdeJson)) return desdeJson;
            }

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
