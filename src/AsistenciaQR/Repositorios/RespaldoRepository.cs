using Microsoft.Data.SqlClient;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// Comandos de respaldo de SQL Server. Los archivos los escribe y
    /// lee el servicio de SQL Server (no la app), por eso las rutas
    /// deben estar en una carpeta a la que ese servicio tenga acceso.
    /// </summary>
    public static class RespaldoRepository
    {
        public static string NombreBaseDatos
        {
            get
            {
                using var conexion = ConexionSQL.ObtenerConexion();
                return conexion.Database;
            }
        }

        public static void Respaldar(string rutaArchivo)
        {
            // COPY_ONLY: no altera la cadena de respaldos que el colegio
            // pudiera tener con otra herramienta. CHECKSUM: detecta
            // paginas danadas al momento de respaldar.
            const string sql = @"
                declare @bd sysname = db_name();
                backup database @bd to disk = @Ruta
                with init, copy_only, checksum, name = N'AsistenciaQR';";

            using var conexion = ConexionSQL.ObtenerConexion();
            using var comando = new SqlCommand(sql, conexion) { CommandTimeout = 600 };
            comando.Parameters.AddWithValue("@Ruta", rutaArchivo);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        /// <summary>
        /// Revisa que el archivo sea un respaldo valido y completo, y
        /// devuelve el nombre de la base de datos que contiene.
        /// </summary>
        public static string Verificar(string rutaArchivo)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            conexion.Open();

            // Sin "with checksum": asi tambien acepta respaldos hechos con
            // otras herramientas (SSMS); si traen checksum, igual se revisa.
            using (var verificar = new SqlCommand("restore verifyonly from disk = @Ruta;", conexion) { CommandTimeout = 600 })
            {
                verificar.Parameters.AddWithValue("@Ruta", rutaArchivo);
                verificar.ExecuteNonQuery();
            }

            using var encabezado = new SqlCommand("restore headeronly from disk = @Ruta;", conexion);
            encabezado.Parameters.AddWithValue("@Ruta", rutaArchivo);
            using var lector = encabezado.ExecuteReader();
            lector.Read();
            return lector.GetString(lector.GetOrdinal("DatabaseName"));
        }

        /// <summary>
        /// Reemplaza la base de datos actual por el respaldo. Se hace
        /// desde master, con la base en modo de un solo usuario para
        /// cerrar cualquier otra conexion abierta. Los archivos se
        /// restauran sobre las rutas actuales (MOVE), asi funciona
        /// aunque el respaldo venga de otra computadora.
        /// </summary>
        public static void Restaurar(string rutaArchivo)
        {
            string baseDatos = NombreBaseDatos;
            SqlConnection.ClearAllPools();

            var constructor = new SqlConnectionStringBuilder(ConexionSQL.ObtenerConexion().ConnectionString)
            {
                InitialCatalog = "master",
                Pooling = false
            };

            using var conexion = new SqlConnection(constructor.ConnectionString);
            conexion.Open();

            var mover = ObtenerClausulasMove(conexion, rutaArchivo, baseDatos);

            string bd = $"[{baseDatos.Replace("]", "]]")}]";
            string sql = $@"
                alter database {bd} set single_user with rollback immediate;
                begin try
                    restore database {bd} from disk = @Ruta with replace{mover.Clausulas};
                end try
                begin catch
                    alter database {bd} set multi_user;
                    throw;
                end catch
                alter database {bd} set multi_user;";

            using var comando = new SqlCommand(sql, conexion) { CommandTimeout = 1800 };
            comando.Parameters.AddWithValue("@Ruta", rutaArchivo);
            foreach (var (nombre, valor) in mover.Parametros) comando.Parameters.AddWithValue(nombre, valor);
            comando.ExecuteNonQuery();
        }

        /// <summary>
        /// Elimina un respaldo. Lo hace SQL Server porque es el dueno
        /// del archivo; la app no tiene permiso para borrarlo.
        /// </summary>
        public static void Eliminar(string rutaArchivo)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            using var comando = new SqlCommand("exec master.sys.xp_delete_file 0, @Ruta;", conexion);
            comando.Parameters.AddWithValue("@Ruta", rutaArchivo);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        private static (string Clausulas, List<(string, object)> Parametros) ObtenerClausulasMove(
            SqlConnection conexion, string rutaArchivo, string baseDatos)
        {
            // Rutas actuales de los archivos de datos (D) y log (L).
            var destinos = new Dictionary<string, string>();
            using (var actuales = new SqlCommand(@"
                select case type when 0 then 'D' else 'L' end, physical_name
                from sys.master_files where database_id = db_id(@Bd) and type in (0, 1);", conexion))
            {
                actuales.Parameters.AddWithValue("@Bd", baseDatos);
                using var lector = actuales.ExecuteReader();
                while (lector.Read()) destinos.TryAdd(lector.GetString(0), lector.GetString(1));
            }

            var parametros = new List<(string, object)>();
            var clausulas = new System.Text.StringBuilder();
            using (var archivos = new SqlCommand("restore filelistonly from disk = @Ruta;", conexion))
            {
                archivos.Parameters.AddWithValue("@Ruta", rutaArchivo);
                using var lector = archivos.ExecuteReader();
                int i = 0;
                while (lector.Read())
                {
                    string logico = lector.GetString(lector.GetOrdinal("LogicalName"));
                    string tipo = lector.GetString(lector.GetOrdinal("Type"));
                    if (!destinos.TryGetValue(tipo, out string? destino)) continue;

                    clausulas.Append($", move @Logico{i} to @Destino{i}");
                    parametros.Add(($"@Logico{i}", logico));
                    parametros.Add(($"@Destino{i}", destino));
                    i++;
                }
            }

            return (clausulas.ToString(), parametros);
        }
    }
}
