using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

namespace AsistenciaQR.Repositorios
{
    public class AvisosRepository
    {
        // La contrasena del correo se cifra con DPAPI (LocalMachine):
        // solo esta computadora puede descifrarla.
        private static readonly byte[] Entropia = Encoding.UTF8.GetBytes("AsistenciaQR.Correo");

        public ConfiguracionCorreo? ObtenerConfiguracion()
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select Activo, HoraEnvio, AvisarTardanzas, AvisarAusencias, ServidorSmtp, Puerto, UsarSsl,
                       Usuario, ContrasenaCifrada, EquipoQueEnvia, UltimoEnvioAutomatico
                from ConfiguracionCorreo where ConfiguracionCorreoId = 1;";

            using var comando = new SqlCommand(sql, conexion);
            conexion.Open();
            using var lector = comando.ExecuteReader();
            if (!lector.Read()) return null;

            return new ConfiguracionCorreo
            {
                Activo = lector.GetBoolean(0),
                HoraEnvio = lector.GetTimeSpan(1),
                AvisarTardanzas = lector.GetBoolean(2),
                AvisarAusencias = lector.GetBoolean(3),
                ServidorSmtp = lector.GetString(4),
                Puerto = lector.GetInt32(5),
                UsarSsl = lector.GetBoolean(6),
                Usuario = lector.GetString(7),
                Contrasena = lector.IsDBNull(8) ? null : Descifrar((byte[])lector[8]),
                EquipoQueEnvia = lector.GetString(9),
                UltimoEnvioAutomatico = lector.IsDBNull(10) ? null : lector.GetDateTime(10)
            };
        }

        public void GuardarConfiguracion(ConfiguracionCorreo config)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                update ConfiguracionCorreo
                set Activo = @Activo, HoraEnvio = @Hora, AvisarTardanzas = @Tardanzas, AvisarAusencias = @Ausencias,
                    ServidorSmtp = @Servidor, Puerto = @Puerto, UsarSsl = @Ssl, Usuario = @Usuario,
                    ContrasenaCifrada = @Contrasena, EquipoQueEnvia = @Equipo
                where ConfiguracionCorreoId = 1;

