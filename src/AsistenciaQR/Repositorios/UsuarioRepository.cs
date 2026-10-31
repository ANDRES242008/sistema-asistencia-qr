using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

namespace AsistenciaQR.Repositorios
{
    public class UsuarioRepository
    {
        private const string ColumnasUsuario = @"
            UsuarioId, NombreUsuario, NombreCompleto, ContrasenaHash, ContrasenaSal, Rol, Activo, FechaRegistro,
            IntentosFallidos, BloqueadoHasta, DebeCambiarContrasena";

        public Usuario? ObtenerPorNombreUsuario(string nombreUsuario)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            string sql = $"select {ColumnasUsuario} from Usuarios where NombreUsuario = @NombreUsuario;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            return lector.Read() ? LeerUsuario(lector) : null;
        }

        public Usuario? ObtenerPorId(int usuarioId)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            string sql = $"select {ColumnasUsuario} from Usuarios where UsuarioId = @UsuarioId;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            return lector.Read() ? LeerUsuario(lector) : null;
        }

        public List<Usuario> Listar(string? texto, bool incluirInactivos)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            string sql = $@"
                select {ColumnasUsuario}
                from Usuarios
                where (@Texto is null or NombreUsuario like @Texto or NombreCompleto like @Texto)
                  and (@IncluirInactivos = 1 or Activo = 1)
                order by NombreCompleto;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Texto",
                string.IsNullOrWhiteSpace(texto) ? DBNull.Value : $"%{texto.Trim()}%");
            comando.Parameters.AddWithValue("@IncluirInactivos", incluirInactivos);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            var usuarios = new List<Usuario>();
            while (lector.Read()) usuarios.Add(LeerUsuario(lector));
            return usuarios;
        }

        public int ContarUsuarios()
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = "select count(*) from Usuarios;";

            using var comando = new SqlCommand(sql, conexion);

            conexion.Open();
            return (int)comando.ExecuteScalar();
        }

        public int ContarAdministradoresActivos()
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = "select count(*) from Usuarios where Rol = 'Administrador' and Activo = 1;";

            using var comando = new SqlCommand(sql, conexion);

            conexion.Open();
            return (int)comando.ExecuteScalar();
        }

        public int Insertar(Usuario usuario)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                insert into Usuarios (NombreUsuario, NombreCompleto, ContrasenaHash, ContrasenaSal, Rol, DebeCambiarContrasena)
                values (@NombreUsuario, @NombreCompleto, @ContrasenaHash, @ContrasenaSal, @Rol, @DebeCambiarContrasena);
                select cast(scope_identity() as int);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@NombreUsuario", usuario.NombreUsuario);
            comando.Parameters.AddWithValue("@NombreCompleto", usuario.NombreCompleto);
            comando.Parameters.AddWithValue("@ContrasenaHash", usuario.ContrasenaHash);
            comando.Parameters.AddWithValue("@ContrasenaSal", usuario.ContrasenaSal);
            comando.Parameters.AddWithValue("@Rol", usuario.Rol);
            comando.Parameters.AddWithValue("@DebeCambiarContrasena", usuario.DebeCambiarContrasena);

            conexion.Open();
            return (int)comando.ExecuteScalar();
        }

        public void ActualizarDatos(int usuarioId, string nombreCompleto, string rol)
        {
            Ejecutar(@"update Usuarios set NombreCompleto = @NombreCompleto, Rol = @Rol where UsuarioId = @UsuarioId;",
                ("@UsuarioId", usuarioId), ("@NombreCompleto", nombreCompleto), ("@Rol", rol));
        }

        public void CambiarEstado(int usuarioId, bool activo)
        {
            Ejecutar("update Usuarios set Activo = @Activo where UsuarioId = @UsuarioId;",
                ("@UsuarioId", usuarioId), ("@Activo", activo));
        }

        /// <summary>
        /// Guarda una contrasena nueva y, de paso, desbloquea la cuenta
        /// y reinicia el contador de intentos fallidos.
        /// </summary>
        public void ActualizarContrasena(int usuarioId, string hash, string sal, bool debeCambiar)
        {
            Ejecutar(@"
                update Usuarios
                set ContrasenaHash = @Hash, ContrasenaSal = @Sal, DebeCambiarContrasena = @DebeCambiar,
                    IntentosFallidos = 0, BloqueadoHasta = null
                where UsuarioId = @UsuarioId;",
                ("@UsuarioId", usuarioId), ("@Hash", hash), ("@Sal", sal), ("@DebeCambiar", debeCambiar));
        }

        public void ActualizarIntentos(int usuarioId, int intentosFallidos, DateTime? bloqueadoHasta)
        {
            Ejecutar(@"
                update Usuarios set IntentosFallidos = @Intentos, BloqueadoHasta = @BloqueadoHasta
                where UsuarioId = @UsuarioId;",
                ("@UsuarioId", usuarioId), ("@Intentos", intentosFallidos),
                ("@BloqueadoHasta", (object?)bloqueadoHasta ?? DBNull.Value));
        }

        private static void Ejecutar(string sql, params (string Nombre, object Valor)[] parametros)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            using var comando = new SqlCommand(sql, conexion);
            foreach (var (nombre, valor) in parametros)
            {
                comando.Parameters.AddWithValue(nombre, valor);
            }

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        private static Usuario LeerUsuario(SqlDataReader lector) => new()
        {
            UsuarioId = lector.GetInt32(0),
            NombreUsuario = lector.GetString(1),
            NombreCompleto = lector.GetString(2),
            ContrasenaHash = lector.GetString(3),
            ContrasenaSal = lector.GetString(4),
            Rol = lector.GetString(5),
            Activo = lector.GetBoolean(6),
            FechaRegistro = lector.GetDateTime(7),
            IntentosFallidos = lector.GetInt32(8),
            BloqueadoHasta = lector.IsDBNull(9) ? null : lector.GetDateTime(9),
            DebeCambiarContrasena = lector.GetBoolean(10)
        };
    }
}
