using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

namespace AsistenciaQR.Repositorios
{
   
    public class EstudianteRepository
    {
        public Estudiante? ObtenerPorNIE(string nie)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select EstudianteId, NIE, NombreCompleto, Grado, Seccion, Correo, FotoRuta, Activo, FechaRegistro,
                       EncargadoNombre, EncargadoCorreo
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
                select EstudianteId, NIE, NombreCompleto, Grado, Seccion, Correo, FotoRuta, Activo, FechaRegistro,
                       EncargadoNombre, EncargadoCorreo
                from Estudiantes
                where EstudianteId = @EstudianteId;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@EstudianteId", estudianteId);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            return lector.Read() ? LeerEstudiante(lector) : null;
        }

        public int Insertar(Estudiante estudiante)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                insert into Estudiantes (NIE, NombreCompleto, Grado, Seccion, Correo, FotoRuta, EncargadoNombre, EncargadoCorreo)
                values (@NIE, @NombreCompleto, @Grado, @Seccion, @Correo, @FotoRuta, @EncargadoNombre, @EncargadoCorreo);
                select cast(scope_identity() as int);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@NIE", estudiante.NIE);
            comando.Parameters.AddWithValue("@NombreCompleto", estudiante.NombreCompleto);
            comando.Parameters.AddWithValue("@Grado", estudiante.Grado);
            comando.Parameters.AddWithValue("@Seccion", estudiante.Seccion);
            comando.Parameters.AddWithValue("@Correo", (object?)estudiante.Correo ?? DBNull.Value);
            comando.Parameters.AddWithValue("@FotoRuta", (object?)estudiante.FotoRuta ?? DBNull.Value);
            AgregarEncargado(comando, estudiante);

