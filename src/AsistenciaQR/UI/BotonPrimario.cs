using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{
    /// <summary>
    /// Boton con degradado verde-azul y esquinas redondeadas, para
    /// las acciones principales en toda la Fase 4 (Iniciar Sesion,
    /// Guardar, etc.), tal como en el boceto de diseno.
    /// </summary>
    public class BotonPrimario : Button
    {
        public BotonPrimario()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Colores.VerdeEsmeralda;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 11, FontStyle.Bold);
            Cursor = Cursors.Hand;
            Height = 44;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var ruta = ObtenerRutaRedondeada(ClientRectangle, 8);
            using var degradado = new LinearGradientBrush(
                ClientRectangle, Colores.VerdeEsmeralda, Colores.AzulDegradado, LinearGradientMode.Horizontal);

            g.FillPath(degradado, ruta);

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static GraphicsPath ObtenerRutaRedondeada(Rectangle area, int radio)
        {
            var ruta = new GraphicsPath();
            int diametro = radio * 2;

            ruta.AddArc(area.X, area.Y, diametro, diametro, 180, 90);
            ruta.AddArc(area.Right - diametro, area.Y, diametro, diametro, 270, 90);
            ruta.AddArc(area.Right - diametro, area.Bottom - diametro, diametro, diametro, 0, 90);
            ruta.AddArc(area.X, area.Bottom - diametro, diametro, diametro, 90, 90);
            ruta.CloseFigure();

            return ruta;
        }
    }
}
