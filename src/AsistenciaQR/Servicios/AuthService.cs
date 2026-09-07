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
        private const int Iteraciones = 100_000;
        private const int TamanoHash = 32;
        private const int TamanoSal = 16;

        private readonly UsuarioRepository _usuarioRepository = new();

        public ResultadoOperacion Registrar(string nombreUsuario, string nombreCompleto, string contrasena, string rol)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(contrasena))
            {
                return ResultadoOperacion.Error("Usuario y contrasena son obligatorios.");
            }

            byte[] sal = RandomNumberGenerator.GetBytes(TamanoSal);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(contrasena), sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);

            var usuario = new Usuario
            {
                NombreUsuario = nombreUsuario.Trim(),
                NombreCompleto = nombreCompleto.Trim(),
                ContrasenaHash = Convert.ToBase64String(hash),
                ContrasenaSal = Convert.ToBase64String(sal),
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

            byte[] sal = Convert.FromBase64String(usuario.ContrasenaSal);
            byte[] hashIngresado = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(contrasena), sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
            byte[] hashGuardado = Convert.FromBase64String(usuario.ContrasenaHash);

            bool coincide = CryptographicOperations.FixedTimeEquals(hashIngresado, hashGuardado);

            return coincide ? usuario : null;
        }
    }
}
