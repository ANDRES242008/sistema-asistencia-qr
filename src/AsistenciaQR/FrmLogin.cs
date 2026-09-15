using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    
    public class FrmLogin : Form
    {
        private const int AnchoColumnaBase = 380;
        private const int AnchoPanelIzquierdo = 500; 


        private readonly AuthService _authService = new();
        private float _escala = 1f;

        private Panel _panelIzquierdo = null!;
        private Panel _panelDerecho = null!;
        private const string RutaLogo = "Assets\\login_logo.jpg";

        private PictureBox _picLogo = null!;

        private Label _lblTitulo = null!;
        private Label _lblSubtitulo = null!;
        private Label _lblUsuario = null!;
        private CampoTexto _campoUsuario = null!;
        private Label _lblContrasena = null!;
        private CampoTexto _campoContrasena = null!;
        private Label _lblRol = null!;
        private ComboBox _cmbRol = null!;
        private CheckBox _chkRecordarme = null!;
        private BotonPrimario _btnIngresar = null!;
        private Label _lblError = null!;

        public FrmLogin()
        {
            ConstruirInterfaz();
            Load += (_, _) => AplicarEscala();
            CargarUsuarioRecordado();
        }

        private void IntentarLogin()
        {
            string usuario = _campoUsuario.ObtenerValorReal().Trim();
            string contrasena = _campoContrasena.ObtenerValorReal();

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
            {
                MostrarError("Completa el usuario y la contraseña.");
                return;
            }

            var usuarioValido = _authService.ValidarLogin(usuario, contrasena);

            if (usuarioValido is null)
            {
                MostrarError("Usuario o contrasena incorrectos.");
                return;
            }

            // Unico agregado antes: guardar quien inicio sesion para
            // que BarraLateral sepa que rol tiene.
            SesionActual.NombreUsuario = usuario;
            SesionActual.NombreCompleto = usuarioValido.NombreCompleto;
            SesionActual.Rol = usuarioValido.Rol;

            
            GuardarPreferenciaRecordar(usuario);

            // Ya no abre el Dashboard aqui. Program.cs es quien lo
            // abre, despues de que este ShowDialog() devuelva OK.
            DialogResult = DialogResult.OK;
            Close();
        }


        private void CargarUsuarioRecordado()
        {
            try
            {
                if (Properties.Settings.Default.Recordarme)
                {
                    _campoUsuario.Text = Properties.Settings.Default.UsuarioGuardado;
                    _chkRecordarme.Checked = true;
                    _campoContrasena.Focus(); 
                }
            }
            catch
            {
              
            }
        }

        private void GuardarPreferenciaRecordar(string usuario)
        {
            try
            {
                if (_chkRecordarme.Checked)
                {
                    Properties.Settings.Default.UsuarioGuardado = usuario;
                    Properties.Settings.Default.Recordarme = true;
                }
                else
                {
                    Properties.Settings.Default.UsuarioGuardado = string.Empty;
                    Properties.Settings.Default.Recordarme = false;
                }

                Properties.Settings.Default.Save();
            }
            catch
            {
                
            }
        }


        private void MostrarError(string mensaje)
        {
            _lblError.Text = mensaje;
            _lblError.Visible = true;
            AplicarEscala();
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Iniciar Sesion";
            Width = 1000;
            Height = 600;
            MinimumSize = new Size(900, 540);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
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
                Text = "Bienvenido de nuevo",
                ForeColor = Colores.AzulMarino,
                AutoSize = true
            };

            _lblSubtitulo = new Label
            {
                Text = "Ingresa tus credenciales para continuar",
                ForeColor = Colores.GrisSecundario,
                AutoSize = true
            };

            _lblUsuario = new Label
            {
                Text = "Usuario",
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            _campoUsuario = new CampoTexto("\U0001F464", "Usuario");

            _lblContrasena = new Label
            {
                Text = "Contrasena",
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            _campoContrasena = new CampoTexto("\U0001F512", "Contrasena", esContrasena: true);
            _campoContrasena.EnterPresionado += IntentarLogin;

            _lblRol = new Label
            {
                Text = "Role",
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            _cmbRol = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cmbRol.Items.Add("Administrador / Docente");
            _cmbRol.SelectedIndex = 0;

            _chkRecordarme = new CheckBox
            {
                Text = "Recordarme",
                ForeColor = Colores.GrisSecundario,
                AutoSize = true
            };

            _btnIngresar = new BotonPrimario { Text = "Iniciar Sesion" };
            _btnIngresar.Click += (_, _) => IntentarLogin();
            AcceptButton = _btnIngresar;

            _lblError = new Label
            {
                Text = string.Empty,
                ForeColor = Color.Firebrick,
                AutoSize = true,
                Visible = false
            };

            _panelDerecho.Controls.AddRange(new Control[]
            {
                _lblTitulo, _lblSubtitulo, _lblUsuario, _campoUsuario,
                _lblContrasena, _campoContrasena, _lblRol, _cmbRol,
                _chkRecordarme, _btnIngresar, _lblError
            });
        }

        
        private void AplicarEscala()
        {
            if (_panelDerecho.Width <= 0) return;

            _escala = Math.Clamp(_panelDerecho.Width / 540f, 1f, 1.8f);

            int anchoColumna = (int)(AnchoColumnaBase * _escala);
            int margenX = Math.Max((_panelDerecho.Width - anchoColumna) / 2, 30);

            int y = (int)(50 * _escala);

            _lblTitulo.Font = new Font("Segoe UI", 19 * _escala, FontStyle.Bold);
            _lblTitulo.Location = new Point(margenX, y);
            y = _lblTitulo.Bottom + (int)(2 * _escala);

            _lblSubtitulo.Font = new Font("Segoe UI", 9.5f * _escala);
            _lblSubtitulo.Location = new Point(margenX, y);
            y = _lblSubtitulo.Bottom + (int)(20 * _escala);

            _lblUsuario.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _lblUsuario.Location = new Point(margenX, y);
            y = _lblUsuario.Bottom + (int)(6 * _escala);

            _campoUsuario.Width = anchoColumna;
            _campoUsuario.Location = new Point(margenX, y);
            _campoUsuario.EscalarA(_escala);
            y = _campoUsuario.Bottom + (int)(20 * _escala);

            _lblContrasena.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _lblContrasena.Location = new Point(margenX, y);
            y = _lblContrasena.Bottom + (int)(6 * _escala);

            _campoContrasena.Width = anchoColumna;
            _campoContrasena.Location = new Point(margenX, y);
            _campoContrasena.EscalarA(_escala);
            y = _campoContrasena.Bottom + (int)(20 * _escala);

            _lblRol.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _lblRol.Location = new Point(margenX, y);
            y = _lblRol.Bottom + (int)(6 * _escala);

            _cmbRol.Font = new Font("Segoe UI", 9.5f * _escala);
            _cmbRol.Width = anchoColumna;
            _cmbRol.Location = new Point(margenX, y);
            y = _cmbRol.Bottom + (int)(18 * _escala);

            _chkRecordarme.Font = new Font("Segoe UI", 9 * _escala);
            _chkRecordarme.Location = new Point(margenX, y);
            y = _chkRecordarme.Bottom + (int)(22 * _escala);

            _btnIngresar.Font = new Font("Segoe UI", 11 * _escala, FontStyle.Bold);
            _btnIngresar.Width = anchoColumna;
            _btnIngresar.Height = (int)(46 * _escala);
            _btnIngresar.Location = new Point(margenX, y);
            _btnIngresar.Invalidate();
            y = _btnIngresar.Bottom + (int)(14 * _escala);

            _lblError.Font = new Font("Segoe UI", 9 * _escala);
            _lblError.MaximumSize = new Size(anchoColumna, 0);
            _lblError.Location = new Point(margenX, y);
        }
    }
}
