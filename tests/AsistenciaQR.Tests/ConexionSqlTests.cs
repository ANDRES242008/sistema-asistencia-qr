using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Tests
{
    public class ConexionSqlTests
    {
        private const string Json = """
            { "ConnectionStrings": { "AsistenciaQR": "Server=desde-json;Database=AsistenciaQR;" } }
            """;

        [Fact]
        public void VariableDeEntorno_TienePrioridad()
        {
            string cadena = ConexionSQL.ResolverCadena("Server=desde-entorno;", Json);

            Assert.Equal("Server=desde-entorno;", cadena);
        }

        [Fact]
        public void SinVariable_UsaAppSettings()
        {
            string cadena = ConexionSQL.ResolverCadena(null, Json);

            Assert.Equal("Server=desde-json;Database=AsistenciaQR;", cadena);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("{ esto no es json")]
        [InlineData("""{ "ConnectionStrings": {} }""")]
        public void SinConfiguracionUtil_UsaInstanciaLocal(string? json)
        {
            string cadena = ConexionSQL.ResolverCadena(null, json);

            Assert.Contains(@"Server=.\SQLEXPRESS", cadena);
        }
    }
}
