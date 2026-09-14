using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{
    /// <summary>
    /// Tarjeta blanca simple con esquinas redondeadas y borde suave,
    /// sin encabezado de color, para paneles de detalle/formulario.
    /// </summary>
    public class TarjetaBlanca : Panel
    {
        private const int Radio = 14;

        public TarjetaBlanca()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Colores.BlancoPuro;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var area = new Rectangle(0, 0, Math.Max(Width - 1, 1), Math.Max(Height - 1, 1));
            using var ruta = Formas.Redondeada(area, Radio);

            using (var fondo = new SolidBrush(Colores.BlancoPuro))
                g.FillPath(fondo, ruta);

            using var borde = new Pen(Color.FromArgb(225, 228, 232), 1);
            g.DrawPath(borde, ruta);
        }
    }
}
