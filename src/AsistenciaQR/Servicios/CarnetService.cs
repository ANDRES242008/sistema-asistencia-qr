using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using AsistenciaQR.Modelos;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace AsistenciaQR.Servicios
{

    public class CarnetService
    {
       
        private const string NombreInstitucion = "Colegio salarrue";

        // Medidas de una tarjeta CR-80 estandar (2.125" x 3.375") a 300 DPI.
        private const int AnchoCarnet = 638;
        private const int AltoCarnet = 1013;

        private static readonly Color ColorEncabezado = Color.FromArgb(21, 58, 92);
        private static readonly Color ColorTextoClaro = Color.White;
        private static readonly Color ColorTextoOscuro = Color.FromArgb(30, 30, 30);

        private readonly string _carpetaCarnets;

        public CarnetService()
        {
          
            _carpetaCarnets = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AsistenciaQR", "Carnets");

            Directory.CreateDirectory(_carpetaCarnets);
        }

        public string GenerarImagenCarnet(Estudiante estudiante, Qr qr)
        {
            using var bitmap = new Bitmap(AnchoCarnet, AltoCarnet);
            using var g = Graphics.FromImage(bitmap);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.Clear(Color.White);
            DibujarEncabezado(g);
            DibujarFoto(g, estudiante);
            DibujarDatos(g, estudiante);
            DibujarQr(g, qr);
            DibujarPie(g);

            string nombreArchivo = $"carnet_{estudiante.NIE}.png";
            string rutaCompleta = Path.Combine(_carpetaCarnets, nombreArchivo);
            bitmap.Save(rutaCompleta, System.Drawing.Imaging.ImageFormat.Png);

            return rutaCompleta;
        }

        public string ExportarCarnetComoPdf(string rutaImagenCarnet)
        {
            using var documento = new PdfDocument();
            var pagina = documento.AddPage();

            // Convierte pixeles (a 300 DPI) a puntos PDF (72 por pulgada)
            // para que el PDF salga al tamano fisico real de un carnet.
            pagina.Width = XUnit.FromPoint(AnchoCarnet * 72.0 / 300.0);
            pagina.Height = XUnit.FromPoint(AltoCarnet * 72.0 / 300.0);

            using var graficos = XGraphics.FromPdfPage(pagina);
            using var imagen = XImage.FromFile(rutaImagenCarnet);
            graficos.DrawImage(imagen, 0, 0, pagina.Width, pagina.Height);

            string rutaPdf = Path.ChangeExtension(rutaImagenCarnet, ".pdf");
            documento.Save(rutaPdf);

            return rutaPdf;
        }

        private static void DibujarEncabezado(Graphics g)
        {
            using var pincel = new SolidBrush(ColorEncabezado);
            g.FillRectangle(pincel, 0, 0, AnchoCarnet, 130);

            using var fuente = new Font("Segoe UI", 20, FontStyle.Bold);
            using var pincelTexto = new SolidBrush(ColorTextoClaro);
            var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(NombreInstitucion, fuente, pincelTexto, new RectangleF(20, 0, AnchoCarnet - 40, 130), formato);
        }

        private static void DibujarFoto(Graphics g, Estudiante estudiante)
        {
            const int diametro = 220;
            int x = (AnchoCarnet - diametro) / 2;
            const int y = 160;

            using var rutaClip = new GraphicsPath();
            rutaClip.AddEllipse(x, y, diametro, diametro);
            g.SetClip(rutaClip);

            if (!string.IsNullOrEmpty(estudiante.FotoRuta) && File.Exists(estudiante.FotoRuta))
            {
                using var foto = Image.FromFile(estudiante.FotoRuta);
                g.DrawImage(foto, x, y, diametro, diametro);
            }
            else
            {
             
                // dibuja un circulo con las iniciales, como hacen
                // apps profesionales cuando no hay foto de perfil.
                using var pincelFondo = new SolidBrush(Color.FromArgb(210, 220, 230));
                g.FillEllipse(pincelFondo, x, y, diametro, diametro);

                string iniciales = ObtenerIniciales(estudiante.NombreCompleto);
                using var fuenteIniciales = new Font("Segoe UI", 48, FontStyle.Bold);
                using var pincelIniciales = new SolidBrush(ColorEncabezado);
                var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(iniciales, fuenteIniciales, pincelIniciales, new RectangleF(x, y, diametro, diametro), formato);
            }

            g.ResetClip();

            using var pincelBorde = new Pen(ColorEncabezado, 4);
            g.DrawEllipse(pincelBorde, x, y, diametro, diametro);
        }







        private static string ObtenerIniciales(string nombreCompleto)
        {
            var partes = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return "?";
            if (partes.Length == 1) return partes[0][0].ToString().ToUpper();
            return $"{partes[0][0]}{partes[1][0]}".ToUpper();
        }

        private static void DibujarDatos(Graphics g, Estudiante estudiante)
        {
            using var fuenteNombre = new Font("Segoe UI", 20, FontStyle.Bold);
            using var fuenteDato = new Font("Segoe UI", 16, FontStyle.Regular);
            using var pincelTexto = new SolidBrush(ColorTextoOscuro);
            var formatoCentrado = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };

            int y = 400;

            g.DrawString(estudiante.NombreCompleto, fuenteNombre, pincelTexto,
                new RectangleF(20, y, AnchoCarnet - 40, 55), formatoCentrado);

            y += 65;
            g.DrawString($"NIE: {estudiante.NIE}", fuenteDato, pincelTexto,
                new RectangleF(20, y, AnchoCarnet - 40, 40), formatoCentrado);

            y += 45;
            g.DrawString($"{estudiante.Grado} - Seccion {estudiante.Seccion}", fuenteDato, pincelTexto,
                new RectangleF(20, y, AnchoCarnet - 40, 40), formatoCentrado);
        }

        private static void DibujarQr(Graphics g, Qr qr)
        {
            const int tamano = 280;
            int x = (AnchoCarnet - tamano) / 2;
            const int y = 610;

            if (!string.IsNullOrEmpty(qr.RutaImagen) && File.Exists(qr.RutaImagen))
            {
                using var imagenQr = Image.FromFile(qr.RutaImagen);
                g.DrawImage(imagenQr, x, y, tamano, tamano);
            }

            using var pincelBorde = new Pen(Color.LightGray, 2);
            g.DrawRectangle(pincelBorde, x, y, tamano, tamano);
        }

        private static void DibujarPie(Graphics g)
        {
            using var pincel = new SolidBrush(ColorEncabezado);
            g.FillRectangle(pincel, 0, AltoCarnet - 40, AnchoCarnet, 40);

            using var fuente = new Font("Segoe UI", 10, FontStyle.Regular);
            using var pincelTexto = new SolidBrush(ColorTextoClaro);
            var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("Presenta este carnet en el kiosco de asistencia", fuente, pincelTexto,
                new RectangleF(0, AltoCarnet - 40, AnchoCarnet, 40), formato);
        }
    }
}
