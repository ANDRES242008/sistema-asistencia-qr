using Microsoft.Data.SqlClient;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// Acceso a datos de la tabla Asistencia.
    /// </summary>
    public class AsistenciaRepository
    {
        public bool ExisteAsistenciaHoy(int estudianteId)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select count(*)
                from Asistencia
                where EstudianteId = @EstudianteId and Fecha = @Fecha;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@EstudianteId", estudianteId);
            comando.Parameters.AddWithValue("@Fecha", DateTime.Today);

            conexion.Open();
            int total = (int)comando.ExecuteScalar();
            return total > 0;
        }

        public void RegistrarAsistencia(int estudianteId)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                insert into Asistencia (EstudianteId, Fecha, Hora)
                values (@EstudianteId, @Fecha, @Hora);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@EstudianteId", estudianteId);
            comando.Parameters.AddWithValue("@Fecha", DateTime.Today);
            comando.Parameters.AddWithValue("@Hora", DateTime.Now.TimeOfDay);

            conexion.Open();
            comando.ExecuteNonQuery();
        }
    }
}
