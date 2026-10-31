using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class SeguridadCuentasTests
    {
        private static readonly DateTime Ahora = new(2026, 10, 31, 7, 30, 0);

        private static Usuario Administrador(bool activo = true) =>
            new() { NombreUsuario = "admin", Rol = AuthService.RolAdministrador, Activo = activo };

        private static Usuario Docente() =>
            new() { NombreUsuario = "docente", Rol = AuthService.RolDocente, Activo = true };

        // --- Bloqueo por intentos fallidos ---

        [Fact]
        public void IntentosAntesDelLimite_SoloSumanSinBloquear()
        {
            var (intentos, bloqueadoHasta) = AuthService.CalcularIntentoFallido(AuthService.IntentosAntesDeBloquear - 2, Ahora);

            Assert.Equal(AuthService.IntentosAntesDeBloquear - 1, intentos);
            Assert.Null(bloqueadoHasta);
        }

        [Fact]
        public void AlLlegarAlLimite_SeBloqueaYElContadorVuelveACero()
        {
            var (intentos, bloqueadoHasta) = AuthService.CalcularIntentoFallido(AuthService.IntentosAntesDeBloquear - 1, Ahora);

            Assert.Equal(0, intentos);
            Assert.Equal(Ahora + AuthService.DuracionBloqueo, bloqueadoHasta);
        }

        [Fact]
        public void CuentaConBloqueoVigente_EstaBloqueada()
        {
            var usuario = Docente();
            usuario.BloqueadoHasta = Ahora.AddMinutes(5);

            Assert.True(AuthService.EstaBloqueado(usuario, Ahora));
        }

        [Fact]
        public void CuentaConBloqueoVencido_YaNoEstaBloqueada()
        {
            var usuario = Docente();
            usuario.BloqueadoHasta = Ahora.AddSeconds(-1);

            Assert.False(AuthService.EstaBloqueado(usuario, Ahora));
            Assert.False(AuthService.EstaBloqueado(Docente(), Ahora));
        }

        // --- Siempre debe quedar un Administrador activo ---

        [Fact]
        public void UltimoAdministrador_NoSePuedeDesactivar()
        {
            Assert.False(AuthService.PuedeQuitarAdministrador(Administrador(), AuthService.RolAdministrador, nuevoActivo: false, administradoresActivos: 1));
        }

        [Fact]
        public void UltimoAdministrador_NoSePuedeConvertirEnDocente()
        {
            Assert.False(AuthService.PuedeQuitarAdministrador(Administrador(), AuthService.RolDocente, nuevoActivo: true, administradoresActivos: 1));
        }

        [Fact]
        public void ConOtroAdministrador_SiSePuedeDesactivar()
        {
            Assert.True(AuthService.PuedeQuitarAdministrador(Administrador(), AuthService.RolAdministrador, nuevoActivo: false, administradoresActivos: 2));
        }

        [Fact]
        public void EditarAlUltimoAdministradorSinQuitarleElRol_EstaPermitido()
        {
            Assert.True(AuthService.PuedeQuitarAdministrador(Administrador(), AuthService.RolAdministrador, nuevoActivo: true, administradoresActivos: 1));
        }

        [Fact]
        public void Docentes_SiempreSePuedenDesactivar()
        {
            Assert.True(AuthService.PuedeQuitarAdministrador(Docente(), AuthService.RolDocente, nuevoActivo: false, administradoresActivos: 1));
        }

        // --- Contrasenas nuevas y temporales ---

        [Fact]
        public void NuevaContrasena_DebeCoincidirConLaConfirmacion()
        {
            Assert.True(AuthService.ValidarNuevaContrasena("ClaveNueva2026", "ClaveNueva2026").Exitoso);
            Assert.False(AuthService.ValidarNuevaContrasena("ClaveNueva2026", "ClaveNueva2025").Exitoso);
        }

        [Fact]
        public void NuevaContrasena_RespetaLaLongitudMinima()
        {
            Assert.False(AuthService.ValidarNuevaContrasena("corta", "corta").Exitoso);
        }

        [Fact]
        public void ContrasenaTemporal_CumpleLaLongitudYEsDistintaCadaVez()
        {
            string primera = AuthService.GenerarContrasenaTemporal();
            string segunda = AuthService.GenerarContrasenaTemporal();

            Assert.True(primera.Length >= AuthService.LongitudMinimaContrasena);
            Assert.NotEqual(primera, segunda);
            Assert.True(AuthService.ValidarNuevaContrasena(primera, primera).Exitoso);
        }

        [Fact]
        public void ContrasenaTemporal_NoUsaCaracteresQueSeConfunden()
        {
            string temporal = AuthService.GenerarContrasenaTemporal(200);

            Assert.DoesNotContain(temporal, c => "0O1lI".Contains(c));
        }
    }
}
