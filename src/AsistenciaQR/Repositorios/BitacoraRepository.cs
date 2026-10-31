using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

namespace AsistenciaQR.Repositorios
{
    public class BitacoraRepository
    {
        public const int MaximoResultados = 2000;

        public void Insertar(string usuario, string accion, string detalle)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = "insert into Bitacora (Usuario, Accion, Detalle) values (@Usuario, @Accion, @Detalle);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Usuario", usuario);
            comando.Parameters.AddWithValue("@Accion", accion);
            comando.Parameters.AddWithValue("@Detalle", detalle);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        /// <summary>
        /// Registros del rango de fechas (ambos dias incluidos), del mas
        /// reciente al mas viejo, con filtros opcionales.
        /// </summary>
        public List<RegistroBitacora> Listar(DateTime desde, DateTime hasta, string? usuario, string? accion, string? texto)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            string sql = $@"
                select top ({MaximoResultados}) BitacoraId, Fecha, Usuario, Accion, Detalle
                from Bitacora
                where Fecha >= @Desde and Fecha < @Hasta
                  and (@Usuario is null or Usuario = @Usuario)
                  and (@Accion is null or Accion = @Accion)
                  and (@Texto is null or Detalle like @Texto or Usuario like @Texto)
                order by Fecha desc, BitacoraId desc;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Desde", desde.Date);
            comando.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));
            comando.Parameters.AddWithValue("@Usuario", (object?)usuario ?? DBNull.Value);
            comando.Parameters.AddWithValue("@Accion", (object?)accion ?? DBNull.Value);
            comando.Parameters.AddWithValue("@Texto",
                string.IsNullOrWhiteSpace(texto) ? DBNull.Value : $"%{texto.Trim()}%");

            conexion.Open();
            using var lector = comando.ExecuteReader();

            var registros = new List<RegistroBitacora>();
            while (lector.Read())
            {
                registros.Add(new RegistroBitacora
                {
                    BitacoraId = lector.GetInt32(0),
                    Fecha = lector.GetDateTime(1),
                    Usuario = lector.GetString(2),
                    Accion = lector.GetString(3),
                    Detalle = lector.GetString(4)
                });
            }
            return registros;
        }

        public List<string> ObtenerUsuarios() => ObtenerDistintos("Usuario");

        public List<string> ObtenerAcciones() => ObtenerDistintos("Accion");

        private static List<string> ObtenerDistintos(string columna)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            // columna viene de las dos llamadas de arriba, nunca del usuario.
            using var comando = new SqlCommand($"select distinct {columna} from Bitacora order by {columna};", conexion);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            var valores = new List<string>();
            while (lector.Read()) valores.Add(lector.GetString(0));
            return valores;
        }
    }
}
