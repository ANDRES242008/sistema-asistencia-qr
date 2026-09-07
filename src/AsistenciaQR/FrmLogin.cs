using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Servicios;

namespace AsistenciaQR
{
    /// <summary>
    /// Pantalla de acceso para docentes y administradores. Valida
    /// contra la tabla Usuarios (contrasenas encriptadas) y abre la
    /// pantalla principal si son correctas.
    /// </summary>
    public class FrmLogin : Form
    {
        private readonly AuthService _authService = new();

        private TextBox _txtUsuario = null!;
        private TextBox _txtContrasena = null!;
        private Label _lblError = null!;
        private Button _btnIngresar = null!;

        public FrmLogin()
        {
            ConstruirInterfaz();
        }

        private void BtnIngresar_Click(object? sender, EventArgs e) => IntentarLogin();

        private void IntentarLogin()
        {
            string usuarioTexto = _txtUsuario.Text.Trim();
            string contrasena = _txtContrasena.Text;

            if (string.IsNullOrEmpty(usuarioTexto) || string.IsNullOrEmpty(contrasena))
            {
                MostrarError("Escribe usuario y contrasena.");
                return;
            }

            var usuario = _authService.ValidarLogin(usuarioTexto, contrasena);

            if (usuario is null)
            {
                MostrarError("Usuario o contrasena incorrectos.");
                _txtContrasena.Clear();
                _txtContrasena.Focus();
                return;
            }

            Hide();
            var frmPrincipal = new Form1();
            frmPrincipal.ShowDialog();
            Close();
        }

        private void MostrarError(string mensaje)
        {
            _lblError.Text = mensaje;
            _lblError.Visible = true;
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Iniciar Sesion";
            ClientSize = new Size(360, 320);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;

            var lblTitulo = new Label
            {
                Text = "AsistenciaQR",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 25),
                Size = new Size(320, 45)
            };

            var lblSubtitulo = new Label
            {
                Text = "Acceso a docentes y administradores",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 70),
                Size = new Size(320, 40),
                ForeColor = Color.Gray
            };

            var lblUsuario = new Label { Text = "Usuario:", Location = new Point(30, 115), AutoSize = true };
            _txtUsuario = new TextBox { Location = new Point(30, 135), Size = new Size(300, 28) };

            var lblContrasena = new Label { Text = "Contrasena:", Location = new Point(30, 175), AutoSize = true };
            _txtContrasena = new TextBox
            {
                Location = new Point(30, 195),
                Size = new Size(300, 28),
                PasswordChar = '\u25CF'
            };
            _txtContrasena.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter) IntentarLogin();
            };

            _lblError = new Label
            {
                Text = string.Empty,
                ForeColor = Color.Firebrick,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 230),
                Size = new Size(320, 24),
                Visible = false
            };

            _btnIngresar = new Button
            {
                Text = "Ingresar",
                Location = new Point(30, 260),
                Size = new Size(300, 38),
                Font = new Font("Segoe UI", 10)
            };
            _btnIngresar.Click += BtnIngresar_Click;
            AcceptButton = _btnIngresar;

            Controls.Add(lblTitulo);
            Controls.Add(lblSubtitulo);
            Controls.Add(lblUsuario);
            Controls.Add(_txtUsuario);
            Controls.Add(lblContrasena);
            Controls.Add(_txtContrasena);
            Controls.Add(_lblError);
            Controls.Add(_btnIngresar);
        }
    }
}
