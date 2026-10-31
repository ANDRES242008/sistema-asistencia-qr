using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Bitacora de auditoria: deja constancia de quien hizo cada accion
    /// importante y cuando. Se llama desde los servicios (no desde las
    /// pantallas), asi queda registrado sin importar desde donde se
    /// hizo la accion. Si la bitacora falla, la accion NO se cancela:
    /// el error se manda a errores.log.
    /// </summary>
    public static class BitacoraService
    {
        public const string UsuarioSistema = "Sistema";

        // Acciones (texto que ve el director en el filtro).
        public const string InicioSesion = "Inicio de sesion";
        public const string InicioSesionFallido = "Inicio de sesion fallido";
        public const string CuentaBloqueada = "Cuenta bloqueada";
        public const string UsuarioCreado = "Usuario creado";
        public const string UsuarioEditado = "Usuario editado";
        public const string UsuarioDesactivado = "Usuario desactivado";
        public const string UsuarioReactivado = "Usuario reactivado";
        public const string ContrasenaReseteada = "Contrasena reseteada";
        public const string ContrasenaCambiada = "Contrasena cambiada";
        public const string EstudianteCreado = "Estudiante creado";
        public const string EstudianteEditado = "Estudiante editado";
        public const string EstudianteDadoDeBaja = "Estudiante dado de baja";
        public const string EstudianteReactivado = "Estudiante reactivado";
        public const string EstudiantesImportados = "Estudiantes importados";
        public const string QrRegenerado = "QR regenerado";
        public const string AsistenciaManual = "Asistencia manual";
        public const string HorarioCambiado = "Horario cambiado";
        public const string RespaldoCreado = "Respaldo creado";
        public const string RespaldoRestaurado = "Respaldo restaurado";

        private const int LargoMaximoDetalle = 500;
        private const int LargoMaximoUsuario = 50;

        /// <summary>Program.cs lo conecta con errores.log.</summary>
        public static Action<Exception>? RegistrarError { get; set; }

        private static readonly BitacoraRepository _repositorio = new();

        /// <param name="usuario">Quien hizo la accion. Por defecto, quien inicio sesion.</param>
        public static void Registrar(string accion, string detalle, string? usuario = null)
        {
            try
            {
                _repositorio.Insertar(
                    Recortar(usuario ?? SesionActual.NombreUsuario ?? UsuarioSistema, LargoMaximoUsuario),
                    accion,
                    Recortar(detalle, LargoMaximoDetalle));
            }
            catch (Exception ex)
            {
                RegistrarError?.Invoke(ex);
            }
        }

        public static List<RegistroBitacora> Listar(DateTime desde, DateTime hasta, string? usuario, string? accion, string? texto) =>
            _repositorio.Listar(desde, hasta, usuario, accion, texto);

        public static List<string> ObtenerUsuarios() => _repositorio.ObtenerUsuarios();

        public static List<string> ObtenerAcciones() => _repositorio.ObtenerAcciones();

        /// <summary>"NIE 12345678 · Valeria Martinez · B2DA A", para identificar al estudiante.</summary>
        internal static string DescribirEstudiante(Estudiante estudiante) =>
            $"NIE {estudiante.NIE} · {estudiante.NombreCompleto} · {estudiante.Grado} {estudiante.Seccion}";

        /// <summary>Lista solo lo que cambio: "Nombre: Ana -> Ana Maria; Rol: Docente -> Administrador".</summary>
        internal static string DescribirCambios(params (string Campo, string? Antes, string? Despues)[] campos)
        {
            var cambios = campos
                .Where(c => !string.Equals(c.Antes ?? string.Empty, c.Despues ?? string.Empty, StringComparison.Ordinal))
                .Select(c => $"{c.Campo}: {Vacio(c.Antes)} -> {Vacio(c.Despues)}")
                .ToList();

            return cambios.Count == 0 ? "sin cambios" : string.Join("; ", cambios);
        }

        internal static string Recortar(string texto, int largo) =>
            texto.Length <= largo ? texto : texto[..(largo - 3)] + "...";

        private static string Vacio(string? valor) => string.IsNullOrEmpty(valor) ? "(vacio)" : valor;
    }
}
