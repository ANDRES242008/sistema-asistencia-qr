using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class BitacoraServiceTests
    {
        [Fact]
        public void DescribirCambios_SoloMencionaLoQueCambio()
        {
            string texto = BitacoraService.DescribirCambios(
                ("Nombre", "Ana Lopez", "Ana Lopez"),
                ("Grado", "B1DA", "B2DA"),
                ("Seccion", "A", "B"));

            Assert.Equal("Grado: B1DA -> B2DA; Seccion: A -> B", texto);
        }

        [Fact]
        public void DescribirCambios_MuestraLosValoresVacios()
        {
            string texto = BitacoraService.DescribirCambios(("Correo", null, "ana@ejemplo.com"));

            Assert.Equal("Correo: (vacio) -> ana@ejemplo.com", texto);
        }

        [Fact]
        public void DescribirCambios_SinDiferencias_LoDice()
        {
            Assert.Equal("sin cambios", BitacoraService.DescribirCambios(("Rol", "Docente", "Docente"), ("Correo", null, "")));
        }

        [Fact]
        public void DescribirEstudiante_IncluyeNieNombreYGrado()
        {
            var estudiante = new Estudiante { NIE = "12345678", NombreCompleto = "Valeria Martinez", Grado = "B2DA", Seccion = "A" };

            Assert.Equal("NIE 12345678 · Valeria Martinez · B2DA A", BitacoraService.DescribirEstudiante(estudiante));
        }

        [Fact]
        public void TextosLargos_SeRecortanAlLimiteDeLaColumna()
        {
            string largo = new('x', 600);

            string recortado = BitacoraService.Recortar(largo, 500);

            Assert.Equal(500, recortado.Length);
            Assert.EndsWith("...", recortado);
            Assert.Equal("corto", BitacoraService.Recortar("corto", 500));
        }
    }
}
