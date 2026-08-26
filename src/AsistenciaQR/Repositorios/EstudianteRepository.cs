using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// Acceso a datos de la tabla Estudiantes.
    /// El CRUD completo (registrar/editar/eliminar) se agrega en la Fase 3.
    /// </summary>
    public class EstudianteRepository
    {
        public Estudiante? ObtenerPorNIE(string nie)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select EstudianteId, NIE, NombreCompleto, Grado, Seccion, Correo, FotoRuta, Activo, FechaRegistro
                from Estudiantes
                where NIE = @NIE;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@NIE", nie);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            return lector.Read() ? LeerEstudiante(lector) : null;
        }

        public Estudiante? ObtenerPorId(int estudianteId)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select EstudianteId, NIE, NombreCompleto, Grado, Seccion, Correo, FotoRuta, Activo, FechaRegistro
                from Estudiantes
                where EstudianteId = @EstudianteId;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@EstudianteId", estudianteId);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            return lector.Read() ? LeerEstudiante(lector) : null;
        }

        private static Estudiante LeerEstudiante(SqlDataReader lector)
        {
            return new Estudiante
            {
                EstudianteId = lector.GetInt32(0),
                NIE = lector.GetString(1),
                NombreCompleto = lector.GetString(2),
                Grado = lector.GetString(3),
                Seccion = lector.GetString(4),
                Correo = lector.IsDBNull(5) ? null : lector.GetString(5),
                FotoRuta = lector.IsDBNull(6) ? null : lector.GetString(6),
                Activo = lector.GetBoolean(7),
                FechaRegistro = lector.GetDateTime(8)
            };
        }
    }
}
