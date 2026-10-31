using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class RespaldoServiceTests
    {
        private static readonly DateTime Hoy = new(2026, 10, 31, 7, 15, 30);

        private static RespaldoInfo Respaldo(TipoRespaldo tipo, DateTime fecha) => new() { Tipo = tipo, Fecha = fecha };

        [Theory]
        [InlineData(TipoRespaldo.Automatico)]
        [InlineData(TipoRespaldo.Manual)]
        [InlineData(TipoRespaldo.AntesDeRestaurar)]
        public void ElNombreDelArchivo_SeInterpretaDeVuelta(TipoRespaldo tipo)
        {
            string nombre = RespaldoService.NombreArchivo(Hoy, tipo);
            var info = RespaldoService.InterpretarNombre(nombre);

            Assert.EndsWith(".bak", nombre);
            Assert.Equal((Hoy, tipo), info);
        }

        [Theory]
        [InlineData("otra_cosa.bak")]
        [InlineData("AsistenciaQR_2026-13-45_999999_auto.bak")]
        [InlineData("AsistenciaQR_2026-10-31_071530_desconocido.bak")]
        [InlineData("AsistenciaQR_.bak")]
        public void ArchivosAjenos_SeIgnoran(string nombre)
        {
            Assert.Null(RespaldoService.InterpretarNombre(nombre));
        }

        [Fact]
        public void SinRespaldoAutomaticoHoy_HayQueHacerlo()
        {
            var respaldos = new[]
            {
                Respaldo(TipoRespaldo.Automatico, Hoy.AddDays(-1)),
                Respaldo(TipoRespaldo.Manual, Hoy) // un manual no cuenta como el automatico del dia
            };

            Assert.True(RespaldoService.NecesitaRespaldoAutomatico(respaldos, Hoy));
        }

        [Fact]
        public void ConRespaldoAutomaticoHoy_NoSeRepite()
        {
            var respaldos = new[] { Respaldo(TipoRespaldo.Automatico, Hoy.Date.AddHours(6)) };

            Assert.False(RespaldoService.NecesitaRespaldoAutomatico(respaldos, Hoy));
        }

        [Fact]
        public void SeConservanLosUltimosSieteAutomaticos()
        {
            var automaticos = Enumerable.Range(0, 10).Select(d => Respaldo(TipoRespaldo.Automatico, Hoy.AddDays(-d))).ToList();

            var aBorrar = RespaldoService.SeleccionarParaBorrar(automaticos);

            Assert.Equal(3, aBorrar.Count);
            Assert.All(aBorrar, r => Assert.True(r.Fecha < Hoy.AddDays(-6)));
        }

        [Fact]
        public void LosManuales_NuncaSeBorranSolos()
        {
            var manuales = Enumerable.Range(0, 30).Select(d => Respaldo(TipoRespaldo.Manual, Hoy.AddDays(-d)));

            Assert.Empty(RespaldoService.SeleccionarParaBorrar(manuales));
        }

        [Fact]
        public void DeLosPreviosARestaurar_SeConservanTres()
        {
            var previos = Enumerable.Range(0, 5).Select(d => Respaldo(TipoRespaldo.AntesDeRestaurar, Hoy.AddDays(-d))).ToList();

            var aBorrar = RespaldoService.SeleccionarParaBorrar(previos);

            Assert.Equal(2, aBorrar.Count);
            Assert.DoesNotContain(previos[0], aBorrar);
        }
    }
}
