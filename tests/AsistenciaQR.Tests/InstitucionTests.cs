using System.Drawing;
using System.Drawing.Imaging;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class InstitucionTests
    {
        private static readonly Estudiante Ejemplo = new()
        {
            NIE = "12345678", NombreCompleto = "Valeria Martinez Lopez", Grado = "B2DA", Seccion = "A"
        };

        private static byte[] ImagenPng(int ancho, int alto, Color color)
        {
            using var bitmap = new Bitmap(ancho, alto);
            using (var g = Graphics.FromImage(bitmap)) g.Clear(color);
            using var flujo = new MemoryStream();
            bitmap.Save(flujo, ImageFormat.Png);
            return flujo.ToArray();
        }

        // Cerca del borde inferior izquierdo de la banda, donde no hay texto ni puntos.
        private static Color ColorDeLaBanda(Bitmap carnet) => carnet.GetPixel(8, 990);

        private static bool Parecido(Color a, Color b) =>
            Math.Abs(a.R - b.R) <= 12 && Math.Abs(a.G - b.G) <= 12 && Math.Abs(a.B - b.B) <= 12;

        [Theory]
        [InlineData("#1B2A4A")]
        [InlineData("#ff8800")]
        public void Color_SeConvierteDeIdaYVuelta(string texto)
        {
            var color = InstitucionService.LeerColor(texto)!.Value;

            Assert.Equal(texto.ToUpperInvariant(), InstitucionService.ConvertirColor(color));
        }

        [Theory]
        [InlineData("1B2A4A")]
        [InlineData("#12345")]
        [InlineData("#GGGGGG")]
        [InlineData("")]
        public void ColoresInvalidos_SeRechazan(string texto)
        {
            Assert.Null(InstitucionService.LeerColor(texto));
            Assert.False(InstitucionService.Validar("Colegio Ejemplo", texto).Exitoso);
        }

        [Fact]
        public void Nombre_EsObligatorioYTieneLimite()
        {
            Assert.True(InstitucionService.Validar("Colegio Ejemplo", null).Exitoso);
            Assert.False(InstitucionService.Validar("   ", null).Exitoso);
            Assert.False(InstitucionService.Validar(new string('A', InstitucionService.LargoMaximoNombre + 1), null).Exitoso);
        }

        [Fact]
        public void LogoGrande_SeReduceAlMaximoManteniendoLaProporcion()
        {
            byte[] logo = InstitucionService.PrepararLogo(ImagenPng(2000, 1000, Color.Red));

            using var resultado = new Bitmap(new MemoryStream(logo));
            Assert.Equal(InstitucionService.LadoMaximoLogo, resultado.Width);
            Assert.Equal(InstitucionService.LadoMaximoLogo / 2, resultado.Height);
        }

        [Fact]
        public void LogoPequeno_NoSeAgranda()
        {
            byte[] logo = InstitucionService.PrepararLogo(ImagenPng(120, 80, Color.Red));

            using var resultado = new Bitmap(new MemoryStream(logo));
            Assert.Equal(120, resultado.Width);
        }

        [Fact]
        public void ArchivoQueNoEsImagen_SeRechaza()
        {
            Assert.Throws<ArgumentException>(() => InstitucionService.PrepararLogo(new byte[] { 1, 2, 3, 4, 5 }));
        }

        [Fact]
        public void Carnet_UsaElColorElegidoEnLaBanda()
        {
            var naranja = Color.FromArgb(230, 120, 20);
            var institucion = new ConfiguracionInstitucion { Nombre = "Colegio Ejemplo", ColorPrincipal = InstitucionService.ConvertirColor(naranja) };

            using var carnet = new CarnetService(institucion).DibujarCarnet(Ejemplo, new Qr());

            Assert.True(Parecido(ColorDeLaBanda(carnet), naranja), $"banda {ColorDeLaBanda(carnet)}");
        }

        [Fact]
        public void Carnet_SinColorElegido_ConservaElColorOriginal()
        {
            using var carnet = new CarnetService(InstitucionService.PorDefecto()).DibujarCarnet(Ejemplo, new Qr());

            Assert.True(Parecido(ColorDeLaBanda(carnet), Color.FromArgb(13, 120, 92)), $"banda {ColorDeLaBanda(carnet)}");
        }

        [Fact]
        public void Carnet_SeDibujaConLogoYNombreLargo()
        {
            var institucion = new ConfiguracionInstitucion
            {
                Nombre = new string('M', InstitucionService.LargoMaximoNombre),
                Logo = InstitucionService.PrepararLogo(ImagenPng(300, 300, Color.Gold))
            };

            using var carnet = new CarnetService(institucion).DibujarCarnet(Ejemplo, new Qr());

            // El logo va en el circulo del emblema, arriba a la izquierda.
            Assert.True(Parecido(carnet.GetPixel(70, 92), Color.Gold), $"emblema {carnet.GetPixel(70, 92)}");
        }
    }
}
