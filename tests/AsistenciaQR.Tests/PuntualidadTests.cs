using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class PuntualidadTests
    {
        private static readonly TimeSpan Inicio = new(7, 0, 0);

        [Fact]
        public void AntesDeLaHoraDeInicio_EsATiempo()
        {
            Assert.False(ConfiguracionHorarioService.EsTarde(new TimeSpan(6, 45, 0), Inicio, 10));
        }

        [Fact]
        public void DentroDeLaTolerancia_EsATiempo()
        {
            Assert.False(ConfiguracionHorarioService.EsTarde(new TimeSpan(7, 9, 59), Inicio, 10));
        }

        [Fact]
        public void JustoEnElLimite_TodaviaEsATiempo()
        {
            Assert.False(ConfiguracionHorarioService.EsTarde(new TimeSpan(7, 10, 0), Inicio, 10));
        }

        [Fact]
        public void UnSegundoDespuesDelLimite_EsTarde()
        {
            Assert.True(ConfiguracionHorarioService.EsTarde(new TimeSpan(7, 10, 1), Inicio, 10));
        }

        [Fact]
        public void SinTolerancia_ElLimiteEsLaHoraDeInicio()
        {
            Assert.False(ConfiguracionHorarioService.EsTarde(Inicio, Inicio, 0));
            Assert.True(ConfiguracionHorarioService.EsTarde(Inicio + TimeSpan.FromSeconds(1), Inicio, 0));
        }
    }
}
