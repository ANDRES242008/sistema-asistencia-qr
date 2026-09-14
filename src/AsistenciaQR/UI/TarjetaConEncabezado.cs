using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{
    /// <summary>
    /// Tarjeta con esquinas redondeadas y un encabezado de color en
    /// la parte de arriba, como las tarjetas del boceto de
    /// referencia del Kiosco. Agrega tus controles a ContenidoPanel,
    /// nunca directo a la tarjeta.
    /// </summary>
    public class TarjetaConEncabezado : Panel
    {
        private const int Radio = 14;

        private int _alturaEncabezado = 46;
        private string _textoEncabezado = string.Empty;
        private Color _colorEncabezado = Colores.AzulMarino;
        private Font _fuenteEncabezado = new("Segoe UI", 11, FontStyle.Bold);

        public Panel ContenidoPanel { get; }

        public int AlturaEncabezado
        {
            get => _alturaEncabezado;
            set { _alturaEncabezado = value; ReubicarContenido(); Invalidate(); }
        }

        public string TextoEncabezado
        {
            get => _textoEncabezado;
            set { _textoEncabezado = value; Invalidate(); }
        }

        public Color ColorEncabezado
        {
            get => _colorEncabezado;
            set { _colorEncabezado = value; Invalidate(); }
        }

        public Font FuenteEncabezado
        {
            get => _fuenteEncabezado;
            set { _fuenteEncabezado = value; Invalidate(); }
        }

        public TarjetaConEncabezado()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Colores.BlancoPuro;

            ContenidoPanel = new Panel { BackColor = Colores.BlancoPuro };
            Controls.Add(ContenidoPanel);

            Resize += (_, _) => ReubicarContenido();
            ReubicarContenido();
        }

        private void ReubicarContenido()
        {
            ContenidoPanel.Location = new Point(1, _alturaEncabezado);
            ContenidoPanel.Size = new Size(Math.Max(Width - 2, 0), Math.Max(Height - _alturaEncabezado - 1, 0));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var areaCompleta = new Rectangle(0, 0, Width - 1, Height - 1);
            using var rutaCompleta = Formas.Redondeada(areaCompleta, Radio);

            using (var fondo = new SolidBrush(Colores.BlancoPuro))
                g.FillPath(fondo, rutaCompleta);

            if (_alturaEncabezado > 0)
            {
                var areaEncabezado = new Rectangle(0, 0, Width, _alturaEncabezado);
                using var rutaEncabezado = Formas.RedondeadaArriba(areaEncabezado, Radio);
                using var pincelEncabezado = new SolidBrush(_colorEncabezado);
                g.FillPath(pincelEncabezado, rutaEncabezado);

                var areaTexto = new Rectangle(14, 0, Math.Max(Width - 28, 0), _alturaEncabezado);
                TextRenderer.DrawText(g, _textoEncabezado, _fuenteEncabezado, areaTexto, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            using var borde = new Pen(Color.FromArgb(225, 228, 232), 1);
            g.DrawPath(borde, rutaCompleta);
        }
    }
}
