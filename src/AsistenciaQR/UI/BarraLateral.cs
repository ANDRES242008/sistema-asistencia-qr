using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{
    /// <summary>Cada pantalla del sistema, para que la barra sepa cual marcar como activa.</summary>
    public enum SeccionMenu
    {
        Dashboard, Estudiantes, Kiosco, CarnetsQR, Reportes, Excepciones, Configuracion, Horarios
    }

    /// <summary>
    /// Menu lateral de navegacion, reutilizable en todas las
    /// pantallas rediseñadas. Soporta escalado real (EscalarA): el
    /// ancho crece a la MISMA velocidad que la letra (antes tenian
    /// topes distintos y el texto largo se cortaba), y los items
    /// tienen mas alto base para que se vean menos apretados.
    /// </summary>
    public class BarraLateral : Panel
    {
        private const int AnchoBase = 240;
        private const int AlturaItemBase = 56; // antes 46, mas espacio por opcion
        private const int AlturaEncabezadoBase = 80; // antes 70
        private const int AlturaCerrarSesionBase = 74; // antes 64

        private Panel _panelEncabezado = null!;
        private Label _lblLogo = null!;

        private readonly List<(Panel Item, Label Etiqueta, Panel BarraActiva)> _items = new();

        private Panel _panelCerrarSesion = null!;
        private Label _lblCerrarSesion = null!;

        public event Action<SeccionMenu>? SeccionSeleccionada;
        public event Action? CerrarSesionSolicitado;

        public BarraLateral(SeccionMenu seccionActiva)
        {
            Dock = DockStyle.Left;
            Width = AnchoBase;
            BackColor = Colores.AzulMarino;

            // Entre controles con el mismo Dock, el ULTIMO agregado
            // gana su lugar primero - por eso se agregan en orden
            // inverso al que se ven en pantalla.
            ConstruirCerrarSesion();

            AgregarItem(SeccionMenu.Horarios, "⏰", "Horarios", seccionActiva);
            AgregarItem(SeccionMenu.Configuracion, "⚙️", "Configuracion", seccionActiva);
            AgregarItem(SeccionMenu.Excepciones, "✍️", "Excepciones", seccionActiva);
            AgregarItem(SeccionMenu.Reportes, "📈", "Reportes", seccionActiva);
            AgregarItem(SeccionMenu.CarnetsQR, "🪪", "Carnets y QR", seccionActiva);
            AgregarItem(SeccionMenu.Kiosco, "✅", "Asistencia del Dia", seccionActiva);
            AgregarItem(SeccionMenu.Estudiantes, "👥", "Estudiantes", seccionActiva);
            AgregarItem(SeccionMenu.Dashboard, "📊", "Dashboard", seccionActiva);

            ConstruirEncabezado();
        }

        private void ConstruirEncabezado()
        {
            _panelEncabezado = new Panel { Dock = DockStyle.Top, Height = AlturaEncabezadoBase };

            _lblLogo = new Label
            {
                Text = "🛡️  AsistenciaQR",
                Font = new Font("Segoe UI Emoji", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 28),
                AutoSize = true
            };

            _panelEncabezado.Controls.Add(_lblLogo);
            Controls.Add(_panelEncabezado);
        }

        private void AgregarItem(SeccionMenu seccion, string icono, string texto, SeccionMenu seccionActiva)
        {
            bool esActiva = seccion == seccionActiva;

            var item = new Panel
            {
                Height = AlturaItemBase,
                Dock = DockStyle.Top,
                BackColor = esActiva ? Color.FromArgb(30, 255, 255, 255) : Colores.AzulMarino,
                Cursor = Cursors.Hand
            };

            var barraActiva = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = esActiva ? Colores.VerdeEsmeralda : Color.Transparent
            };

            var lbl = new Label
            {
                Text = $"   {icono}   {texto}",
                Font = new Font("Segoe UI Emoji", 9.5f, esActiva ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = esActiva ? Color.White : Color.FromArgb(200, 210, 225),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            void manejarClick(object? s, EventArgs e)
            {
                if (!esActiva) SeccionSeleccionada?.Invoke(seccion);
            }
            item.Click += manejarClick;
            lbl.Click += manejarClick;

            item.Controls.Add(lbl);
            item.Controls.Add(barraActiva);
            Controls.Add(item);

            _items.Add((item, lbl, barraActiva));
        }

        private void ConstruirCerrarSesion()
        {
            _panelCerrarSesion = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = AlturaCerrarSesionBase,
                BackColor = Color.FromArgb(20, 0, 0, 0)
            };

            var separador = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(45, 255, 255, 255) };

            _lblCerrarSesion = new Label
            {
                Text = "🚪   Cerrar Sesion",
                Font = new Font("Segoe UI Emoji", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 190, 170),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _lblCerrarSesion.Click += (_, _) => CerrarSesionSolicitado?.Invoke();

            _panelCerrarSesion.Controls.Add(_lblCerrarSesion);
            _panelCerrarSesion.Controls.Add(separador);
            Controls.Add(_panelCerrarSesion);
        }

        /// <summary>
        /// Escala fuentes y tamanos de todo el menu. El ANCHO ahora
        /// crece exactamente a la misma velocidad que la letra (antes
        /// el ancho tenia un tope mas bajo que la fuente, y textos
        /// largos como "Asistencia del Dia" se cortaban).
        /// </summary>
        public void EscalarA(float escala)
        {
            Width = (int)(AnchoBase * escala);

            _lblLogo.Font = new Font("Segoe UI Emoji", 12 * escala, FontStyle.Bold);
            _panelEncabezado.Height = (int)(AlturaEncabezadoBase * escala);

            foreach (var (item, etiqueta, barraActiva) in _items)
            {
                item.Height = (int)(AlturaItemBase * escala);
                barraActiva.Width = Math.Max((int)(4 * escala), 3);
                var estiloActual = etiqueta.Font.Style;
                etiqueta.Font = new Font("Segoe UI Emoji", 9.5f * escala, estiloActual);
            }

            _panelCerrarSesion.Height = (int)(AlturaCerrarSesionBase * escala);
            _lblCerrarSesion.Font = new Font("Segoe UI Emoji", 11 * escala, FontStyle.Bold);
        }
    }
}