                if @@rowcount = 0
                    insert into ConfiguracionCorreo (ConfiguracionCorreoId, Activo, HoraEnvio, AvisarTardanzas, AvisarAusencias,
                        ServidorSmtp, Puerto, UsarSsl, Usuario, ContrasenaCifrada, EquipoQueEnvia)
                    values (1, @Activo, @Hora, @Tardanzas, @Ausencias, @Servidor, @Puerto, @Ssl, @Usuario, @Contrasena, @Equipo);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Activo", config.Activo);
            comando.Parameters.AddWithValue("@Hora", config.HoraEnvio);
            comando.Parameters.AddWithValue("@Tardanzas", config.AvisarTardanzas);
            comando.Parameters.AddWithValue("@Ausencias", config.AvisarAusencias);
            comando.Parameters.AddWithValue("@Servidor", config.ServidorSmtp);
            comando.Parameters.AddWithValue("@Puerto", config.Puerto);
            comando.Parameters.AddWithValue("@Ssl", config.UsarSsl);
            comando.Parameters.AddWithValue("@Usuario", config.Usuario);
            comando.Parameters.Add("@Contrasena", System.Data.SqlDbType.VarBinary, -1).Value =
                string.IsNullOrEmpty(config.Contrasena) ? DBNull.Value : Cifrar(config.Contrasena);
            comando.Parameters.AddWithValue("@Equipo", config.EquipoQueEnvia);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        public void MarcarEnvioAutomatico(DateTime fecha)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            using var comando = new SqlCommand(
                "update ConfiguracionCorreo set UltimoEnvioAutomatico = @Fecha where ConfiguracionCorreoId = 1;", conexion);
            comando.Parameters.AddWithValue("@Fecha", fecha.Date);
            conexion.Open();
            comando.ExecuteNonQuery();
        }

        /// <summary>
        /// Estudiantes activos con correo de encargado que hoy llegaron
        /// tarde o no registraron asistencia, y a cuyo encargado todavia
        /// no se le ha mandado el aviso del dia.
        /// </summary>
        public List<AvisoAsistencia> ObtenerPendientes(DateTime fecha, bool tardanzas, bool ausencias)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select e.EstudianteId, e.NIE, e.NombreCompleto, e.Grado, e.Seccion, e.EncargadoNombre, e.EncargadoCorreo,
                       a.Hora, a.EstadoPuntualidad
                from Estudiantes e
                left join Asistencia a on a.EstudianteId = e.EstudianteId and a.Fecha = @Fecha
                where e.Activo = 1
                  and e.FechaRegistro < dateadd(day, 1, @Fecha)
                  and isnull(e.EncargadoCorreo, '') <> ''
                  and ((@Ausencias = 1 and a.AsistenciaId is null) or (@Tardanzas = 1 and a.EstadoPuntualidad = 'Tarde'))
                  and not exists (select 1 from NotificacionesEnviadas n
                                  where n.EstudianteId = e.EstudianteId and n.Fecha = @Fecha and n.Enviado = 1)
                order by e.Grado, e.Seccion, e.NombreCompleto;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Fecha", fecha.Date);
            comando.Parameters.AddWithValue("@Tardanzas", tardanzas);
            comando.Parameters.AddWithValue("@Ausencias", ausencias);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            var avisos = new List<AvisoAsistencia>();
            while (lector.Read())
            {
                bool vino = !lector.IsDBNull(7);
                avisos.Add(new AvisoAsistencia
                {
                    Fecha = fecha.Date,
                    Tipo = vino ? TipoAviso.Tardanza : TipoAviso.Ausencia,
                    HoraLlegada = vino ? lector.GetTimeSpan(7) : null,
                    Estudiante = new Estudiante
                    {
                        EstudianteId = lector.GetInt32(0),
                        NIE = lector.GetString(1),
                        NombreCompleto = lector.GetString(2),
                        Grado = lector.GetString(3),
                        Seccion = lector.GetString(4),
                        EncargadoNombre = lector.IsDBNull(5) ? null : lector.GetString(5),
                        EncargadoCorreo = lector.GetString(6),
                        Activo = true
                    }
                });
            }
            return avisos;
        }

        /// <summary>Guarda el resultado de un envio (reemplaza un intento fallido anterior del mismo dia).</summary>
        public void RegistrarEnvio(AvisoAsistencia aviso, bool enviado, string? error)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                update NotificacionesEnviadas
                set Tipo = @Tipo, Correo = @Correo, Enviado = @Enviado, Error = @Error, FechaIntento = getdate()
                where EstudianteId = @EstudianteId and Fecha = @Fecha;

                if @@rowcount = 0
                    insert into NotificacionesEnviadas (Fecha, EstudianteId, Tipo, Correo, Enviado, Error)
                    values (@Fecha, @EstudianteId, @Tipo, @Correo, @Enviado, @Error);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Fecha", aviso.Fecha.Date);
            comando.Parameters.AddWithValue("@EstudianteId", aviso.Estudiante.EstudianteId);
            comando.Parameters.AddWithValue("@Tipo", aviso.Tipo == TipoAviso.Tardanza ? "Tardanza" : "Ausencia");
            comando.Parameters.AddWithValue("@Correo", aviso.Estudiante.EncargadoCorreo ?? string.Empty);
            comando.Parameters.AddWithValue("@Enviado", enviado);
            comando.Parameters.AddWithValue("@Error", (object?)error ?? DBNull.Value);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        public List<RegistroAviso> ListarHistorial(DateTime desde, DateTime hasta)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select top (1000) n.Fecha, e.NombreCompleto, n.Tipo, n.Correo, n.Enviado, n.Error, n.FechaIntento
                from NotificacionesEnviadas n
                join Estudiantes e on e.EstudianteId = n.EstudianteId
                where n.Fecha >= @Desde and n.Fecha <= @Hasta
                order by n.FechaIntento desc;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@Desde", desde.Date);
            comando.Parameters.AddWithValue("@Hasta", hasta.Date);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            var registros = new List<RegistroAviso>();
            while (lector.Read())
            {
                registros.Add(new RegistroAviso
                {
                    Fecha = lector.GetDateTime(0),
                    Estudiante = lector.GetString(1),
                    Tipo = lector.GetString(2),
                    Correo = lector.GetString(3),
                    Enviado = lector.GetBoolean(4),
                    Error = lector.IsDBNull(5) ? null : lector.GetString(5),
                    FechaIntento = lector.GetDateTime(6)
                });
            }
            return registros;
        }

        internal static byte[] Cifrar(string texto) =>
            ProtectedData.Protect(Encoding.UTF8.GetBytes(texto), Entropia, DataProtectionScope.LocalMachine);

        /// <summary>Null si la contrasena se guardo en otra computadora.</summary>
        internal static string? Descifrar(byte[] cifrado)
        {
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(cifrado, Entropia, DataProtectionScope.LocalMachine));
            }
            catch (CryptographicException)
            {
                return null;
            }
        }
    }
}
