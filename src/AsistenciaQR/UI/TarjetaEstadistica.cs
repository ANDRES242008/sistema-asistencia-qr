using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{
    /// <summary>
    /// Tarjeta de color solido con un titulo y un numero grande,
    /// para las estadisticas del Dashboard. La posicion del numero
    /// se calcula midiendo el texto de verdad (no con porcentajes
    /// fijos), para que nunca se corte sin importar el tamano de fuente.
    /// </summary>
    public class TarjetaEstadistica : Panel
    {
        private const int Radio = 14;

        private string _titulo = string.Empty;
        private string _valor = "0";
        private Font _fuenteTitulo = new("Segoe UI", 10);
        private Font _fuenteValor = new("Segoe UI", 26, FontStyle.Bold);

        public string Titulo { get => _titulo; set { _titulo = value; Invalidate(); } }
        public string Valor { get => _valor; set { _valor = value; Invalidate(); } }
        public Color ColorFondo { get; set; } = Colores.VerdeEsmeralda;

        public Font FuenteTitulo { get => _fuenteTitulo; set { _fuenteTitulo = value; Invalidate(); } }
        public Font FuenteValor { get => _fuenteValor; set { _fuenteValor = value; Invalidate(); } }

        public TarjetaEstadistica()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var area = new Rectangle(0, 0, Math.Max(Width - 1, 1), Math.Max(Height - 1, 1));
            using var ruta = Formas.Redondeada(area, Radio);

            using (var fondo = new SolidBrush(ColorFondo))
                g.FillPath(fondo, ruta);

            int diametroArco = (int)(Height * 0.95);
            var areaArco = new Rectangle(Width - diametroArco + 10, (Height - diametroArco) / 2, diametroArco, diametroArco);
            using var plumaArco = new Pen(Color.FromArgb(60, Color.White), 7);
            g.DrawArc(plumaArco, areaArco, -100, 260);

            int margen = Math.Max((int)(Width * 0.09), 12);
            int margenSuperior = Math.Max((int)(Height * 0.12), 10);

            // Se mide el titulo de verdad para saber donde termina, en
            // vez de adivinar con un porcentaje fijo del alto (eso era
            // lo que hacia que el numero terminara "muy abajo" y se
            // cortara con fuentes grandes).
            var tamanoTitulo = TextRenderer.MeasureText(_titulo, _fuenteTitulo);
            var areaTitulo = new Rectangle(margen, margenSuperior, Width - margen * 2, tamanoTitulo.Height);
            TextRenderer.DrawText(g, _titulo, _fuenteTitulo, areaTitulo, Color.White,
                TextFormatFlags.Left | TextFormatFlags.Top);

            int espacioEntre = Math.Max((int)(Height * 0.05), 6);
            int yValor = areaTitulo.Bottom + espacioEntre;
            int altoValorDisponible = Math.Max(Height - yValor - 8, 20);
            var areaValor = new Rectangle(margen, yValor, Width - margen * 2, altoValorDisponible);

            TextRenderer.DrawText(g, _valor, _fuenteValor, areaValor, Color.White,
                TextFormatFlags.Left | TextFormatFlags.Top);
        }
    }
}
