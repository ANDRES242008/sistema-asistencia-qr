using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using AsistenciaQR.Modelos;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Genera el carnet del estudiante. V5: layout de una sola
    /// columna centrada (foto, nombre, grado, QR, NIE, pie), como la
    /// referencia que se aprobo. Todo se acomoda con un cursor Y
    /// dinamico (se mide cada bloque antes de dibujar el siguiente),
    /// para que nunca se monte un elemento con otro sin importar que
    /// tan largo sea el nombre del estudiante.
    ///
    /// Dos cosas de la referencia que NO se copiaron a proposito:
    /// - El escudo de la referencia es generico/inventado (typico de
    ///   una imagen hecha con IA). Se sigue usando el escudo REAL de
    ///   Sonsonate que ya me diste, dentro del mismo marco dorado.
    /// - La referencia trae un error de ortografia ("Sécción"). Aqui
    ///   se usa "Seccion" bien escrito.
    ///
    /// El tamaño del QR (330px) y el tamaño de tarjeta (CR-80) siguen
    /// sin tocarse, por las mismas razones de siempre.
    /// </summary>
    public class CarnetService
    {
        private const string NombreInstitucion = "Colegio salarrue";
        private const string RutaEscudo = "Assets\\escudo_sonsonate.jpg";

        private const int AnchoCarnet = 638;
        private const int AltoCarnet = 1013;
        private const int AnchoBanda = 140;

        private const int MargenPrincipal = 20;
        private const int AnchoPrincipal = AnchoCarnet - AnchoBanda;
        private const int CentroPrincipalX = AnchoBanda + AnchoPrincipal / 2;

        private const int FotoDiametro = 300;

        private static readonly Color ColorBandaArriba = Color.FromArgb(18, 88, 104);
        private static readonly Color ColorBandaAbajo = Color.FromArgb(13, 120, 92);   // verde esmeralda
        private static readonly Color ColorFondoArriba = Color.FromArgb(26, 66, 102);
        private static readonly Color ColorFondoAbajo = Color.FromArgb(14, 38, 64);
        private static readonly Color ColorAcentoNombre = Color.FromArgb(110, 231, 209); // mint
        private static readonly Color ColorDorado = Color.FromArgb(197, 164, 90);
        private static readonly Color ColorTextoClaro = Color.White;
        private static readonly Color ColorTextoMuted = Color.FromArgb(185, 205, 210);
        private static readonly Color ColorLinea = Color.FromArgb(90, 255, 255, 255);

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

            g.Clear(Color.Transparent);

            using var rutaTarjeta = ObtenerRutaTarjetaRedondeada();
            g.SetClip(rutaTarjeta);

            DibujarFondoPrincipal(g);
            DibujarBandaLateral(g);

            int y = 46;
            y = DibujarFoto(g, estudiante, y) + 26;
            y = DibujarNombre(g, estudiante, y) + 14;
            y = DibujarEtiquetaConLineas(g, $"{estudiante.Grado} - Seccion {estudiante.Seccion}",
                new Font("Segoe UI", 15, FontStyle.Bold), ColorTextoClaro, y) + 20;
            y = DibujarEtiquetaConLineas(g, "CODIGO DE ASISTENCIA",
                new Font("Segoe UI", 10.5f, FontStyle.Bold), ColorTextoMuted, y) + 14;
            y = DibujarQr(g, qr, y) + 18;
            y = DibujarEtiquetaConLineas(g, $"NIE: {estudiante.NIE}",
                new Font("Segoe UI", 14, FontStyle.Bold), ColorTextoClaro, y) + 12;
            DibujarPie(g, y);

            g.ResetClip();

            string nombreArchivo = $"carnet_{estudiante.NIE}.png";
            string rutaCompleta = Path.Combine(_carpetaCarnets, nombreArchivo);
            bitmap.Save(rutaCompleta, System.Drawing.Imaging.ImageFormat.Png);

            return rutaCompleta;
        }

        public string ExportarCarnetComoPdf(string rutaImagenCarnet)
        {
            using var documento = new PdfDocument();
            var pagina = documento.AddPage();

            pagina.Width = XUnit.FromPoint(AnchoCarnet * 72.0 / 300.0);
            pagina.Height = XUnit.FromPoint(AltoCarnet * 72.0 / 300.0);

            using var graficos = XGraphics.FromPdfPage(pagina);
            using var imagen = XImage.FromFile(rutaImagenCarnet);
            graficos.DrawImage(imagen, 0, 0, pagina.Width, pagina.Height);

            string rutaPdf = Path.ChangeExtension(rutaImagenCarnet, ".pdf");
            documento.Save(rutaPdf);

            return rutaPdf;
        }

        private static GraphicsPath ObtenerRutaTarjetaRedondeada()
        {
            const int radio = 28;
            var ruta = new GraphicsPath();
            ruta.AddArc(0, 0, radio * 2, radio * 2, 180, 90);
            ruta.AddArc(AnchoCarnet - radio * 2, 0, radio * 2, radio * 2, 270, 90);
            ruta.AddArc(AnchoCarnet - radio * 2, AltoCarnet - radio * 2, radio * 2, radio * 2, 0, 90);
            ruta.AddArc(0, AltoCarnet - radio * 2, radio * 2, radio * 2, 90, 90);
            ruta.CloseFigure();
            return ruta;
        }

        private static void DibujarFondoPrincipal(Graphics g)
        {
            var area = new Rectangle(AnchoBanda, 0, AnchoPrincipal, AltoCarnet);
            using var degradado = new LinearGradientBrush(area, ColorFondoArriba, ColorFondoAbajo, LinearGradientMode.Vertical);
            g.FillRectangle(degradado, area);
        }

        private static void DibujarBandaLateral(Graphics g)
        {
            var areaBanda = new Rectangle(0, 0, AnchoBanda, AltoCarnet);
            using (var degradado = new LinearGradientBrush(areaBanda, ColorBandaArriba, ColorBandaAbajo, LinearGradientMode.Vertical))
                g.FillRectangle(degradado, areaBanda);

            using (var pincelPunto = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
            {
                for (int yPunto = 24; yPunto < AltoCarnet; yPunto += 16)
                    for (int xPunto = 14; xPunto < AnchoBanda; xPunto += 16)
                        g.FillEllipse(pincelPunto, xPunto, yPunto, 3, 3);
            }

            DibujarEmblema(g);

            using var fuenteBanda = new Font("Segoe UI", 20, FontStyle.Bold);
            using var pincelTexto = new SolidBrush(ColorTextoClaro);
            var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            float yInicio = 195;
            float yFin = AltoCarnet - 40;
            float longitud = yFin - yInicio;
            float centroY = yInicio + longitud / 2f;

            g.TranslateTransform(AnchoBanda / 2f, centroY);
            g.RotateTransform(-90);
            g.DrawString(NombreInstitucion.ToUpper(), fuenteBanda, pincelTexto,
                new RectangleF(-longitud / 2f, -AnchoBanda / 2f, longitud, AnchoBanda), formato);
            g.ResetTransform();
        }

        private static void DibujarEmblema(Graphics g)
        {
            const int cx = AnchoBanda / 2;
            const int cy = 92;
            const int radio = 44;

            using (var pincelSombra = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
                g.FillEllipse(pincelSombra, cx - radio + 2, cy - radio + 5, radio * 2, radio * 2);

            using (var pincelCirculo = new SolidBrush(Color.White))
                g.FillEllipse(pincelCirculo, cx - radio, cy - radio, radio * 2, radio * 2);

            string rutaEscudo = Path.Combine(AppContext.BaseDirectory, RutaEscudo);
            if (File.Exists(rutaEscudo))
            {
                const int margenInterno = 6;
                using var rutaClip = new GraphicsPath();
                rutaClip.AddEllipse(cx - radio + margenInterno, cy - radio + margenInterno,
                    (radio - margenInterno) * 2, (radio - margenInterno) * 2);

                var clipAnterior = g.Clip;
                g.SetClip(rutaClip, CombineMode.Intersect);

                using var escudo = Image.FromFile(rutaEscudo);
                int destino = (radio - margenInterno) * 2;
                float escala = Math.Max((float)destino / escudo.Width, (float)destino / escudo.Height);
                int anchoDestino = (int)(escudo.Width * escala);
                int altoDestino = (int)(escudo.Height * escala);
                g.DrawImage(escudo, cx - anchoDestino / 2, cy - altoDestino / 2, anchoDestino, altoDestino);

                g.Clip = clipAnterior;
            }
            else
            {
                DibujarEmblemaGenerico(g, cx, cy, radio);
            }

            // Marco dorado, como en la referencia (antes era navy/mint).
            using var pincelBordeEmblema = new Pen(ColorDorado, 3);
            g.DrawEllipse(pincelBordeEmblema, cx - radio, cy - radio, radio * 2, radio * 2);
        }

        /// <summary>Se usa solo si no se encuentra el archivo del escudo real.</summary>
        private static void DibujarEmblemaGenerico(Graphics g, int cx, int cy, int radio)
        {
            using var rutaEscudo = new GraphicsPath();
            int ax = cx - 19, ay = cy - 20;
            rutaEscudo.AddLine(ax, ay, ax + 38, ay);
            rutaEscudo.AddLine(ax + 38, ay, ax + 38, ay + 16);
            rutaEscudo.AddBezier(ax + 38, ay + 16, ax + 38, ay + 32, ax + 19, ay + 40, ax + 19, ay + 40);
            rutaEscudo.AddBezier(ax + 19, ay + 40, ax, ay + 32, ax, ay + 16, ax, ay + 16);
            rutaEscudo.CloseFigure();

            using var pincelEscudo = new SolidBrush(ColorBandaAbajo);
            g.FillPath(pincelEscudo, rutaEscudo);

            using var pincelQrMini = new SolidBrush(Color.White);
            int qx = cx - 9, qy = cy - 9;
            g.FillRectangle(pincelQrMini, qx, qy, 6, 6);
            g.FillRectangle(pincelQrMini, qx + 12, qy, 6, 6);
            g.FillRectangle(pincelQrMini, qx, qy + 12, 6, 6);
            g.FillRectangle(pincelQrMini, qx + 7, qy + 7, 4, 4);
        }

        private static string ObtenerIniciales(string nombreCompleto)
        {
            var partes = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return "?";
            if (partes.Length == 1) return partes[0][0].ToString().ToUpper();
            return $"{partes[0][0]}{partes[1][0]}".ToUpper();
        }

        /// <summary>Foto grande y centrada, como en la referencia. Devuelve el Y donde termino.</summary>
        private static int DibujarFoto(Graphics g, Estudiante estudiante, int y)
        {
            int x = CentroPrincipalX - FotoDiametro / 2;

            using (var pincelSombra = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                g.FillEllipse(pincelSombra, x + 3, y + 6, FotoDiametro, FotoDiametro);

            using var rutaClip = new GraphicsPath();
            rutaClip.AddEllipse(x, y, FotoDiametro, FotoDiametro);
            var clipAnterior = g.Clip;
            g.SetClip(rutaClip, CombineMode.Intersect);

            if (!string.IsNullOrEmpty(estudiante.FotoRuta) && File.Exists(estudiante.FotoRuta))
            {
                using var foto = Image.FromFile(estudiante.FotoRuta);
                g.DrawImage(foto, x, y, FotoDiametro, FotoDiametro);
            }
            else
            {
                using var degradadoFondo = new LinearGradientBrush(
                    new Rectangle(x, y, FotoDiametro, FotoDiametro),
                    ColorBandaAbajo, ColorFondoArriba, LinearGradientMode.ForwardDiagonal);
                g.FillEllipse(degradadoFondo, x, y, FotoDiametro, FotoDiametro);

                string iniciales = ObtenerIniciales(estudiante.NombreCompleto);
                using var fuenteIniciales = new Font("Segoe UI", 64, FontStyle.Bold);
                using var pincelIniciales = new SolidBrush(Color.White);
                var formatoIni = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(iniciales, fuenteIniciales, pincelIniciales,
                    new RectangleF(x, y, FotoDiametro, FotoDiametro), formatoIni);
            }

            g.Clip = clipAnterior;

            using var pincelBorde = new Pen(Color.White, 5);
            g.DrawEllipse(pincelBorde, x, y, FotoDiametro, FotoDiametro);

            return y + FotoDiametro;
        }

        /// <summary>Nombre centrado, en 1 o mas lineas segun haga falta. Devuelve el Y donde termino.</summary>
        private static int DibujarNombre(Graphics g, Estudiante estudiante, int y)
        {
            using var fuenteNombre = new Font("Segoe UI", 19, FontStyle.Bold);
            using var pincelNombre = new SolidBrush(ColorTextoClaro);
            var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

            int anchoTexto = AnchoPrincipal - MargenPrincipal * 2;
            string nombre = estudiante.NombreCompleto.ToUpper();

            SizeF tamano = g.MeasureString(nombre, fuenteNombre, anchoTexto);
            g.DrawString(nombre, fuenteNombre, pincelNombre,
                new RectangleF(AnchoBanda + MargenPrincipal, y, anchoTexto, tamano.Height), formato);

            return y + (int)tamano.Height;
        }

        /// <summary>
        /// Dibuja un texto centrado con una linea delgada a cada
        /// lado (mismo estilo que la referencia). Devuelve el Y
        /// donde termino, para encadenar el siguiente bloque.
        /// </summary>
        private static int DibujarEtiquetaConLineas(Graphics g, string texto, Font fuente, Color color, int y)
        {
            using var pincelTexto = new SolidBrush(color);
            using var pluma = new Pen(ColorLinea, 1);

            SizeF tamano = g.MeasureString(texto, fuente);
            float centroLineaY = y + tamano.Height / 2f + 2;
            float mitadTexto = tamano.Width / 2f + 14;

            g.DrawLine(pluma, AnchoBanda + MargenPrincipal + 10, centroLineaY, CentroPrincipalX - mitadTexto, centroLineaY);
            g.DrawLine(pluma, CentroPrincipalX + mitadTexto, centroLineaY, AnchoCarnet - MargenPrincipal - 10, centroLineaY);

            var formato = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(texto, fuente, pincelTexto,
                new RectangleF(AnchoBanda + MargenPrincipal, y, AnchoPrincipal - MargenPrincipal * 2, tamano.Height + 4), formato);

            fuente.Dispose();
            return y + (int)tamano.Height + 4;
        }

        private static int DibujarQr(Graphics g, Qr qr, int y)
        {
            const int tamano = 330;
            const int relleno = 26;
            const int radioCaja = 18;

            int x = CentroPrincipalX - tamano / 2;
            var cajaExterior = new Rectangle(x - relleno, y, tamano + relleno * 2, tamano + relleno * 2);

            using (var rutaCaja = new GraphicsPath())
            {
                rutaCaja.AddArc(cajaExterior.X, cajaExterior.Y, radioCaja * 2, radioCaja * 2, 180, 90);
                rutaCaja.AddArc(cajaExterior.Right - radioCaja * 2, cajaExterior.Y, radioCaja * 2, radioCaja * 2, 270, 90);
                rutaCaja.AddArc(cajaExterior.Right - radioCaja * 2, cajaExterior.Bottom - radioCaja * 2, radioCaja * 2, radioCaja * 2, 0, 90);
                rutaCaja.AddArc(cajaExterior.X, cajaExterior.Bottom - radioCaja * 2, radioCaja * 2, radioCaja * 2, 90, 90);
                rutaCaja.CloseFigure();

                using (var pincelCaja = new SolidBrush(Color.White))
                    g.FillPath(pincelCaja, rutaCaja);

                using var plumaCaja = new Pen(ColorAcentoNombre, 3);
                g.DrawPath(plumaCaja, rutaCaja);
            }

            if (!string.IsNullOrEmpty(qr.RutaImagen) && File.Exists(qr.RutaImagen))
            {
                using var imagenQr = Image.FromFile(qr.RutaImagen);
                g.DrawImage(imagenQr, x, y + relleno, tamano, tamano);
            }

            return cajaExterior.Bottom;
        }

        private static void DibujarPie(Graphics g, int y)
        {
            using var fuente = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            using var pincelTexto = new SolidBrush(ColorTextoMuted);
            var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

            var area = new RectangleF(AnchoBanda, y, AnchoPrincipal, 24);
            g.DrawString("Escanea para registrar tu asistencia", fuente, pincelTexto, area, formato);
        }
    }
}