            conexion.Open();
            return (int)comando.ExecuteScalar();
        }

        /// <summary>
        /// Inserta todos los estudiantes en una sola transaccion: si
        /// uno falla (por ejemplo, un NIE que otra persona registro
        /// mientras tanto), no se guarda ninguno.
        /// </summary>
        public void InsertarVarios(IReadOnlyList<Estudiante> estudiantes)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            conexion.Open();
            using var transaccion = conexion.BeginTransaction();

            const string sql = @"
                insert into Estudiantes (NIE, NombreCompleto, Grado, Seccion, Correo, EncargadoNombre, EncargadoCorreo)
                values (@NIE, @NombreCompleto, @Grado, @Seccion, @Correo, @EncargadoNombre, @EncargadoCorreo);
                select cast(scope_identity() as int);";

            try
            {
                foreach (var estudiante in estudiantes)
                {
                    using var comando = new SqlCommand(sql, conexion, transaccion);
                    comando.Parameters.AddWithValue("@NIE", estudiante.NIE);
                    comando.Parameters.AddWithValue("@NombreCompleto", estudiante.NombreCompleto);
                    comando.Parameters.AddWithValue("@Grado", estudiante.Grado);
                    comando.Parameters.AddWithValue("@Seccion", estudiante.Seccion);
                    comando.Parameters.AddWithValue("@Correo", (object?)estudiante.Correo ?? DBNull.Value);
                    AgregarEncargado(comando, estudiante);

                    estudiante.EstudianteId = (int)comando.ExecuteScalar();
                }

                transaccion.Commit();
            }
            catch
            {
                transaccion.Rollback();
                foreach (var estudiante in estudiantes) estudiante.EstudianteId = 0;
                throw;
            }
        }

        public HashSet<string> ObtenerTodosLosNie()
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = "select NIE from Estudiantes;";

            using var comando = new SqlCommand(sql, conexion);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            var nies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (lector.Read()) nies.Add(lector.GetString(0));
            return nies;
        }

        public void Actualizar(Estudiante estudiante)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                update Estudiantes
                set NombreCompleto = @NombreCompleto,
                    Grado = @Grado,
                    Seccion = @Seccion,
                    Correo = @Correo,
                    FotoRuta = @FotoRuta,
                    EncargadoNombre = @EncargadoNombre,
                    EncargadoCorreo = @EncargadoCorreo
                where EstudianteId = @EstudianteId;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@NombreCompleto", estudiante.NombreCompleto);
            comando.Parameters.AddWithValue("@Grado", estudiante.Grado);
            comando.Parameters.AddWithValue("@Seccion", estudiante.Seccion);
            comando.Parameters.AddWithValue("@Correo", (object?)estudiante.Correo ?? DBNull.Value);
            comando.Parameters.AddWithValue("@FotoRuta", (object?)estudiante.FotoRuta ?? DBNull.Value);
            comando.Parameters.AddWithValue("@EstudianteId", estudiante.EstudianteId);
            AgregarEncargado(comando, estudiante);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        public void DarDeBaja(int estudianteId) => CambiarEstadoActivo(estudianteId, activo: false);

        public void Reactivar(int estudianteId) => CambiarEstadoActivo(estudianteId, activo: true);

        public List<Estudiante> Listar(string? busqueda = null, string? seccion = null, bool incluirInactivos = false)
        {
            var resultado = new List<Estudiante>();

            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select EstudianteId, NIE, NombreCompleto, Grado, Seccion, Correo, FotoRuta, Activo, FechaRegistro,
                       EncargadoNombre, EncargadoCorreo
                from Estudiantes
                where (@Busqueda is null or NombreCompleto like '%' + @Busqueda + '%' or NIE like '%' + @Busqueda + '%')
                  and (@Seccion is null or Seccion = @Seccion)
                  and (@IncluirInactivos = 1 or Activo = 1)
                order by NombreCompleto;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Busqueda", string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda);
            comando.Parameters.AddWithValue("@Seccion", (object?)seccion ?? DBNull.Value);
            comando.Parameters.AddWithValue("@IncluirInactivos", incluirInactivos);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            while (lector.Read())
            {
                resultado.Add(LeerEstudiante(lector));
            }

            return resultado;
        }

        public int ContarActivos(string? seccion = null)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select count(*)
                from Estudiantes
                where Activo = 1
                  and (@Seccion is null or Seccion = @Seccion);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Seccion", (object?)seccion ?? DBNull.Value);

            conexion.Open();
            return (int)comando.ExecuteScalar();
        }

        public List<string> ObtenerSeccionesDisponibles()
        {
            var resultado = new List<string>();

            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = "select distinct Seccion from Estudiantes order by Seccion;";

            using var comando = new SqlCommand(sql, conexion);
            conexion.Open();
            using var lector = comando.ExecuteReader();

            while (lector.Read())
            {
                resultado.Add(lector.GetString(0));
            }

            return resultado;
        }

        public List<string> ObtenerGradosDisponibles()
        {
            var resultado = new List<string>();

            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = "select distinct Grado from Estudiantes order by Grado;";

            using var comando = new SqlCommand(sql, conexion);
            conexion.Open();
            using var lector = comando.ExecuteReader();

            while (lector.Read())
            {
                resultado.Add(lector.GetString(0));
            }

            return resultado;
        }

        private static void CambiarEstadoActivo(int estudianteId, bool activo)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = "update Estudiantes set Activo = @Activo where EstudianteId = @EstudianteId;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Activo", activo);
            comando.Parameters.AddWithValue("@EstudianteId", estudianteId);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        private static void AgregarEncargado(SqlCommand comando, Estudiante estudiante)
        {
            comando.Parameters.AddWithValue("@EncargadoNombre", (object?)estudiante.EncargadoNombre ?? DBNull.Value);
            comando.Parameters.AddWithValue("@EncargadoCorreo", (object?)estudiante.EncargadoCorreo ?? DBNull.Value);
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
                FechaRegistro = lector.GetDateTime(8),
                EncargadoNombre = lector.IsDBNull(9) ? null : lector.GetString(9),
                EncargadoCorreo = lector.IsDBNull(10) ? null : lector.GetString(10)
            };
        }
    }
}
