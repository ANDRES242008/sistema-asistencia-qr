using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.UI
{
    /// <summary>Cada pantalla del sistema, para que la barra sepa cual marcar como activa.</summary>
    public enum SeccionMenu
    {
        Dashboard, Estudiantes, Kiosco, CarnetsQR, Reportes, Excepciones, Configuracion, Horarios, Usuarios, Bitacora
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
        private const int AlturaItemBase = 50; // 10 opciones para Administrador: deben caber a 720 px de alto
        private const int AlturaEncabezadoBase = 80; // antes 70
        private const int AlturaCerrarSesionBase = 100; // incluye "Cambiar contrasena"

        private Panel _panelEncabezado = null!;
        private Label _lblLogo = null!;
        private Label _lblInstitucion = null!;

        private readonly List<(Panel Item, Label Etiqueta, Panel BarraActiva)> _items = new();

        private Panel _panelCerrarSesion = null!;
        private Label _lblCerrarSesion = null!;
        private Label _lblCambiarContrasena = null!;

        public event Action<SeccionMenu>? SeccionSeleccionada;
        public event Action? CerrarSesionSolicitado;
        public event Action? CambiarContrasenaSolicitado;

        public BarraLateral(SeccionMenu seccionActiva)
        {
            Dock = DockStyle.Left;
            Width = AnchoBase;
            BackColor = Colores.AzulMarino;

            // Entre controles con el mismo Dock, el ULTIMO agregado
            // gana su lugar primero - por eso se agregan en orden
            // inverso al que se ven en pantalla.
            ConstruirCerrarSesion();

            AgregarItem(SeccionMenu.Bitacora, "📜", "Bitacora", seccionActiva, soloAdministrador: true);
            AgregarItem(SeccionMenu.Usuarios, "🔐", "Usuarios", seccionActiva, soloAdministrador: true);
            AgregarItem(SeccionMenu.Horarios, "⏰", "Horarios", seccionActiva, soloAdministrador: true);
            AgregarItem(SeccionMenu.Configuracion, "⚙️", "Configuracion", seccionActiva, soloAdministrador: true);
            AgregarItem(SeccionMenu.Excepciones, "✍️", "Excepciones", seccionActiva, soloAdministrador: true);
            AgregarItem(SeccionMenu.Reportes, "📈", "Reportes", seccionActiva);
            AgregarItem(SeccionMenu.CarnetsQR, "🪪", "Carnets y QR", seccionActiva, soloAdministrador: true);
            AgregarItem(SeccionMenu.Kiosco, "✅", "Asistencia del Dia", seccionActiva);
            AgregarItem(SeccionMenu.Estudiantes, "👥", "Estudiantes", seccionActiva, soloAdministrador: true);
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
                Location = new Point(20, 18),
                AutoSize = true
            };

            _lblInstitucion = new Label
            {
                Text = new InstitucionService().Obtener().Nombre,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 175, 195),
                Location = new Point(22, 50),
                AutoEllipsis = true,
                Size = new Size(AnchoBase - 30, 18)
            };

            _panelEncabezado.Controls.Add(_lblLogo);
            _panelEncabezado.Controls.Add(_lblInstitucion);
            Controls.Add(_panelEncabezado);
            AjustarLogo(12);
        }

        /// <summary>
        /// Con la escala de Windows en 125% o mas, "AsistenciaQR" no cabia
        /// en el ancho del menu: se baja la letra lo necesario.
        /// </summary>
        private void AjustarLogo(float tamanoMaximo)
        {
            float tamano = tamanoMaximo;
            var fuente = new Font("Segoe UI Emoji", tamano, FontStyle.Bold);
            // El emoji del escudo mide mas de lo que reporta MeasureText:
            // se deja un margen amplio a la derecha.
            while (tamano > 8 && TextRenderer.MeasureText(_lblLogo.Text, fuente).Width * 1.15f > Width - _lblLogo.Left - 12)
            {
                fuente.Dispose();
                tamano -= 0.5f;
                fuente = new Font("Segoe UI Emoji", tamano, FontStyle.Bold);
            }
            _lblLogo.Font = fuente;
        }

        private void AgregarItem(SeccionMenu seccion, string icono, string texto, SeccionMenu seccionActiva, bool soloAdministrador = false)
        {
            // Si es una seccion solo-Administrador y quien inicio
            // sesion es Docente, ni se construye el boton.
            if (soloAdministrador && !SesionActual.EsAdministrador) return;

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

            // Disponible para cualquier rol: cada quien cambia la suya.
            _lblCambiarContrasena = new Label
            {
                Text = "🔑  Cambiar contrasena",
                Font = new Font("Segoe UI Emoji", 8.5f),
                ForeColor = Color.FromArgb(200, 210, 225),
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _lblCambiarContrasena.Click += (_, _) => CambiarContrasenaSolicitado?.Invoke();

            _panelCerrarSesion.Controls.Add(_lblCerrarSesion);
            _panelCerrarSesion.Controls.Add(_lblCambiarContrasena);
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

            AjustarLogo(12 * escala);
            _lblLogo.Location = new Point((int)(20 * escala), (int)(18 * escala));
            _lblInstitucion.Font = new Font("Segoe UI", 8.5f * escala);
            _lblInstitucion.Location = new Point((int)(22 * escala), (int)(50 * escala));
            _lblInstitucion.Size = new Size(Width - (int)(30 * escala), (int)(18 * escala));
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
            _lblCambiarContrasena.Font = new Font("Segoe UI Emoji", 8.5f * escala);
            _lblCambiarContrasena.Height = (int)(26 * escala);
        }
    }
}
