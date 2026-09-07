using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

namespace AsistenciaQR.Repositorios
{
    public class UsuarioRepository
    {
        public Usuario? ObtenerPorNombreUsuario(string nombreUsuario)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select UsuarioId, NombreUsuario, NombreCompleto, ContrasenaHash, ContrasenaSal, Rol, Activo, FechaRegistro
                from Usuarios
                where NombreUsuario = @NombreUsuario;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            if (!lector.Read()) return null;

            return new Usuario
            {
                UsuarioId = lector.GetInt32(0),
                NombreUsuario = lector.GetString(1),
                NombreCompleto = lector.GetString(2),
                ContrasenaHash = lector.GetString(3),
                ContrasenaSal = lector.GetString(4),
                Rol = lector.GetString(5),
                Activo = lector.GetBoolean(6),
                FechaRegistro = lector.GetDateTime(7)
            };
        }

        public int Insertar(Usuario usuario)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                insert into Usuarios (NombreUsuario, NombreCompleto, ContrasenaHash, ContrasenaSal, Rol)
                values (@NombreUsuario, @NombreCompleto, @ContrasenaHash, @ContrasenaSal, @Rol);
                select cast(scope_identity() as int);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@NombreUsuario", usuario.NombreUsuario);
            comando.Parameters.AddWithValue("@NombreCompleto", usuario.NombreCompleto);
            comando.Parameters.AddWithValue("@ContrasenaHash", usuario.ContrasenaHash);
            comando.Parameters.AddWithValue("@ContrasenaSal", usuario.ContrasenaSal);
            comando.Parameters.AddWithValue("@Rol", usuario.Rol);

            conexion.Open();
            return (int)comando.ExecuteScalar();
        }
    }
}
