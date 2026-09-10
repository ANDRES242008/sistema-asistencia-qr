using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

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

        public void RegistrarAsistencia(int estudianteId, string? observacion, string? estadoPuntualidad)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                insert into Asistencia (EstudianteId, Fecha, Hora, Observacion, EstadoPuntualidad)
                values (@EstudianteId, @Fecha, @Hora, @Observacion, @EstadoPuntualidad);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@EstudianteId", estudianteId);
            comando.Parameters.AddWithValue("@Fecha", DateTime.Today);
            comando.Parameters.AddWithValue("@Hora", DateTime.Now.TimeOfDay);
            comando.Parameters.AddWithValue("@Observacion", (object?)observacion ?? DBNull.Value);
            comando.Parameters.AddWithValue("@EstadoPuntualidad", (object?)estadoPuntualidad ?? DBNull.Value);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        public int ContarPresentesDelDia(DateTime fecha, string? seccion = null)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select count(*)
                from Asistencia a
                inner join Estudiantes e on e.EstudianteId = a.EstudianteId
                where a.Fecha = @Fecha
                  and (@Seccion is null or e.Seccion = @Seccion);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Fecha", fecha.Date);
            comando.Parameters.AddWithValue("@Seccion", (object?)seccion ?? DBNull.Value);

            conexion.Open();
            return (int)comando.ExecuteScalar();
        }

        public int ContarTardanzasDelDia(DateTime fecha, string? seccion = null)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select count(*)
                from Asistencia a
                inner join Estudiantes e on e.EstudianteId = a.EstudianteId
                where a.Fecha = @Fecha
                  and a.EstadoPuntualidad = 'Tarde'
                  and (@Seccion is null or e.Seccion = @Seccion);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Fecha", fecha.Date);
            comando.Parameters.AddWithValue("@Seccion", (object?)seccion ?? DBNull.Value);

            conexion.Open();
            return (int)comando.ExecuteScalar();
        }

        public List<AsistenciaDetalle> ListarDetalleDelDia(DateTime fecha, string? seccion = null)
        {
            var resultado = new List<AsistenciaDetalle>();

            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select e.NIE, e.NombreCompleto, e.Grado, e.Seccion, a.Fecha, a.Hora, a.Observacion, a.EstadoPuntualidad
                from Asistencia a
                inner join Estudiantes e on e.EstudianteId = a.EstudianteId
                where a.Fecha = @Fecha
                  and (@Seccion is null or e.Seccion = @Seccion)
                order by a.Hora desc;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Fecha", fecha.Date);
            comando.Parameters.AddWithValue("@Seccion", (object?)seccion ?? DBNull.Value);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            while (lector.Read())
            {
                resultado.Add(LeerDetalle(lector));
            }

            return resultado;
        }

        public List<AsistenciaDetalle> ListarHistorico(
            DateTime fechaInicio, DateTime fechaFin,
            string? busqueda = null, string? grado = null, string? seccion = null)
        {
            var resultado = new List<AsistenciaDetalle>();

            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select e.NIE, e.NombreCompleto, e.Grado, e.Seccion, a.Fecha, a.Hora, a.Observacion, a.EstadoPuntualidad
                from Asistencia a
                inner join Estudiantes e on e.EstudianteId = a.EstudianteId
                where a.Fecha between @FechaInicio and @FechaFin
                  and (@Busqueda is null or e.NombreCompleto like '%' + @Busqueda + '%' or e.NIE like '%' + @Busqueda + '%')
                  and (@Grado is null or e.Grado = @Grado)
                  and (@Seccion is null or e.Seccion = @Seccion)
                order by a.Fecha desc, a.Hora desc;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@FechaInicio", fechaInicio.Date);
            comando.Parameters.AddWithValue("@FechaFin", fechaFin.Date);
            comando.Parameters.AddWithValue("@Busqueda", string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda);
            comando.Parameters.AddWithValue("@Grado", (object?)grado ?? DBNull.Value);
            comando.Parameters.AddWithValue("@Seccion", (object?)seccion ?? DBNull.Value);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            while (lector.Read())
            {
                resultado.Add(LeerDetalle(lector));
            }

            return resultado;
        }

        private static AsistenciaDetalle LeerDetalle(SqlDataReader lector)
        {
            return new AsistenciaDetalle
            {
                NIE = lector.GetString(0),
                NombreCompleto = lector.GetString(1),
                Grado = lector.GetString(2),
                Seccion = lector.GetString(3),
                Fecha = lector.GetDateTime(4),
                Hora = lector.GetTimeSpan(5),
                Observacion = lector.IsDBNull(6) ? null : lector.GetString(6),
                EstadoPuntualidad = lector.IsDBNull(7) ? null : lector.GetString(7)
            };
        }
    }
}
