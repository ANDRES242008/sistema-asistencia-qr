using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Logica de autenticacion y de administracion de cuentas.
    /// La contrasena NUNCA se guarda en texto plano: se genera una sal
    /// aleatoria por usuario y se deriva un hash con PBKDF2 (estandar
    /// de la industria). Al validar el login, se compara con una
    /// tecnica de tiempo fijo para no dar pistas por diferencias de
    /// tiempo de respuesta. Tras varios intentos fallidos la cuenta
    /// se bloquea unos minutos.
    /// </summary>
    public class AuthService
    {
        public const string RolAdministrador = "Administrador";
        public const string RolDocente = "Docente";
        public const int LongitudMinimaContrasena = 8;
        public const int IntentosAntesDeBloquear = 5;
        public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

        private const int Iteraciones = 100_000;
        private const int TamanoHash = 32;
        private const int TamanoSal = 16;

        private const string MensajeCredencialesIncorrectas = "Usuario o contrasena incorrectos.";

        private readonly UsuarioRepository _usuarioRepository = new();

        /// <summary>
        /// True si la base de datos todavia no tiene ninguna cuenta.
        /// Program.cs lo usa para mostrar el asistente de primer uso.
        /// </summary>
        public bool NecesitaPrimerUsuario() => _usuarioRepository.ContarUsuarios() == 0;

        public ResultadoOperacion Registrar(string nombreUsuario, string nombreCompleto, string contrasena, string rol,
            bool debeCambiarContrasena = false)
        {
            var validacion = ValidarRegistro(nombreUsuario, nombreCompleto, contrasena, rol);
            if (!validacion.Exitoso) return validacion;

            var (hash, sal) = CrearHash(contrasena);

            var usuario = new Usuario
            {
                NombreUsuario = nombreUsuario.Trim(),
                NombreCompleto = nombreCompleto.Trim(),
                ContrasenaHash = hash,
                ContrasenaSal = sal,
                Rol = rol,
                DebeCambiarContrasena = debeCambiarContrasena
            };

            try
            {
                _usuarioRepository.Insertar(usuario);
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                return ResultadoOperacion.Error("Ya existe un usuario con ese nombre.");
            }

            return ResultadoOperacion.Ok("Usuario creado correctamente.");
        }

        public ResultadoLogin IniciarSesion(string nombreUsuario, string contrasena)
        {
            var ahora = DateTime.Now;
            var usuario = _usuarioRepository.ObtenerPorNombreUsuario(nombreUsuario);
            if (usuario is null || !usuario.Activo)
            {
                return new ResultadoLogin { Mensaje = MensajeCredencialesIncorrectas };
            }

            // Mientras dure el bloqueo ni siquiera se revisa la
            // contrasena: asi no se puede seguir adivinando.
            if (EstaBloqueado(usuario, ahora))
            {
                return new ResultadoLogin { Mensaje = MensajeBloqueo(usuario.BloqueadoHasta!.Value) };
            }

            if (!VerificarContrasena(contrasena, usuario.ContrasenaHash, usuario.ContrasenaSal))
            {
                var (intentos, bloqueadoHasta) = CalcularIntentoFallido(usuario.IntentosFallidos, ahora);
                _usuarioRepository.ActualizarIntentos(usuario.UsuarioId, intentos, bloqueadoHasta);

                return new ResultadoLogin
                {
                    Mensaje = bloqueadoHasta is { } hasta ? MensajeBloqueo(hasta) : MensajeCredencialesIncorrectas
                };
            }

            if (usuario.IntentosFallidos > 0 || usuario.BloqueadoHasta is not null)
            {
                _usuarioRepository.ActualizarIntentos(usuario.UsuarioId, 0, null);
            }

            return new ResultadoLogin { Usuario = usuario };
        }

        public List<Usuario> ListarUsuarios(string? texto, bool incluirInactivos) =>
            _usuarioRepository.Listar(texto, incluirInactivos);

        public ResultadoOperacion ActualizarDatos(int usuarioId, string nombreCompleto, string rol)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return ResultadoOperacion.Error("Escribe el nombre completo.");
            }

            if (!EsRolValido(rol))
            {
                return ResultadoOperacion.Error("El rol debe ser Administrador o Docente.");
            }

            var usuario = _usuarioRepository.ObtenerPorId(usuarioId);
            if (usuario is null) return ResultadoOperacion.Error("El usuario ya no existe.");

            if (!PuedeQuitarAdministrador(usuario, rol, usuario.Activo, _usuarioRepository.ContarAdministradoresActivos()))
            {
                return ResultadoOperacion.Error("Debe quedar al menos un Administrador activo.");
            }

            _usuarioRepository.ActualizarDatos(usuarioId, nombreCompleto.Trim(), rol);
            return ResultadoOperacion.Ok("Cambios guardados correctamente.");
        }

        public ResultadoOperacion Desactivar(int usuarioId, string? nombreUsuarioActual)
        {
            var usuario = _usuarioRepository.ObtenerPorId(usuarioId);
            if (usuario is null) return ResultadoOperacion.Error("El usuario ya no existe.");

            if (string.Equals(usuario.NombreUsuario, nombreUsuarioActual, StringComparison.OrdinalIgnoreCase))
            {
                return ResultadoOperacion.Error("No puedes desactivar tu propia cuenta.");
            }

            if (!PuedeQuitarAdministrador(usuario, usuario.Rol, nuevoActivo: false, _usuarioRepository.ContarAdministradoresActivos()))
            {
                return ResultadoOperacion.Error("Debe quedar al menos un Administrador activo.");
            }

            _usuarioRepository.CambiarEstado(usuarioId, activo: false);
            return ResultadoOperacion.Ok("Usuario desactivado. Ya no podra iniciar sesion.");
        }

        public ResultadoOperacion Reactivar(int usuarioId)
        {
            _usuarioRepository.CambiarEstado(usuarioId, activo: true);
            return ResultadoOperacion.Ok("Usuario reactivado.");
        }

        /// <summary>
        /// El Administrador asigna una contrasena temporal. La cuenta
        /// queda desbloqueada y el usuario debera cambiarla al entrar.
        /// </summary>
        public ResultadoOperacion ResetearContrasena(int usuarioId, string contrasenaTemporal)
        {
            if (!EsContrasenaValida(contrasenaTemporal))
            {
                return ResultadoOperacion.Error($"La contrasena debe tener al menos {LongitudMinimaContrasena} caracteres.");
            }

            var (hash, sal) = CrearHash(contrasenaTemporal);
            _usuarioRepository.ActualizarContrasena(usuarioId, hash, sal, debeCambiar: true);
            return ResultadoOperacion.Ok("Contrasena reseteada. El usuario debera cambiarla al iniciar sesion.");
        }

        /// <summary>
        /// Cambio de contrasena hecho por el propio usuario. Si
        /// contrasenaActual es null (cambio obligatorio justo despues
        /// del login) no se vuelve a pedir.
        /// </summary>
        public ResultadoOperacion CambiarContrasena(int usuarioId, string? contrasenaActual, string nueva, string confirmacion)
        {
            var usuario = _usuarioRepository.ObtenerPorId(usuarioId);
            if (usuario is null) return ResultadoOperacion.Error("El usuario ya no existe.");

            if (contrasenaActual is not null &&
                !VerificarContrasena(contrasenaActual, usuario.ContrasenaHash, usuario.ContrasenaSal))
            {
                return ResultadoOperacion.Error("La contrasena actual no es correcta.");
            }

            var validacion = ValidarNuevaContrasena(nueva, confirmacion);
            if (!validacion.Exitoso) return validacion;

            if (VerificarContrasena(nueva, usuario.ContrasenaHash, usuario.ContrasenaSal))
            {
                return ResultadoOperacion.Error("La nueva contrasena debe ser distinta de la anterior.");
            }

            var (hash, sal) = CrearHash(nueva);
            _usuarioRepository.ActualizarContrasena(usuarioId, hash, sal, debeCambiar: false);
            return ResultadoOperacion.Ok("Contrasena actualizada correctamente.");
        }

        /// <summary>
        /// Contrasena temporal facil de dictar: sin letras o numeros
        /// que se confunden entre si (0/O, 1/l/I).
        /// </summary>
        public static string GenerarContrasenaTemporal(int longitud = 10)
        {
            const string caracteres = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789";
            return RandomNumberGenerator.GetString(caracteres, Math.Max(longitud, LongitudMinimaContrasena));
        }

        internal static ResultadoOperacion ValidarRegistro(string nombreUsuario, string nombreCompleto, string contrasena, string rol)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(contrasena))
            {
                return ResultadoOperacion.Error("Usuario y contrasena son obligatorios.");
            }

            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return ResultadoOperacion.Error("Escribe el nombre completo.");
            }

            if (!EsContrasenaValida(contrasena))
            {
                return ResultadoOperacion.Error($"La contrasena debe tener al menos {LongitudMinimaContrasena} caracteres.");
            }

            if (!EsRolValido(rol))
            {
                return ResultadoOperacion.Error("El rol debe ser Administrador o Docente.");
            }

            return ResultadoOperacion.Ok();
        }

        internal static ResultadoOperacion ValidarNuevaContrasena(string nueva, string confirmacion)
        {
            if (!EsContrasenaValida(nueva))
            {
                return ResultadoOperacion.Error($"La contrasena debe tener al menos {LongitudMinimaContrasena} caracteres.");
            }

            if (nueva != confirmacion)
            {
                return ResultadoOperacion.Error("Las contrasenas no coinciden.");
            }

            return ResultadoOperacion.Ok();
        }

        internal static bool EstaBloqueado(Usuario usuario, DateTime ahora) =>
            usuario.BloqueadoHasta is { } hasta && hasta > ahora;

        /// <summary>
        /// Suma un intento fallido. Al llegar al limite, la cuenta se
        /// bloquea y el contador vuelve a cero para la proxima ronda.
        /// </summary>
        internal static (int IntentosFallidos, DateTime? BloqueadoHasta) CalcularIntentoFallido(int intentosActuales, DateTime ahora)
        {
            int intentos = intentosActuales + 1;
            return intentos >= IntentosAntesDeBloquear
                ? (0, ahora + DuracionBloqueo)
                : (intentos, null);
        }

        /// <summary>
        /// Impide dejar el sistema sin ningun Administrador activo:
        /// no se puede degradar ni desactivar al ultimo que queda.
        /// </summary>
        internal static bool PuedeQuitarAdministrador(Usuario usuario, string nuevoRol, bool nuevoActivo, int administradoresActivos)
        {
            bool eraAdministradorActivo = usuario.Activo && usuario.Rol == RolAdministrador;
            bool sigueSiendoAdministradorActivo = nuevoActivo && nuevoRol == RolAdministrador;

            if (!eraAdministradorActivo || sigueSiendoAdministradorActivo) return true;

            return administradoresActivos > 1;
        }

        internal static (string Hash, string Sal) CrearHash(string contrasena)
        {
            byte[] sal = RandomNumberGenerator.GetBytes(TamanoSal);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(contrasena), sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);

            return (Convert.ToBase64String(hash), Convert.ToBase64String(sal));
        }

        internal static bool VerificarContrasena(string contrasena, string hashGuardadoBase64, string salBase64)
        {
            byte[] sal = Convert.FromBase64String(salBase64);
            byte[] hashIngresado = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(contrasena), sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
            byte[] hashGuardado = Convert.FromBase64String(hashGuardadoBase64);

            return CryptographicOperations.FixedTimeEquals(hashIngresado, hashGuardado);
        }

        private static bool EsContrasenaValida(string contrasena) =>
            !string.IsNullOrEmpty(contrasena) && contrasena.Length >= LongitudMinimaContrasena;

        private static bool EsRolValido(string rol) => rol is RolAdministrador or RolDocente;

        private static string MensajeBloqueo(DateTime hasta) =>
            $"Cuenta bloqueada por demasiados intentos fallidos. Intenta de nuevo a las {hasta:HH:mm}.";
    }
}
