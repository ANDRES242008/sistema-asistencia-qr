using ClosedXML.Excel;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class ImportacionEstudiantesTests : IDisposable
    {
        private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"plantilla_{Guid.NewGuid():N}.xlsx");

        public void Dispose()
        {
            if (File.Exists(_ruta)) File.Delete(_ruta);
        }

        /// <summary>Crea un libro desde la plantilla real y le agrega filas.</summary>
        private XLWorkbook PlantillaCon(params object[][] filas)
        {
            ImportacionEstudiantesService.GenerarPlantilla(_ruta);
            var libro = new XLWorkbook(_ruta);
            var hoja = libro.Worksheet(ImportacionEstudiantesService.NombreHoja);
            for (int i = 0; i < filas.Length; i++)
                for (int j = 0; j < filas[i].Length; j++)
                    hoja.Cell(i + 2, j + 1).Value = XLCellValue.FromObject(filas[i][j]);
            return libro;
        }

        private static List<FilaImportacion> Leer(XLWorkbook libro, ISet<string>? registrados = null)
        {
            var filas = ImportacionEstudiantesService.LeerFilas(libro, out string? error);
            Assert.Null(error);
            ImportacionEstudiantesService.Revisar(filas, registrados ?? new HashSet<string>());
            return filas;
        }

        [Fact]
        public void Plantilla_TieneLosEncabezadosYNingunEstudiante()
        {
            ImportacionEstudiantesService.GenerarPlantilla(_ruta);
            using var libro = new XLWorkbook(_ruta);

            var hoja = libro.Worksheet(ImportacionEstudiantesService.NombreHoja);
            Assert.Equal("NIE", hoja.Cell(1, 1).GetString());
            Assert.Equal("Correo", hoja.Cell(1, 5).GetString());

            ImportacionEstudiantesService.LeerFilas(libro, out string? error);
            Assert.NotNull(error); // vacia: no hay nada que importar
        }

        [Fact]
        public void FilasCorrectas_QuedanListasConLosDatos()
        {
            using var libro = PlantillaCon(
                new object[] { "12345678", "  Valeria   Martinez Lopez ", "B2DA", "A", "valeria@ejemplo.com" },
                new object[] { "87654321", "Jose Luis Franco", "B2DA", "B", "" });

            var filas = Leer(libro);

            Assert.Equal(2, filas.Count);
            Assert.All(filas, f => Assert.True(f.EsValida));
            Assert.Equal("Valeria Martinez Lopez", filas[0].Estudiante.NombreCompleto);
            Assert.Null(filas[1].Estudiante.Correo);
            Assert.Equal(3, filas[1].NumeroFila);
        }

        [Fact]
        public void NieEscritoComoNumero_SeLeeSinDecimales()
        {
            using var libro = PlantillaCon(new object[] { 12345678, "Valeria Martinez", "B2DA", "A" });

            var fila = Leer(libro).Single();

            Assert.Equal("12345678", fila.Estudiante.NIE);
            Assert.True(fila.EsValida);
        }

        [Fact]
        public void FilasVacias_SeIgnoran()
        {
            using var libro = PlantillaCon(
                new object[] { "12345678", "Valeria Martinez", "B2DA", "A" },
                new object[] { "", "", "", "" },
                new object[] { "87654321", "Jose Franco", "B2DA", "A" });

            Assert.Equal(2, Leer(libro).Count);
        }

        [Fact]
        public void DatosInvalidos_SeMarcanConElMotivo()
        {
            using var libro = PlantillaCon(
                new object[] { "12A45", "Valeria Martinez", "B2DA", "A" },
                new object[] { "87654321", "Jose Franco", "", "A" });

            var filas = Leer(libro);

            Assert.Contains("NIE", filas[0].Error);
            Assert.Contains("grado", filas[1].Error);
        }

        [Fact]
        public void NieRepetidoEnElArchivo_SeMarcaLaSegundaVez()
        {
            using var libro = PlantillaCon(
                new object[] { "12345678", "Valeria Martinez", "B2DA", "A" },
                new object[] { "12345678", "Otra Persona", "B2DA", "A" });

            var filas = Leer(libro);

            Assert.True(filas[0].EsValida);
            Assert.Equal("NIE repetido: ya aparece en la fila 2.", filas[1].Error);
        }

        [Fact]
        public void NieYaRegistrado_SeRechaza()
        {
            using var libro = PlantillaCon(new object[] { "12345678", "Valeria Martinez", "B2DA", "A" });

            var fila = Leer(libro, new HashSet<string> { "12345678" }).Single();

            Assert.Contains("Ya existe", fila.Error);
        }

        [Fact]
        public void EncabezadosConTildeYEnOtroOrden_SeReconocen()
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Hoja1");
            string[] encabezados = { "Sección", "GRADO", "Nombre", "nie" };
            for (int i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
            hoja.Cell(2, 1).Value = "A";
            hoja.Cell(2, 2).Value = "B2DA";
            hoja.Cell(2, 3).Value = "Valeria Martinez";
            hoja.Cell(2, 4).Value = "12345678";

            var fila = Leer(libro).Single();

            Assert.True(fila.EsValida);
            Assert.Equal("A", fila.Estudiante.Seccion);
            Assert.Equal("12345678", fila.Estudiante.NIE);
        }

        [Fact]
        public void ArchivoSinEncabezados_DevuelveErrorClaro()
        {
            using var libro = new XLWorkbook();
            libro.Worksheets.Add("Hoja1").Cell(1, 1).Value = "Lista de alumnos";

            var filas = ImportacionEstudiantesService.LeerFilas(libro, out string? error);

            Assert.Empty(filas);
            Assert.Contains("encabezados", error);
        }
    }
}
