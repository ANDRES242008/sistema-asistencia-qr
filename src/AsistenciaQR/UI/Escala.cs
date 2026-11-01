using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{
    /// <summary>
    /// Ayuda a que las ventanas de medidas fijas se vean bien con la
    /// escala de Windows en 125% o 150%: las fuentes ya crecen solas
    /// (estan en puntos), pero los tamanos en pixeles no. Estas
    /// funciones convierten pixeles "de diseno" (a 100%) a pixeles
    /// reales y crean botones que se ajustan a su texto.
    /// </summary>
    public static class Escala
    {
        public static float Factor(Control control) => control.DeviceDpi / 96f;

        public static int Px(Control control, int pixelesDiseno) =>
            (int)Math.Round(pixelesDiseno * Factor(control));

        public static Size Px(Control control, int ancho, int alto) => new(Px(control, ancho), Px(control, alto));

        /// <summary>
        /// Tamano de ventana escalado, sin pasarse del area visible de
        /// la pantalla (por ejemplo, una laptop 1366x768 al 125%).
        /// </summary>
        public static Size Ventana(Form form, int ancho, int alto)
        {
            var area = Screen.FromControl(form).WorkingArea;
            return new Size(Math.Min(Px(form, ancho), area.Width - 20), Math.Min(Px(form, alto), area.Height - 20));
        }

        /// <summary>
        /// Ancho para que el texto de un boton quepa en una linea, con
        /// el margen indicado a cada lado (en pixeles reales).
        /// </summary>
        public static int AnchoTexto(Control boton, int margenLateral) =>
            // +15%: los emoji de color miden mas de lo que reporta MeasureText.
            (int)(TextRenderer.MeasureText(boton.Text, boton.Font).Width * 1.15f) + margenLateral * 2;

        /// <summary>Boton plano que toma el ancho de su texto.</summary>
        public static Button Boton(string texto, bool peligro = false, int altoDiseno = 38)
        {
            var boton = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                BackColor = peligro ? Color.FromArgb(255, 240, 240) : Colores.BlancoPuro,
                ForeColor = peligro ? Color.Firebrick : Colores.GrisOscuro,
                Font = new Font("Segoe UI Emoji", 9.5f),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(14, 0, 14, 0),
                MinimumSize = new Size(0, altoDiseno)
            };
            boton.FlatAppearance.BorderColor = peligro ? Color.FromArgb(240, 200, 200) : Color.FromArgb(219, 224, 230);
            boton.HandleCreated += (_, _) => boton.MinimumSize = new Size(0, Px(boton, altoDiseno));
            return boton;
        }
    }
}
