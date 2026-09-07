using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Logica de negocio de reportes: consulta el historico de
    /// asistencia con filtros (fecha, grado, seccion, busqueda de
    /// estudiante) y lo exporta a Excel o PDF.
    /// </summary>
    public class ReporteService
    {
        private readonly AsistenciaRepository _asistenciaRepository = new();
        private readonly EstudianteRepository _estudianteRepository = new();

        // Se registra una sola vez, la primera vez que se usa esta
        // clase, sin importar cuantas veces se abra la pantalla.
        static ReporteService()
        {
            GlobalFontSettings.FontResolver = new ResolvedorFuentesWindows();
        }

        public List<AsistenciaDetalle> ObtenerHistorico(
            DateTime fechaInicio, DateTime fechaFin,
            string? busqueda = null, string? grado = null, string? seccion = null)
        {
            return _asistenciaRepository.ListarHistorico(fechaInicio, fechaFin, busqueda, grado, seccion);
        }

        public List<string> ObtenerGrados() => _estudianteRepository.ObtenerGradosDisponibles();

        public List<string> ObtenerSecciones() => _estudianteRepository.ObtenerSeccionesDisponibles();

        public void ExportarExcel(List<AsistenciaDetalle> datos, string rutaDestino)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Asistencias");

            string[] encabezados = { "Fecha", "Hora", "NIE", "Nombre completo", "Grado", "Seccion" };
            for (int i = 0; i < encabezados.Length; i++)
            {
                var celda = hoja.Cell(1, i + 1);
                celda.Value = encabezados[i];
                celda.Style.Font.Bold = true;
            }

            int fila = 2;
            foreach (var registro in datos)
            {
                hoja.Cell(fila, 1).Value = registro.Fecha.ToString("dd/MM/yyyy");
                hoja.Cell(fila, 2).Value = registro.Hora.ToString(@"hh\:mm\:ss");
                hoja.Cell(fila, 3).Value = registro.NIE;
                hoja.Cell(fila, 4).Value = registro.NombreCompleto;
                hoja.Cell(fila, 5).Value = registro.Grado;
                hoja.Cell(fila, 6).Value = registro.Seccion;
                fila++;
            }

            hoja.Columns().AdjustToContents();
            libro.SaveAs(rutaDestino);
        }

        public void ExportarPdf(List<AsistenciaDetalle> datos, string rutaDestino)
        {
            using var documento = new PdfDocument();
            var pagina = documento.AddPage();
            var graficos = XGraphics.FromPdfPage(pagina);

            var fuenteTitulo = new XFont("Segoe UI", 14, XFontStyleEx.Bold);
            var fuenteEncabezado = new XFont("Segoe UI", 9, XFontStyleEx.Bold);
            var fuenteTexto = new XFont("Segoe UI", 9, XFontStyleEx.Regular);

            const double margen = 30;
            double y = margen;
            double[] anchoColumnas = { 70, 55, 70, 160, 70, 60 };
            string[] encabezados = { "Fecha", "Hora", "NIE", "Nombre", "Grado", "Seccion" };

            graficos.DrawString("Reporte de Asistencias", fuenteTitulo, XBrushes.Black,
                new XPoint(margen, y + 15));
            y += 35;

            void DibujarEncabezado()
            {
                double x = margen;
                for (int i = 0; i < encabezados.Length; i++)
                {
                    graficos.DrawString(encabezados[i], fuenteEncabezado, XBrushes.Black, new XPoint(x, y));
                    x += anchoColumnas[i];
                }
                y += 20;
            }

            DibujarEncabezado();

            foreach (var registro in datos)
            {
                if (y > pagina.Height.Point - margen)
                {
                    pagina = documento.AddPage();
                    graficos = XGraphics.FromPdfPage(pagina);
                    y = margen;
                    DibujarEncabezado();
                }

                double x = margen;
                string[] valores =
                {
                    registro.Fecha.ToString("dd/MM/yyyy"),
                    registro.Hora.ToString(@"hh\:mm"),
                    registro.NIE,
                    registro.NombreCompleto,
                    registro.Grado,
                    registro.Seccion
                };

                for (int i = 0; i < valores.Length; i++)
                {
                    graficos.DrawString(valores[i], fuenteTexto, XBrushes.Black, new XPoint(x, y));
                    x += anchoColumnas[i];
                }

                y += 18;
            }

            documento.Save(rutaDestino);
        }
    }
}
