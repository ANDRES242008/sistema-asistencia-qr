using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{
    /// <summary>
    /// Boton de accion con esquinas redondeadas, color de fondo
    /// configurable, y un titulo + subtitulo opcional (para
    /// acciones que necesitan una aclaracion corta, como
    /// "Regenerar Codigo QR / el anterior quedara inactivo").
    /// </summary>
    public class BotonAccion : Panel
    {
        private const int Radio = 12;
        private bool _hover;
        private string _titulo = string.Empty;
        private string? _subtitulo;

        public Color ColorFondo { get; set; } = Colores.VerdeEsmeralda;
        public Color ColorTexto { get; set; } = Color.White;

        public BotonAccion()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            Cursor = Cursors.Hand;
            Size = new Size(260, 54);

            MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        }

        public void ActualizarTexto(string titulo, string? subtitulo = null)
        {
            _titulo = titulo;
            _subtitulo = subtitulo;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var area = new Rectangle(0, 0, Math.Max(Width - 1, 1), Math.Max(Height - 1, 1));
            using var ruta = Formas.Redondeada(area, Radio);

            Color fondo = _hover ? ControlPaint.Dark(ColorFondo, 0.06f) : ColorFondo;
            using (var pincel = new SolidBrush(fondo))
                g.FillPath(pincel, ruta);

            if (!Enabled)
            {
                using var overlay = new SolidBrush(Color.FromArgb(120, Colores.GrisClaro));
                g.FillPath(overlay, ruta);
            }

            if (string.IsNullOrEmpty(_subtitulo))
            {
                using var fuente = new Font("Segoe UI Emoji", Math.Max(Height * 0.24f, 8f), FontStyle.Bold);
                TextRenderer.DrawText(g, _titulo, fuente, area, ColorTexto,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            using var fuenteTitulo = new Font("Segoe UI Emoji", Math.Max(Height * 0.22f, 8f), FontStyle.Bold);
            using var fuenteSub = new Font("Segoe UI", Math.Max(Height * 0.14f, 7f));

            var medidaTitulo = TextRenderer.MeasureText(_titulo, fuenteTitulo);
            var medidaSub = TextRenderer.MeasureText(_subtitulo, fuenteSub);
            int altoTotal = medidaTitulo.Height + medidaSub.Height + 2;
            int yInicio = Math.Max((Height - altoTotal) / 2, 4);

            TextRenderer.DrawText(g, _titulo, fuenteTitulo, new Rectangle(0, yInicio, Width, medidaTitulo.Height),
                ColorTexto, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
            TextRenderer.DrawText(g, _subtitulo, fuenteSub, new Rectangle(0, yInicio + medidaTitulo.Height + 2, Width, medidaSub.Height),
                Color.FromArgb(215, ColorTexto), TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
        }
    }
}
