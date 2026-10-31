using System.Drawing;
using System.Drawing.Imaging;
using PdfSharp.Pdf.IO;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class HojaDeCarnetsTests : IDisposable
    {
        private const double AnchoCarta = 8.5 * 72, AltoCarta = 11 * 72;
        private const double AnchoCarnet = 638 * 72.0 / 300.0, AltoCarnet = 1013 * 72.0 / 300.0;

        private readonly string _carpeta = Path.Combine(Path.GetTempPath(), $"carnets_{Guid.NewGuid():N}");

        public HojaDeCarnetsTests() => Directory.CreateDirectory(_carpeta);

        public void Dispose() => Directory.Delete(_carpeta, recursive: true);

        [Fact]
        public void NueveCarnets_CabenEnUnaHojaCartaSinSalirse()
        {
            for (int i = 0; i < CarnetService.CarnetsPorHoja; i++)
            {
                var (pagina, x, y) = CarnetService.PosicionEnHoja(i);

                Assert.Equal(0, pagina);
                Assert.True(x >= 18 && x + AnchoCarnet <= AnchoCarta - 18, $"carnet {i} se sale a lo ancho");
                Assert.True(y >= 18 && y + AltoCarnet <= AltoCarta - 18, $"carnet {i} se sale a lo alto");
            }
        }

        [Fact]
        public void LosCarnetsNoSeEncimanEntreSi()
        {
            var posiciones = Enumerable.Range(0, CarnetService.CarnetsPorHoja)
                .Select(i => CarnetService.PosicionEnHoja(i)).ToList();

            for (int a = 0; a < posiciones.Count; a++)
                for (int b = a + 1; b < posiciones.Count; b++)
                {
                    bool seEnciman = posiciones[a].X < posiciones[b].X + AnchoCarnet && posiciones[b].X < posiciones[a].X + AnchoCarnet &&
                                     posiciones[a].Y < posiciones[b].Y + AltoCarnet && posiciones[b].Y < posiciones[a].Y + AltoCarnet;
                    Assert.False(seEnciman, $"carnets {a} y {b} se enciman");
                }
        }

        [Fact]
        public void ElDecimoCarnet_EmpiezaUnaHojaNueva()
        {
            var primero = CarnetService.PosicionEnHoja(0);
            var decimo = CarnetService.PosicionEnHoja(9);

            Assert.Equal(1, decimo.Pagina);
            Assert.Equal(primero.X, decimo.X);
            Assert.Equal(primero.Y, decimo.Y);
        }

        [Fact]
        public void ExportarHoja_CreaUnaPaginaPorCadaNueveCarnets()
        {
            string imagen = Path.Combine(_carpeta, "carnet.png");
            using (var bitmap = new Bitmap(64, 100))
            {
                using (var g = Graphics.FromImage(bitmap)) g.Clear(Color.SteelBlue);
                bitmap.Save(imagen, ImageFormat.Png);
            }

            string pdf = Path.Combine(_carpeta, "carnets.pdf");
            CarnetService.ExportarHojaDeCarnets(Enumerable.Repeat(imagen, 10).ToList(), pdf);

            using var documento = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
            Assert.Equal(2, documento.PageCount);
        }
    }
}
