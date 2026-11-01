using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

namespace AsistenciaQR.Repositorios
{
    public class InstitucionRepository
    {
        public ConfiguracionInstitucion? Obtener()
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = "select Nombre, Logo, ColorPrincipal from ConfiguracionInstitucion where ConfiguracionInstitucionId = 1;";

            using var comando = new SqlCommand(sql, conexion);

            conexion.Open();
            using var lector = comando.ExecuteReader();
            if (!lector.Read()) return null;

            return new ConfiguracionInstitucion
            {
                Nombre = lector.GetString(0),
                Logo = lector.IsDBNull(1) ? null : (byte[])lector[1],
                ColorPrincipal = lector.IsDBNull(2) ? null : lector.GetString(2)
            };
        }

        public void Guardar(ConfiguracionInstitucion institucion)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                update ConfiguracionInstitucion
                set Nombre = @Nombre, Logo = @Logo, ColorPrincipal = @Color, FechaActualizacion = getdate()
                where ConfiguracionInstitucionId = 1;

                if @@rowcount = 0
                    insert into ConfiguracionInstitucion (ConfiguracionInstitucionId, Nombre, Logo, ColorPrincipal)
                    values (1, @Nombre, @Logo, @Color);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Nombre", institucion.Nombre);
            comando.Parameters.Add("@Logo", System.Data.SqlDbType.VarBinary, -1).Value =
                (object?)institucion.Logo ?? DBNull.Value;
            comando.Parameters.AddWithValue("@Color", (object?)institucion.ColorPrincipal ?? DBNull.Value);

            conexion.Open();
            comando.ExecuteNonQuery();
        }
    }
}
