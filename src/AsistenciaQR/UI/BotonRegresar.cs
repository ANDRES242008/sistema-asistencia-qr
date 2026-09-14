using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{

    public class BotonRegresar : Panel
    {
        private bool _hover;

        public BotonRegresar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            Cursor = Cursors.Hand;

            Size = new Size(160, 44);

            MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var area = new Rectangle(0, 0, Math.Max(Width - 1, 1), Math.Max(Height - 1, 1));
            int radio = Math.Min(Height / 2, 22);
            using var ruta = Formas.Redondeada(area, radio);

            Color fondo = _hover ? Color.FromArgb(226, 232, 240) : Color.FromArgb(240, 243, 247);
            using (var pincel = new SolidBrush(fondo))
                g.FillPath(pincel, ruta);

            using var borde = new Pen(Color.FromArgb(206, 213, 222), 1);
            g.DrawPath(borde, ruta);


            float tamanoFuente = Math.Max(Height * 0.25f, 9f);
            using var fuente = new Font("Segoe UI", tamanoFuente, FontStyle.Bold);
            TextRenderer.DrawText(g, "←   Regresar", fuente, area, Colores.AzulMarino,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}