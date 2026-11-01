using Microsoft.Data.SqlClient;
using AsistenciaQR.Repositorios;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class InstalacionTests
    {
        private const string JsonEquipo = """{ "ConnectionStrings": { "AsistenciaQR": "Server=desde-equipo;" } }""";
        private const string JsonPrograma = """{ "ConnectionStrings": { "AsistenciaQR": "Server=desde-programa;" } }""";

        [Fact]
        public void ConfiguracionDelEquipo_GanaALaDelPrograma()
        {
            Assert.Equal("Server=desde-equipo;", ConexionSQL.ResolverCadena(null, JsonEquipo, JsonPrograma));
        }

        [Fact]
        public void SinConfiguracionDelEquipo_UsaLaDelPrograma()
        {
            Assert.Equal("Server=desde-programa;", ConexionSQL.ResolverCadena(null, null, JsonPrograma));
        }

        [Fact]
        public void VariableDeEntorno_GanaATodo()
        {
            Assert.Equal("Server=entorno;", ConexionSQL.ResolverCadena("Server=entorno;", JsonEquipo, JsonPrograma));
        }

        [Theory]
        [InlineData("SQLEXPRESS", @".\SQLEXPRESS")]
        [InlineData("MSSQLSERVER", ".")]
        [InlineData("COLEGIO", @".\COLEGIO")]
        public void NombreDeInstancia_SeConvierteEnServidor(string instancia, string servidor)
        {
            Assert.Equal(servidor, BaseDatosService.NombreDeServidor(instancia));
        }

        [Fact]
        public void CadenaCreada_UsaWindowsYConfiaEnElCertificadoLocal()
        {
            var cadena = new SqlConnectionStringBuilder(ConexionSQL.CrearCadena(@".\SQLEXPRESS"));

            Assert.Equal(@".\SQLEXPRESS", cadena.DataSource);
            Assert.Equal("AsistenciaQR", cadena.InitialCatalog);
            Assert.True(cadena.IntegratedSecurity);
            Assert.True(cadena.TrustServerCertificate);
        }

        [Fact]
        public void ServidorVacio_SeRechazaSinIntentarConectar()
        {
            Assert.False(BaseDatosService.Preparar("  ", permitirTodos: false).Exitoso);
        }
    }
}
