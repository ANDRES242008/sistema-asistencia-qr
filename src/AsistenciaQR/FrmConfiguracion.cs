using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR
{
    /// <summary>
    /// Pantalla 8: Configuracion del Sistema. Permite elegir que
    /// camara usar (con vista previa en vivo para confirmar cual es
    /// cual antes de guardar), ajustar el cooldown de lecturas
    /// repetidas del Kiosco, y probar la conexion a SQL Server.
    /// Todo se guarda en un archivo local, fuera del repositorio de
    /// Git, porque es especifico de esta computadora.
    /// </summary>
    public class FrmConfiguracion : Form
    {
        private readonly ConfiguracionService _configuracionService = new();
        private CamaraQRService? _camaraPrueba;

        private ComboBox _cmbCamara = null!;
        private Button _btnProbarCamara = null!;
        private Button _btnDetenerPrueba = null!;
        private PictureBox _picPrueba = null!;
        private Label _lblEstadoCamara = null!;

        private NumericUpDown _numCooldown = null!;

        private Button _btnProbarSql = null!;
        private Label _lblResultadoSql = null!;

        private Button _btnGuardar = null!;
        private Label _lblGuardado = null!;

        public FrmConfiguracion()
        {
            ConstruirInterfaz();
            Load += (_, _) => CargarConfiguracionActual();
            FormClosing += (_, _) => DetenerPruebaCamara();
        }

        private void CargarConfiguracionActual()
        {
            var config = _configuracionService.Cargar();
            _cmbCamara.SelectedIndex = Math.Min(Math.Max(config.IndiceCamara, 0), _cmbCamara.Items.Count - 1);
            _numCooldown.Value = Math.Min(Math.Max(config.SegundosCooldown, (int)_numCooldown.Minimum), (int)_numCooldown.Maximum);
        }

        private void BtnProbarCamara_Click(object? sender, EventArgs e)
        {
            DetenerPruebaCamara();

            _lblEstadoCamara.ForeColor = Color.FromArgb(216, 152, 22);
            _lblEstadoCamara.Text = "Conectando...";

            _camaraPrueba = new CamaraQRService();
            _camaraPrueba.FrameCapturado += OnFrameDePrueba;

            try
            {
                _camaraPrueba.Iniciar(_cmbCamara.SelectedIndex);
                _btnDetenerPrueba.Enabled = true;
            }
            catch (Exception ex)
            {
                _camaraPrueba.Dispose();
                _camaraPrueba = null;

                _lblEstadoCamara.ForeColor = Color.FromArgb(200, 55, 55);
                _lblEstadoCamara.Text = "No se pudo abrir esta camara.";
                MessageBox.Show(this, ex.Message, "Error de camara",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnFrameDePrueba(Bitmap frame)
        {
            if (IsDisposed) return;

            BeginInvoke(() =>
            {
                var anterior = _picPrueba.Image;
                _picPrueba.Image = frame;
                anterior?.Dispose();

                _lblEstadoCamara.ForeColor = Color.FromArgb(46, 160, 67);
                _lblEstadoCamara.Text = $"Camara activa\n{frame.Width}x{frame.Height}";
            });
        }

        private void BtnDetenerPrueba_Click(object? sender, EventArgs e) => DetenerPruebaCamara();

        private void DetenerPruebaCamara()
        {
            if (_camaraPrueba is null) return;

            _camaraPrueba.Detener();
            _camaraPrueba.Dispose();
            _camaraPrueba = null;

            _picPrueba.Image?.Dispose();
            _picPrueba.Image = null;

            _lblEstadoCamara.ForeColor = Color.Gray;
            _lblEstadoCamara.Text = "Sin senal";

            _btnDetenerPrueba.Enabled = false;
        }

        private void BtnProbarSql_Click(object? sender, EventArgs e)
        {
            bool exito = _configuracionService.ProbarConexionSql(out string mensaje);
            _lblResultadoSql.ForeColor = exito ? Color.FromArgb(46, 160, 67) : Color.FromArgb(200, 55, 55);
            _lblResultadoSql.Text = mensaje;
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            var config = new ConfiguracionSistema
            {
                IndiceCamara = _cmbCamara.SelectedIndex,
                SegundosCooldown = (int)_numCooldown.Value
            };

            _configuracionService.Guardar(config);

            _lblGuardado.ForeColor = Color.FromArgb(46, 160, 67);
            _lblGuardado.Text = "Configuracion guardada. Se aplica la proxima vez que abras el Kiosco.";
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Configuracion del Sistema";
            Width = 680;
            Height = 640;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            var lblCamaraTitulo = new Label
            {
                Text = "Camara para el Kiosco de Escaneo",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            var lblTip = new Label
            {
                Text = "Tip: la camara integrada suele ser la 0. Las camaras USB externas suelen ser 1, 2, etc.",
                Location = new Point(20, 50),
                Size = new Size(620, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };

            var lblCamara = new Label { Text = "Camara:", Location = new Point(20, 80), AutoSize = true };
            _cmbCamara = new ComboBox
            {
                Location = new Point(90, 77),
                Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbCamara.Items.AddRange(new object[] { "Camara 0", "Camara 1", "Camara 2", "Camara 3", "Camara 4" });
            _cmbCamara.SelectedIndex = 0;

            _btnProbarCamara = new Button
            {
                Text = "Probar camara",
                Location = new Point(245, 76),
                Size = new Size(140, 30)
            };
            _btnProbarCamara.Click += BtnProbarCamara_Click;

            _btnDetenerPrueba = new Button
            {
                Text = "Detener prueba",
                Location = new Point(395, 76),
                Size = new Size(140, 30),
                Enabled = false
            };
            _btnDetenerPrueba.Click += BtnDetenerPrueba_Click;

            _picPrueba = new PictureBox
            {
                Location = new Point(20, 120),
                Size = new Size(420, 250),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Black
            };

            _lblEstadoCamara = new Label
            {
                Text = "Sin senal",
                Location = new Point(450, 120),
                Size = new Size(200, 60),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var lblCooldownTitulo = new Label
            {
                Text = "Tiempo entre lecturas repetidas",
                Location = new Point(20, 395),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            var lblCooldown = new Label
            {
                Text = "Cooldown (segundos):",
                Location = new Point(20, 430),
                AutoSize = true
            };
            _numCooldown = new NumericUpDown
            {
                Location = new Point(200, 427),
                Size = new Size(60, 28),
                Minimum = 1,
                Maximum = 30,
                Value = 4
            };

            var lblSqlTitulo = new Label
            {
                Text = "Conexion a SQL Server",
                Location = new Point(340, 395),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            _btnProbarSql = new Button
            {
                Text = "Probar conexion SQL",
                Location = new Point(340, 425),
                Size = new Size(170, 32)
            };
            _btnProbarSql.Click += BtnProbarSql_Click;

            _lblResultadoSql = new Label
            {
                Text = string.Empty,
                Location = new Point(20, 470),
                Size = new Size(620, 40)
            };

            _btnGuardar = new Button
            {
                Text = "Guardar configuracion",
                Location = new Point(20, 525),
                Size = new Size(220, 40),
                Font = new Font("Segoe UI", 10)
            };
            _btnGuardar.Click += BtnGuardar_Click;

            _lblGuardado = new Label
            {
                Text = string.Empty,
                Location = new Point(20, 575),
                Size = new Size(620, 30)
            };

            Controls.Add(lblCamaraTitulo);
            Controls.Add(lblTip);
            Controls.Add(lblCamara);
            Controls.Add(_cmbCamara);
            Controls.Add(_btnProbarCamara);
            Controls.Add(_btnDetenerPrueba);
            Controls.Add(_picPrueba);
            Controls.Add(_lblEstadoCamara);
            Controls.Add(lblCooldownTitulo);
            Controls.Add(lblCooldown);
            Controls.Add(_numCooldown);
            Controls.Add(lblSqlTitulo);
            Controls.Add(_btnProbarSql);
            Controls.Add(_lblResultadoSql);
            Controls.Add(_btnGuardar);
            Controls.Add(_lblGuardado);
        }
    }
}
