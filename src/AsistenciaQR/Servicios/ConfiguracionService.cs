using System.Text.Json;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Lee y guarda la configuracion local del sistema (indice de
    /// camara, segundos de cooldown) en un archivo JSON fuera del
    /// repositorio de Git, y prueba la conexion a SQL Server.
    /// </summary>
    public class ConfiguracionService
    {
        private readonly string _rutaArchivo;

        public ConfiguracionService()
        {
            string carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AsistenciaQR");

            Directory.CreateDirectory(carpeta);
            _rutaArchivo = Path.Combine(carpeta, "configuracion.json");
        }

        public ConfiguracionSistema Cargar()
        {
            if (!File.Exists(_rutaArchivo))
            {
                return new ConfiguracionSistema();
            }

            try
            {
                string json = File.ReadAllText(_rutaArchivo);
                return JsonSerializer.Deserialize<ConfiguracionSistema>(json) ?? new ConfiguracionSistema();
            }
            catch
            {
                // Si el archivo esta corrupto o vacio, no tumba la
                // aplicacion: simplemente usa los valores por defecto.
                return new ConfiguracionSistema();
            }
        }

        public void Guardar(ConfiguracionSistema configuracion)
        {
            string json = JsonSerializer.Serialize(configuracion, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_rutaArchivo, json);
        }

        public bool ProbarConexionSql(out string mensaje)
        {
            try
            {
                using var conexion = ConexionSQL.ObtenerConexion();
                conexion.Open();
                mensaje = "Conexion exitosa a la base de datos.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error de conexion: {ex.Message}";
                return false;
            }
        }
    }
}
