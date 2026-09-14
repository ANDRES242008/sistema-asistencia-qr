using System.Drawing;
using System.Drawing.Drawing2D;

namespace AsistenciaQR.UI
{
    /// <summary>
    /// Formas reutilizables para dibujar controles con esquinas
    /// redondeadas en toda la Fase 4.
    /// </summary>
    public static class Formas
    {
        public static GraphicsPath Redondeada(Rectangle area, int radio)
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

        /// <summary>Rectangulo con solo las esquinas de ARRIBA redondeadas (encabezados de tarjeta).</summary>
        public static GraphicsPath RedondeadaArriba(Rectangle area, int radio)
        {
            var ruta = new GraphicsPath();
            int diametro = radio * 2;

            ruta.AddArc(area.X, area.Y, diametro, diametro, 180, 90);
            ruta.AddArc(area.Right - diametro, area.Y, diametro, diametro, 270, 90);
            ruta.AddLine(area.Right, area.Y + radio, area.Right, area.Bottom);
            ruta.AddLine(area.Right, area.Bottom, area.X, area.Bottom);
            ruta.CloseFigure();

            return ruta;
        }
    }
}
