using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class ValidacionEstudianteTests
    {
        private static Estudiante EstudianteValido() => new()
        {
            NIE = "12345678",
            NombreCompleto = "Valeria Martinez Lopez",
            Grado = "B2DA",
            Seccion = "A",
            Correo = "valeria@ejemplo.com"
        };

        [Fact]
        public void EstudianteCompleto_EsValido()
        {
            Assert.True(EstudianteService.Validar(EstudianteValido()).Exitoso);
        }

        [Fact]
        public void SinCorreo_EsValido()
        {
            var estudiante = EstudianteValido();
            estudiante.Correo = null;

            Assert.True(EstudianteService.Validar(estudiante).Exitoso);
        }

        [Theory]
        [InlineData("")]
        [InlineData("12345")]
        [InlineData("12345678901")]
        [InlineData("1234A678")]
        public void NieInvalido_EsRechazado(string nie)
        {
            var estudiante = EstudianteValido();
            estudiante.NIE = nie;

            Assert.False(EstudianteService.Validar(estudiante).Exitoso);
        }

        [Theory]
        [InlineData("123456")]
        [InlineData("1234567890")]
        public void NieEnLosLimites_EsValido(string nie)
        {
            var estudiante = EstudianteValido();
            estudiante.NIE = nie;

            Assert.True(EstudianteService.Validar(estudiante).Exitoso);
        }

        [Fact]
        public void NombreMuyCorto_EsRechazado()
        {
            var estudiante = EstudianteValido();
            estudiante.NombreCompleto = "Al";

            Assert.False(EstudianteService.Validar(estudiante).Exitoso);
        }

        [Fact]
        public void SinGradoOSeccion_EsRechazado()
        {
            var sinGrado = EstudianteValido();
            sinGrado.Grado = " ";
            var sinSeccion = EstudianteValido();
            sinSeccion.Seccion = "";

            Assert.False(EstudianteService.Validar(sinGrado).Exitoso);
            Assert.False(EstudianteService.Validar(sinSeccion).Exitoso);
        }

        [Fact]
        public void CorreoMalFormado_EsRechazado()
        {
            var estudiante = EstudianteValido();
            estudiante.Correo = "valeria-sin-arroba";

            Assert.False(EstudianteService.Validar(estudiante).Exitoso);
        }
    }
}
