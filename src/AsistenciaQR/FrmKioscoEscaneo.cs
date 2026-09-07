using System.Drawing;
using System.Media;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR
{
    /// <summary>
    /// Pantalla principal del kiosco: muestra la camara en vivo,
    /// procesa los codigos QR detectados, y ofrece un respaldo
    /// manual (escribir el NIE) por si la camara falla.
    /// </summary>
    public class FrmKioscoEscaneo : Form
    {
        private readonly CamaraQRService _camaraService = new();
        private readonly AsistenciaService _asistenciaService = new();

        private PictureBox _pictureCamara = null!;
        private Panel _panelAlerta = null!;
        private Label _lblEstado = null!;
        private Label _lblNombre = null!;
        private Label _lblNie = null!;
        private Label _lblGrado = null!;
        private Label _lblHora = null!;
        private TextBox _txtNieManual = null!;
        private Button _btnRegistrarManual = null!;

        public FrmKioscoEscaneo()
        {
            ConstruirInterfaz();

            _camaraService.FrameCapturado += ActualizarFrame;
            _camaraService.CodigoDetectado += ProcesarCodigoDetectado;

            Load += (_, _) => IniciarCamara();
            FormClosing += (_, _) =>
            {
                _camaraService.Detener();
                _pictureCamara.Image?.Dispose();
            };
        }

        private void IniciarCamara()
        {
            try
            {
                _camaraService.Iniciar(indiceCamara: 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error de camara",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ActualizarFrame(Bitmap frame)
        {
            if (IsDisposed) return;

            BeginInvoke(() =>
            {
                var anterior = _pictureCamara.Image;
                _pictureCamara.Image = frame;
                anterior?.Dispose();
            });
        }

        private void ProcesarCodigoDetectado(string codigoQr)
        {
            var resultado = _asistenciaService.RegistrarPorCodigoQr(codigoQr);
            MostrarResultado(resultado);
        }

        private void BtnRegistrarManual_Click(object? sender, EventArgs e)
        {
            string nie = _txtNieManual.Text.Trim();
            if (string.IsNullOrEmpty(nie))
            {
                MessageBox.Show(this, "Escribe un NIE valido.", "Dato faltante",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var resultado = _asistenciaService.RegistrarPorNie(nie);
            MostrarResultado(resultado);
            _txtNieManual.Clear();
        }

        private void MostrarResultado(ResultadoAsistencia resultado)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => MostrarResultado(resultado));
                return;
            }

            switch (resultado.Estado)
            {
                case ResultadoEscaneo.Exitoso:
                    _panelAlerta.BackColor = Color.FromArgb(46, 160, 67);
                    _lblEstado.Text = "Asistencia registrada";
                    SystemSounds.Asterisk.Play();
                    break;

                case ResultadoEscaneo.YaRegistradoHoy:
                    _panelAlerta.BackColor = Color.FromArgb(216, 152, 22);
                    _lblEstado.Text = "Ya registraste tu asistencia hoy";
                    SystemSounds.Hand.Play();
                    break;

                case ResultadoEscaneo.CodigoNoValido:
                    _panelAlerta.BackColor = Color.FromArgb(200, 55, 55);
                    _lblEstado.Text = "Codigo no reconocido";
                    SystemSounds.Hand.Play();
                    break;

                case ResultadoEscaneo.EstudianteNoEncontrado:
                    _panelAlerta.BackColor = Color.FromArgb(200, 55, 55);
                    _lblEstado.Text = "NIE no encontrado o inactivo";
                    SystemSounds.Hand.Play();
                    break;
            }

            if (resultado.Estudiante is not null)
            {
                _lblNombre.Text = resultado.Estudiante.NombreCompleto;
                _lblNie.Text = $"NIE: {resultado.Estudiante.NIE}";
                _lblGrado.Text = $"{resultado.Estudiante.Grado} - Seccion {resultado.Estudiante.Seccion}";
                _lblHora.Text = (resultado.HoraRegistro ?? DateTime.Now).ToString("hh:mm:ss tt");
            }
            else
            {
                _lblNombre.Text = string.Empty;
                _lblNie.Text = string.Empty;
                _lblGrado.Text = string.Empty;
                _lblHora.Text = string.Empty;
            }
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Kiosco de Escaneo";
            Width = 920;
            Height = 680;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            _pictureCamara = new PictureBox
            {
                Location = new Point(20, 20),
                Size = new Size(480, 360),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Black
            };

            _panelAlerta = new Panel
            {
                Location = new Point(520, 20),
                Size = new Size(360, 90),
                BackColor = Color.FromArgb(210, 210, 210)
            };

            _lblEstado = new Label
            {
                Text = "Esperando codigo...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };
            _panelAlerta.Controls.Add(_lblEstado);

            _lblNombre = CrearEtiquetaDato(520, 130, 18, negrita: true);
            _lblNie = CrearEtiquetaDato(520, 180, 13);
            _lblGrado = CrearEtiquetaDato(520, 210, 13);
            _lblHora = CrearEtiquetaDato(520, 240, 13);

            var lblManual = new Label
            {
                Text = "Respaldo manual - ingresar NIE:",
                Location = new Point(20, 400),
                AutoSize = true
            };

            _txtNieManual = new TextBox
            {
                Location = new Point(20, 425),
                Size = new Size(200, 28)
            };

            _btnRegistrarManual = new Button
            {
                Text = "Registrar",
                Location = new Point(230, 423),
                Size = new Size(100, 32),
                Font = new Font("Segoe UI", 9)
            };
            _btnRegistrarManual.Click += BtnRegistrarManual_Click;

            Controls.Add(_pictureCamara);
            Controls.Add(_panelAlerta);
            Controls.Add(_lblNombre);
            Controls.Add(_lblNie);
            Controls.Add(_lblGrado);
            Controls.Add(_lblHora);
            Controls.Add(lblManual);
            Controls.Add(_txtNieManual);
            Controls.Add(_btnRegistrarManual);
        }

        private static Label CrearEtiquetaDato(int x, int y, int tamanoFuente, bool negrita = false)
        {
            return new Label
            {
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", tamanoFuente, negrita ? FontStyle.Bold : FontStyle.Regular)
            };
        }
    }
}
