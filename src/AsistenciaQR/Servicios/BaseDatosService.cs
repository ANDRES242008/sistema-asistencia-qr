using System.Security.Principal;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Primer arranque en una computadora nueva: encuentra SQL Server,
    /// crea la base de datos AsistenciaQR si no existe, deja todas las
    /// tablas listas y guarda la conexion para esta computadora.
    /// </summary>
    public static class BaseDatosService
    {
        public const string UrlDescargaSqlExpress = "https://www.microsoft.com/es-es/sql-server/sql-server-downloads";

        /// <summary>Instancias de SQL Server instaladas en esta computadora.</summary>
        public static List<string> DetectarInstancias()
        {
            var instancias = new List<string>();

            try
            {
                using var maquina = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var nombres = maquina.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                if (nombres is not null)
                {
                    instancias.AddRange(nombres.GetValueNames().Select(NombreDeServidor));
                }

                using var localDb = maquina.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions");
                if (localDb is not null && localDb.SubKeyCount > 0)
                {
                    instancias.Add(@"(localdb)\MSSQLLocalDB");
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
            {
                // Sin permiso para leer el registro: el usuario escribe el nombre a mano.
            }

            // SQL Express primero: es lo que usan casi todos los colegios.
            return instancias.Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(i => i.EndsWith(@"\SQLEXPRESS", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>"MSSQLSERVER" es la instancia sin nombre ("."); las demas van como ".\NOMBRE".</summary>
        internal static string NombreDeServidor(string nombreInstancia) =>
            string.Equals(nombreInstancia, "MSSQLSERVER", StringComparison.OrdinalIgnoreCase) ? "." : $@".\{nombreInstancia}";

        /// <summary>
        /// Se conecta al servidor, crea la base si hace falta, aplica el
        /// esquema completo y guarda la conexion. Si permitirTodos es true,
        /// las demas cuentas de Windows de esta computadora tambien pueden
        /// leer y escribir datos (pero no respaldar ni restaurar).
        /// </summary>
        public static ResultadoOperacion Preparar(string servidor, bool permitirTodos, string baseDatos = ConexionSQL.NombreBaseDatos)
        {
            servidor = servidor.Trim();
            if (string.IsNullOrWhiteSpace(servidor))
            {
                return ResultadoOperacion.Error("Escribe o elige el servidor de SQL Server.");
            }

            bool creada;
            try
            {
                using var maestro = new SqlConnection(ConexionSQL.CrearCadena(servidor, "master"));
                maestro.Open();

                using var existe = new SqlCommand("select db_id(@Bd);", maestro);
                existe.Parameters.AddWithValue("@Bd", baseDatos);
                creada = existe.ExecuteScalar() is DBNull;
                if (creada)
                {
                    using var crear = new SqlCommand($"create database [{baseDatos.Replace("]", "]]")}];", maestro) { CommandTimeout = 120 };
                    crear.ExecuteNonQuery();
                }

                if (permitirTodos) PermitirUsuariosDelEquipo(maestro, baseDatos);
            }
            catch (SqlException ex)
            {
                return ResultadoOperacion.Error(ExplicarError(ex, servidor));
            }

            ConexionSQL.GuardarParaEsteEquipo(ConexionSQL.CrearCadena(servidor, baseDatos));

            try
            {
                EsquemaRepository.AplicarActualizaciones();
            }
            catch (SqlException ex)
            {
                return ResultadoOperacion.Error("Se conecto, pero no se pudieron crear las tablas: " + ex.Message);
            }

            return ResultadoOperacion.Ok(creada
                ? $"Base de datos creada en {servidor}. Todo listo."
                : $"Conectado a la base de datos existente en {servidor}. Todo listo.");
        }

        /// <summary>
        /// Da acceso de lectura y escritura al grupo local "Usuarios" (por su
        /// SID, asi funciona en Windows en espanol o ingles).
        /// </summary>
        private static void PermitirUsuariosDelEquipo(SqlConnection maestro, string baseDatos)
        {
            string grupo = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null).Translate(typeof(NTAccount)).Value;
            const string sql = @"
                if not exists (select 1 from sys.server_principals where name = @Grupo)
                    exec('create login ' + @GrupoSql + ' from windows');

                declare @usar nvarchar(max) = 'use ' + quotename(@Bd) + ';
                    if user_id(' + quotename(@Grupo, '''') + ') is null
                        create user ' + @GrupoSql + ' for login ' + @GrupoSql + ';
                    alter role db_datareader add member ' + @GrupoSql + ';
                    alter role db_datawriter add member ' + @GrupoSql + ';';
                exec(@usar);";

            using var comando = new SqlCommand(sql, maestro);
            comando.Parameters.AddWithValue("@Grupo", grupo);
            comando.Parameters.AddWithValue("@GrupoSql", "[" + grupo.Replace("]", "]]") + "]");
            comando.Parameters.AddWithValue("@Bd", baseDatos);
            comando.ExecuteNonQuery();
        }

        internal static string ExplicarError(SqlException ex, string servidor) => ex.Number switch
        {
            -1 or 2 or 53 or 26 or -2 =>
                $"No se encontro SQL Server en \"{servidor}\". Revisa el nombre (por ejemplo .\\SQLEXPRESS) " +
                "y que el servicio de SQL Server este iniciado. Si no esta instalado, descarga SQL Server Express (gratis).",
            18456 =>
                "SQL Server rechazo la cuenta de Windows con la que abriste el programa. " +
                "Abre el programa con la cuenta que instalo SQL Server (normalmente la del administrador).",
            262 or 1802 =>
                "Tu cuenta no tiene permiso para crear bases de datos en ese servidor. " +
                "Abre el programa con la cuenta que instalo SQL Server.",
            _ => ex.Message
        };
    }
}
