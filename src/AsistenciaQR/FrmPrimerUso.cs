using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Asistente de primer uso. Aparece solo cuando la base de datos
    /// no tiene ninguna cuenta, para crear el primer Administrador sin
    /// tener que tocar el codigo. Usa el mismo diseno que FrmLogin.
    /// </summary>
    public class FrmPrimerUso : Form
    {
        private const int AnchoColumnaBase = 380;
        private const int AnchoPanelIzquierdo = 500;
        private const string RutaLogo = "Assets\\login_logo.jpg";

        private readonly AuthService _authService = new();
        private float _escala = 1f;

        private Panel _panelIzquierdo = null!;
        private Panel _panelDerecho = null!;
        private PictureBox _picLogo = null!;

        private Label _lblTitulo = null!;
        private Label _lblSubtitulo = null!;
        private Label _lblNombre = null!;
        private CampoTexto _campoNombre = null!;
        private Label _lblUsuario = null!;
        private CampoTexto _campoUsuario = null!;
        private Label _lblContrasena = null!;
        private CampoTexto _campoContrasena = null!;
        private Label _lblConfirmar = null!;
        private CampoTexto _campoConfirmar = null!;
        private BotonPrimario _btnCrear = null!;
        private Label _lblError = null!;

        public FrmPrimerUso()
        {
            ConstruirInterfaz();
            Load += (_, _) => AplicarEscala();
        }

        private void IntentarCrear()
        {
            string nombre = _campoNombre.ObtenerValorReal().Trim();
            string usuario = _campoUsuario.ObtenerValorReal().Trim();
            string contrasena = _campoContrasena.ObtenerValorReal();
            string confirmacion = _campoConfirmar.ObtenerValorReal();

            if (contrasena != confirmacion)
            {
                MostrarError("Las contrasenas no coinciden.");
                return;
            }

            var resultado = _authService.Registrar(usuario, nombre, contrasena, AuthService.RolAdministrador);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.Mensaje);
                return;
            }

            MessageBox.Show(this,
                $"Cuenta de Administrador \"{usuario}\" creada.\nAhora inicia sesion con ella.",
                "AsistenciaQR", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void MostrarError(string mensaje)
        {
            _lblError.Text = mensaje;
            _lblError.Visible = true;
            AplicarEscala();
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Configuracion inicial";
            Width = 1000;
            Height = 680;
            MinimumSize = new Size(900, 620);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Colores.BlancoPuro;

            ConstruirPanelIzquierdo();
            ConstruirPanelDerecho();

            Controls.Add(_panelDerecho);
            Controls.Add(_panelIzquierdo);

            _panelDerecho.Resize += (_, _) => AplicarEscala();
        }

        private void ConstruirPanelIzquierdo()
        {
            _panelIzquierdo = new Panel
            {
                Dock = DockStyle.Left,
                Width = AnchoPanelIzquierdo,
                BackColor = Colores.AzulMarino
            };

            _picLogo = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Colores.AzulMarino
            };

            string rutaCompleta = Path.Combine(AppContext.BaseDirectory, RutaLogo);
            if (File.Exists(rutaCompleta))
            {
                _picLogo.Image = Image.FromFile(rutaCompleta);
            }

            _panelIzquierdo.Controls.Add(_picLogo);
        }

        private void ConstruirPanelDerecho()
        {
            _panelDerecho = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Colores.BlancoPuro
            };

            _lblTitulo = new Label
            {
                Text = "Configuracion inicial",
                ForeColor = Colores.AzulMarino,
                AutoSize = true
            };

            _lblSubtitulo = new Label
            {
                Text = "Crea la cuenta de Administrador para empezar",
                ForeColor = Colores.GrisSecundario,
                AutoSize = true
            };

            _lblNombre = CrearEtiqueta("Nombre completo");
            _campoNombre = new CampoTexto("\U0001F4DD", "Nombre completo");

            _lblUsuario = CrearEtiqueta("Usuario");
            _campoUsuario = new CampoTexto("\U0001F464", "Usuario");

            _lblContrasena = CrearEtiqueta($"Contrasena (minimo {AuthService.LongitudMinimaContrasena} caracteres)");
            _campoContrasena = new CampoTexto("\U0001F512", "Contrasena", esContrasena: true);

            _lblConfirmar = CrearEtiqueta("Confirmar contrasena");
            _campoConfirmar = new CampoTexto("\U0001F512", "Confirmar contrasena", esContrasena: true);
            _campoConfirmar.EnterPresionado += IntentarCrear;

            _btnCrear = new BotonPrimario { Text = "Crear Administrador" };
            _btnCrear.Click += (_, _) => IntentarCrear();
            AcceptButton = _btnCrear;

            _lblError = new Label
            {
                Text = string.Empty,
                ForeColor = Color.Firebrick,
                AutoSize = true,
                Visible = false
            };

            _panelDerecho.Controls.AddRange(new Control[]
            {
                _lblTitulo, _lblSubtitulo, _lblNombre, _campoNombre,
                _lblUsuario, _campoUsuario, _lblContrasena, _campoContrasena,
                _lblConfirmar, _campoConfirmar, _btnCrear, _lblError
            });
        }

        private static Label CrearEtiqueta(string texto) => new()
        {
            Text = texto,
            ForeColor = Colores.GrisOscuro,
            AutoSize = true
        };

        private void AplicarEscala()
        {
            if (_panelDerecho.Width <= 0) return;

            _escala = Math.Clamp(_panelDerecho.Width / 540f, 1f, 1.8f);

            int anchoColumna = (int)(AnchoColumnaBase * _escala);
            int margenX = Math.Max((_panelDerecho.Width - anchoColumna) / 2, 30);

            int y = (int)(40 * _escala);

            _lblTitulo.Font = new Font("Segoe UI", 19 * _escala, FontStyle.Bold);
            _lblTitulo.Location = new Point(margenX, y);
            y = _lblTitulo.Bottom + (int)(2 * _escala);

            _lblSubtitulo.Font = new Font("Segoe UI", 9.5f * _escala);
            _lblSubtitulo.Location = new Point(margenX, y);
            y = _lblSubtitulo.Bottom + (int)(18 * _escala);

            y = UbicarCampo(_lblNombre, _campoNombre, margenX, y, anchoColumna);
            y = UbicarCampo(_lblUsuario, _campoUsuario, margenX, y, anchoColumna);
            y = UbicarCampo(_lblContrasena, _campoContrasena, margenX, y, anchoColumna);
            y = UbicarCampo(_lblConfirmar, _campoConfirmar, margenX, y, anchoColumna);
            y += (int)(6 * _escala);

            _btnCrear.Font = new Font("Segoe UI", 11 * _escala, FontStyle.Bold);
            _btnCrear.Width = anchoColumna;
            _btnCrear.Height = (int)(46 * _escala);
            _btnCrear.Location = new Point(margenX, y);
            _btnCrear.Invalidate();
            y = _btnCrear.Bottom + (int)(14 * _escala);

            _lblError.Font = new Font("Segoe UI", 9 * _escala);
            _lblError.MaximumSize = new Size(anchoColumna, 0);
            _lblError.Location = new Point(margenX, y);
        }

        private int UbicarCampo(Label etiqueta, CampoTexto campo, int x, int y, int ancho)
        {
            etiqueta.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            etiqueta.Location = new Point(x, y);
            y = etiqueta.Bottom + (int)(6 * _escala);

            campo.Width = ancho;
            campo.Location = new Point(x, y);
            campo.EscalarA(_escala);
            return campo.Bottom + (int)(16 * _escala);
        }
    }
}
