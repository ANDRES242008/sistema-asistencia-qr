using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class AuthServiceTests
    {
        [Fact]
        public void ContrasenaCorrecta_SeVerifica()
        {
            var (hash, sal) = AuthService.CrearHash("ClaveSegura2026");

            Assert.True(AuthService.VerificarContrasena("ClaveSegura2026", hash, sal));
        }

        [Fact]
        public void ContrasenaIncorrecta_NoSeVerifica()
        {
            var (hash, sal) = AuthService.CrearHash("ClaveSegura2026");

            Assert.False(AuthService.VerificarContrasena("claveSegura2026", hash, sal));
        }

        [Fact]
        public void MismaContrasena_GeneraSalYHashDistintos()
        {
            var primero = AuthService.CrearHash("ClaveSegura2026");
            var segundo = AuthService.CrearHash("ClaveSegura2026");

            Assert.NotEqual(primero.Sal, segundo.Sal);
            Assert.NotEqual(primero.Hash, segundo.Hash);
        }

        [Fact]
        public void ElHash_NoContieneLaContrasena()
        {
            var (hash, _) = AuthService.CrearHash("ClaveSegura2026");

            Assert.DoesNotContain("ClaveSegura2026", hash);
        }

        [Fact]
        public void RegistroValido_EsAceptado()
        {
            var resultado = AuthService.ValidarRegistro("admin", "Administrador", "ClaveSegura2026", AuthService.RolAdministrador);

            Assert.True(resultado.Exitoso);
        }

        [Theory]
        [InlineData("", "Nombre", "ClaveSegura2026", "Administrador")]
        [InlineData("admin", "", "ClaveSegura2026", "Administrador")]
        [InlineData("admin", "Nombre", "corta", "Administrador")]
        [InlineData("admin", "Nombre", "ClaveSegura2026", "SuperUsuario")]
        public void RegistroInvalido_EsRechazado(string usuario, string nombre, string contrasena, string rol)
        {
            var resultado = AuthService.ValidarRegistro(usuario, nombre, contrasena, rol);

            Assert.False(resultado.Exitoso);
        }
    }
}
