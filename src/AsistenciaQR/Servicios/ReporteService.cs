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
    /// asistencia con filtros y lo exporta a Excel o PDF, incluyendo
    /// Observacion y el Estado de Puntualidad (A tiempo / Tarde).
    /// </summary>
    public class ReporteService
    {
        private readonly AsistenciaRepository _asistenciaRepository = new();
        private readonly EstudianteRepository _estudianteRepository = new();

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

            string[] encabezados = { "Fecha", "Hora", "NIE", "Nombre completo", "Grado", "Seccion", "Observacion", "Puntualidad" };
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
                hoja.Cell(fila, 7).Value = registro.Observacion ?? string.Empty;
                hoja.Cell(fila, 8).Value = registro.EstadoPuntualidad ?? string.Empty;
                fila++;
            }

            hoja.Columns().AdjustToContents();
            libro.SaveAs(rutaDestino);
        }

        public void ExportarPdf(List<AsistenciaDetalle> datos, string rutaDestino)
        {
            using var documento = new PdfDocument();
            var pagina = documento.AddPage();
            pagina.Orientation = PdfSharp.PageOrientation.Landscape;
            var graficos = XGraphics.FromPdfPage(pagina);

            var fuenteTitulo = new XFont("Segoe UI", 14, XFontStyleEx.Bold);
            var fuenteEncabezado = new XFont("Segoe UI", 9, XFontStyleEx.Bold);
            var fuenteTexto = new XFont("Segoe UI", 9, XFontStyleEx.Regular);

            const double margen = 30;
            double y = margen;

            double[] anchoColumnas = { 60, 45, 60, 130, 60, 50, 150, 65 };
            string[] encabezados = { "Fecha", "Hora", "NIE", "Nombre", "Grado", "Seccion", "Observacion", "Puntual." };

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
                    pagina.Orientation = PdfSharp.PageOrientation.Landscape;
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
                    registro.Seccion,
                    registro.Observacion ?? string.Empty,
                    registro.EstadoPuntualidad ?? string.Empty
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
