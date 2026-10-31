using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Logica de autenticacion. La contrasena NUNCA se guarda en
    /// texto plano: se genera una sal aleatoria por usuario y se
    /// deriva un hash con PBKDF2 (estandar de la industria). Al
    /// validar el login, se compara con una tecnica de tiempo fijo
    /// para no dar pistas por diferencias de tiempo de respuesta.
    /// </summary>
    public class AuthService
    {
        public const string RolAdministrador = "Administrador";
        public const string RolDocente = "Docente";
        public const int LongitudMinimaContrasena = 8;

        private const int Iteraciones = 100_000;
        private const int TamanoHash = 32;
        private const int TamanoSal = 16;

        private readonly UsuarioRepository _usuarioRepository = new();

        /// <summary>
        /// True si la base de datos todavia no tiene ninguna cuenta.
        /// Program.cs lo usa para mostrar el asistente de primer uso.
        /// </summary>
        public bool NecesitaPrimerUsuario() => _usuarioRepository.ContarUsuarios() == 0;

        public ResultadoOperacion Registrar(string nombreUsuario, string nombreCompleto, string contrasena, string rol)
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
                Rol = rol
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

        public Usuario? ValidarLogin(string nombreUsuario, string contrasena)
        {
            var usuario = _usuarioRepository.ObtenerPorNombreUsuario(nombreUsuario);
            if (usuario is null || !usuario.Activo) return null;

            return VerificarContrasena(contrasena, usuario.ContrasenaHash, usuario.ContrasenaSal) ? usuario : null;
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

            if (contrasena.Length < LongitudMinimaContrasena)
            {
                return ResultadoOperacion.Error($"La contrasena debe tener al menos {LongitudMinimaContrasena} caracteres.");
            }

            if (rol is not (RolAdministrador or RolDocente))
            {
                return ResultadoOperacion.Error("El rol debe ser Administrador o Docente.");
            }

            return ResultadoOperacion.Ok();
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
    }
}
