using System.Net;
using System.Net.Mail;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>Manda un correo. Separado para poder probar sin internet.</summary>
    public interface IEnviadorCorreo
    {
        void Enviar(ConfiguracionCorreo config, string para, string asunto, string cuerpoHtml);
    }

    public class EnviadorSmtp : IEnviadorCorreo
    {
        public void Enviar(ConfiguracionCorreo config, string para, string asunto, string cuerpoHtml)
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(config.Usuario, new InstitucionService().Obtener().Nombre),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            mensaje.To.Add(para);

            using var cliente = new SmtpClient(config.ServidorSmtp, config.Puerto)
            {
                EnableSsl = config.UsarSsl,
                Credentials = new NetworkCredential(config.Usuario, config.Contrasena),
                Timeout = 30_000
            };
            cliente.Send(mensaje);
        }
    }

    public record ResumenEnvio(int Enviados, int Fallidos, string? PrimerError);

    /// <summary>
    /// Avisos por correo a los encargados de los estudiantes que llegaron
    /// tarde o no llegaron. Una vez al dia, a la hora configurada, los
    /// manda la computadora elegida (la que tiene la contrasena). Nunca
    /// se manda dos veces el aviso del mismo estudiante el mismo dia.
    /// </summary>
    public class AvisosService
    {
        private readonly AvisosRepository _repositorio = new();
        private readonly IEnviadorCorreo _enviador;

        public AvisosService(IEnviadorCorreo? enviador = null)
        {
            _enviador = enviador ?? new EnviadorSmtp();
        }

        public static string EquipoActual => Environment.MachineName;

        public ConfiguracionCorreo ObtenerConfiguracion() =>
            _repositorio.ObtenerConfiguracion() ?? new ConfiguracionCorreo { EquipoQueEnvia = EquipoActual };

        public ResultadoOperacion GuardarConfiguracion(ConfiguracionCorreo config)
        {
            var validacion = Validar(config);
            if (!validacion.Exitoso) return validacion;

            config.EquipoQueEnvia = EquipoActual;
            _repositorio.GuardarConfiguracion(config);

            BitacoraService.Registrar(BitacoraService.AvisosConfigurados,
                $"{(config.Activo ? "Activados" : "Desactivados")} · {config.HoraEnvio:hh\\:mm} · " +
                $"{(config.AvisarTardanzas ? "tardanzas" : "")}{(config.AvisarTardanzas && config.AvisarAusencias ? " y " : "")}" +
                $"{(config.AvisarAusencias ? "ausencias" : "")} · desde {config.Usuario} · envia {EquipoActual}");

            return ResultadoOperacion.Ok("Configuracion guardada.");
        }

        /// <summary>Manda un correo de prueba para confirmar que la configuracion funciona.</summary>
        public ResultadoOperacion Probar(ConfiguracionCorreo config, string destino)
        {
            var validacion = Validar(config);
            if (!validacion.Exitoso) return validacion;
            if (!EstudianteService.EsCorreoValido(destino))
            {
                return ResultadoOperacion.Error("Escribe un correo de destino valido para la prueba.");
            }

            try
            {
                string institucion = new InstitucionService().Obtener().Nombre;
                _enviador.Enviar(config, destino.Trim(), $"[{institucion}] Correo de prueba",
                    Plantilla(institucion, "<p>Si recibiste este mensaje, los avisos de asistencia estan bien configurados.</p>"));
            }
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
            {
                return ResultadoOperacion.Error(ExplicarError(ex));
            }

            return ResultadoOperacion.Ok($"Correo de prueba enviado a {destino.Trim()}. Revisa la bandeja de entrada (y la de spam).");
        }

        public List<AvisoAsistencia> ObtenerPendientes(DateTime fecha, ConfiguracionCorreo config) =>
            _repositorio.ObtenerPendientes(fecha, config.AvisarTardanzas, config.AvisarAusencias);

        public List<RegistroAviso> ListarHistorial(DateTime desde, DateTime hasta) => _repositorio.ListarHistorial(desde, hasta);

        /// <summary>Manda los avisos pendientes del dia. Los que fallen se pueden reintentar despues.</summary>
        public ResumenEnvio EnviarPendientes(DateTime fecha, string origen)
        {
            var config = ObtenerConfiguracion();
            if (string.IsNullOrEmpty(config.Contrasena))
            {
                return new ResumenEnvio(0, 0,
                    "La contrasena del correo no esta guardada en esta computadora. Abre Avisos a padres y guardala de nuevo.");
            }

            string institucion = new InstitucionService().Obtener().Nombre;
            int enviados = 0, fallidos = 0;
            string? primerError = null;

            foreach (var aviso in ObtenerPendientes(fecha, config))
            {
                var (asunto, cuerpo) = ConstruirMensaje(aviso, institucion, config.HoraEnvio);
                try
                {
                    _enviador.Enviar(config, aviso.Estudiante.EncargadoCorreo!, asunto, cuerpo);
                    _repositorio.RegistrarEnvio(aviso, enviado: true, error: null);
                    enviados++;
                }
                catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
                {
                    string error = ExplicarError(ex);
                    _repositorio.RegistrarEnvio(aviso, enviado: false, error: BitacoraService.Recortar(error, 400));
                    primerError ??= error;
                    fallidos++;
                }
            }

            if (enviados + fallidos > 0)
            {
                BitacoraService.Registrar(BitacoraService.AvisosEnviados,
                    $"{origen}: {enviados} enviados" + (fallidos > 0 ? $", {fallidos} con error ({primerError})" : string.Empty));
            }

            return new ResumenEnvio(enviados, fallidos, primerError);
        }

        /// <summary>
        /// Lo llama un temporizador cada minuto. Si ya es la hora y esta
        /// es la computadora que envia, manda los avisos del dia (una sola
        /// vez). Nunca lanza excepciones.
        /// </summary>
        public static void EnviarAutomaticoSiCorresponde(Action<Exception> registrarError)
        {
            try
            {
                var servicio = new AvisosService();
                var config = servicio._repositorio.ObtenerConfiguracion();
                var ahora = DateTime.Now;
                if (config is null || !DebeEnviarAutomatico(config, ahora, EquipoActual)) return;

                // Se marca antes de enviar: si algo falla a medias, no se
                // repite cada minuto; lo pendiente se puede mandar a mano.
                servicio._repositorio.MarcarEnvioAutomatico(ahora);
                servicio.EnviarPendientes(ahora, "Envio automatico");
            }
            catch (Exception ex)
            {
                registrarError(ex);
            }
        }

        internal static bool DebeEnviarAutomatico(ConfiguracionCorreo config, DateTime ahora, string equipo) =>
            config.Activo
            && string.Equals(config.EquipoQueEnvia, equipo, StringComparison.OrdinalIgnoreCase)
            && ahora.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
            && ahora.TimeOfDay >= config.HoraEnvio
            && config.UltimoEnvioAutomatico?.Date != ahora.Date;

        internal static ResultadoOperacion Validar(ConfiguracionCorreo config)
        {
            if (string.IsNullOrWhiteSpace(config.ServidorSmtp))
            {
                return ResultadoOperacion.Error("Escribe el servidor de correo (por ejemplo, smtp.gmail.com).");
            }

            if (config.Puerto is < 1 or > 65535)
            {
                return ResultadoOperacion.Error("El puerto no es valido (normalmente 587).");
            }

            if (!EstudianteService.EsCorreoValido(config.Usuario))
            {
                return ResultadoOperacion.Error("El correo del colegio (usuario) no es valido.");
            }

            if (string.IsNullOrEmpty(config.Contrasena))
            {
                return ResultadoOperacion.Error("Escribe la contrasena del correo (en Gmail, una contrasena de aplicacion).");
            }

            if (!config.AvisarTardanzas && !config.AvisarAusencias)
            {
                return ResultadoOperacion.Error("Elige al menos un tipo de aviso: tardanzas o ausencias.");
            }

            return ResultadoOperacion.Ok();
        }

        /// <summary>Asunto y cuerpo (HTML) del aviso para el encargado.</summary>
        internal static (string Asunto, string CuerpoHtml) ConstruirMensaje(AvisoAsistencia aviso, string institucion, TimeSpan horaCorte)
        {
            var e = aviso.Estudiante;
            string Html(string? texto) => WebUtility.HtmlEncode(texto ?? string.Empty);

            string saludo = string.IsNullOrWhiteSpace(e.EncargadoNombre)
                ? "Estimado(a) encargado(a):"
                : $"Estimado(a) {Html(e.EncargadoNombre!.Trim())}:";
            string fecha = aviso.Fecha.ToString("dd/MM/yyyy");
            string estudiante = $"<strong>{Html(e.NombreCompleto)}</strong> ({Html(e.Grado)} {Html(e.Seccion)})";

            string asunto, detalle;
            if (aviso.Tipo == TipoAviso.Tardanza)
            {
                string hora = aviso.HoraLlegada is { } h ? DateTime.Today.Add(h).ToString("hh:mm tt") : "-";
                asunto = $"[{institucion}] Llegada tarde: {e.NombreCompleto}";
                detalle = $"<p>Le informamos que {estudiante} registro su llegada hoy {fecha} a las <strong>{hora}</strong>, despues de la hora de entrada.</p>";
            }
            else
            {
                string corte = DateTime.Today.Add(horaCorte).ToString("hh:mm tt");
                asunto = $"[{institucion}] Ausencia: {e.NombreCompleto}";
                detalle = $"<p>Le informamos que {estudiante} no ha registrado su asistencia hoy {fecha} hasta las <strong>{corte}</strong>.</p>" +
                          "<p>Si su hijo(a) se encuentra en la institucion o la ausencia esta justificada, puede ignorar este mensaje o comunicarse con nosotros.</p>";
            }

            return (asunto, Plantilla(institucion, $"<p>{saludo}</p>{detalle}"));
        }

        private static string Plantilla(string institucion, string contenido) =>
            "<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1E293B;max-width:560px\">" +
            $"<div style=\"background:#1B2A4A;color:#fff;padding:14px 18px;font-size:16px;font-weight:bold\">{WebUtility.HtmlEncode(institucion)}</div>" +
            $"<div style=\"padding:16px 18px\">{contenido}" +
            "<p style=\"color:#64748B;font-size:12px;margin-top:24px\">Mensaje automatico del sistema de asistencia AsistenciaQR. Por favor no responda a este correo.</p>" +
            "</div></div>";

        private static string ExplicarError(Exception ex)
        {
            string detalle = ex.InnerException?.Message ?? ex.Message;
            if (ex.InnerException is System.Net.Sockets.SocketException or IOException)
            {
                return "No se pudo conectar con el servidor de correo. Revisa el servidor, el puerto y la conexion a internet. Detalle: " + detalle;
            }
            if (detalle.Contains("5.7.") || detalle.Contains("Authentication", StringComparison.OrdinalIgnoreCase))
            {
                return "El servidor rechazo el usuario o la contrasena. En Gmail usa una contrasena de aplicacion. Detalle: " + detalle;
            }
            return detalle;
        }
    }
}
