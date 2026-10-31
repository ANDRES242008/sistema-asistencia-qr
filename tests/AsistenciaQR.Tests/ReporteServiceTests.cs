using ClosedXML.Excel;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class ReporteServiceTests : IDisposable
    {
        private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"reporte_{Guid.NewGuid():N}.xlsx");

        public void Dispose()
        {
            if (File.Exists(_ruta)) File.Delete(_ruta);
        }

        [Fact]
        public void ExportarExcel_EscribeEncabezadosYUnaFilaPorRegistro()
        {
            var datos = new List<AsistenciaDetalle>
            {
                new()
                {
                    NIE = "12345678", NombreCompleto = "Valeria Martinez Lopez", Grado = "B2DA", Seccion = "A",
                    Fecha = new DateTime(2026, 10, 7), Hora = new TimeSpan(7, 12, 30), EstadoPuntualidad = "Tarde"
                },
                new()
                {
                    NIE = "87654321", NombreCompleto = "Jose Luis Franco", Grado = "B2DA", Seccion = "A",
                    Fecha = new DateTime(2026, 10, 7), Hora = new TimeSpan(6, 55, 0), EstadoPuntualidad = "A tiempo",
                    Observacion = "Carnet danado"
                }
            };

            new ReporteService().ExportarExcel(datos, _ruta);

            using var libro = new XLWorkbook(_ruta);
            var hoja = libro.Worksheet("Asistencias");

            Assert.Equal("NIE", hoja.Cell(1, 3).GetString());
            Assert.Equal("Puntualidad", hoja.Cell(1, 8).GetString());
            Assert.Equal("07/10/2026", hoja.Cell(2, 1).GetString());
            Assert.Equal("07:12:30", hoja.Cell(2, 2).GetString());
            Assert.Equal("Tarde", hoja.Cell(2, 8).GetString());
            Assert.Equal("Carnet danado", hoja.Cell(3, 7).GetString());
            Assert.True(hoja.Cell(4, 1).IsEmpty());
        }
    }
}
