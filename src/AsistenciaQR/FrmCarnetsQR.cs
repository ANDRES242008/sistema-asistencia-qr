using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;
using AsistenciaQR.Servicios;

namespace AsistenciaQR
{
    /// <summary>
    /// Pantalla de Generacion de Carnets y Control de QR: lista los
    /// estudiantes activos, muestra si ya tienen un QR generado, y
    /// permite generar/regenerar su QR (esto inactiva el anterior
    /// automaticamente, util si se perdio el carnet) y generar su
    /// carnet en imagen y PDF. Diseno funcional (el estilo visual
    /// final llega en la Fase 4).
    /// </summary>
    public class FrmCarnetsQR : Form
    {
        private readonly EstudianteService _estudianteService = new();
        private readonly QRService _qrService = new();
        private readonly CarnetService _carnetService = new();
        private readonly QRRepository _qrRepository = new();

        private TextBox _txtBuscar = null!;
        private DataGridView _dgvEstudiantes = null!;

        private Label _lblNombreSeleccionado = null!;
        private Label _lblEstadoQr = null!;
        private PictureBox _picQr = null!;
        private Button _btnGenerarQr = null!;
        private Button _btnGenerarCarnet = null!;

        private Estudiante? _estudianteSeleccionado;

        public FrmCarnetsQR()
        {
            ConstruirInterfaz();
            Load += (_, _) => CargarLista();
        }

        private void CargarLista()
        {
            // Solo estudiantes activos: no tiene sentido generar
            // carnets para estudiantes dados de baja.
            var lista = _estudianteService.Buscar(_txtBuscar.Text, null, incluirInactivos: false);

            _dgvEstudiantes.DataSource = null;
            _dgvEstudiantes.DataSource = lista;

            OcultarColumna("EstudianteId");
            OcultarColumna("FotoRuta");
            OcultarColumna("FechaRegistro");
            OcultarColumna("Correo");
            OcultarColumna("Activo");

            if (_dgvEstudiantes.Columns["NombreCompleto"] is { } colNombre)
            {
                colNombre.HeaderText = "Nombre completo";
            }
        }

        private void OcultarColumna(string nombre)
        {
            if (_dgvEstudiantes.Columns[nombre] is { } columna)
            {
                columna.Visible = false;
            }
        }

        private void DgvEstudiantes_SelectionChanged(object? sender, EventArgs e)
        {
            if (_dgvEstudiantes.CurrentRow?.DataBoundItem is not Estudiante estudiante) return;

            _estudianteSeleccionado = estudiante;
            _lblNombreSeleccionado.Text = $"{estudiante.NombreCompleto}  ({estudiante.NIE})";

            ActualizarEstadoQr();
        }

        private void ActualizarEstadoQr()
        {
            if (_estudianteSeleccionado is null) return;

            var qrActivo = _qrRepository.ObtenerQrActivo(_estudianteSeleccionado.EstudianteId);

            if (qrActivo is null)
            {
                _lblEstadoQr.Text = "Este estudiante todavia no tiene un QR generado.";
                MostrarQr(null);
                _btnGenerarQr.Text = "Generar QR";
            }
            else
            {
                _lblEstadoQr.Text = $"QR activo, generado el {qrActivo.FechaGeneracion:dd/MM/yyyy HH:mm}.";
                MostrarQr(qrActivo.RutaImagen);
                _btnGenerarQr.Text = "Regenerar QR (el anterior queda inactivo)";
            }
        }

        private void MostrarQr(string? rutaImagen)
        {
            _picQr.Image?.Dispose();

            if (!string.IsNullOrEmpty(rutaImagen) && File.Exists(rutaImagen))
            {
                using var flujo = new MemoryStream(File.ReadAllBytes(rutaImagen));
                _picQr.Image = new Bitmap(flujo);
            }
            else
            {
                _picQr.Image = null;
            }
        }

        private void BtnGenerarQr_Click(object? sender, EventArgs e)
        {
            if (_estudianteSeleccionado is null)
            {
                MessageBox.Show(this, "Selecciona un estudiante de la lista.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _qrService.GenerarQrParaEstudiante(_estudianteSeleccionado);
            ActualizarEstadoQr();

            MessageBox.Show(this, "QR generado correctamente.", "Listo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnGenerarCarnet_Click(object? sender, EventArgs e)
        {
            if (_estudianteSeleccionado is null)
            {
                MessageBox.Show(this, "Selecciona un estudiante de la lista.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var qrActivo = _qrRepository.ObtenerQrActivo(_estudianteSeleccionado.EstudianteId);
            if (qrActivo is null)
            {
                MessageBox.Show(this, "Primero genera un QR para este estudiante.", "Falta el QR",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string rutaImagen = _carnetService.GenerarImagenCarnet(_estudianteSeleccionado, qrActivo);
            _carnetService.ExportarCarnetComoPdf(rutaImagen);

            // Abre la imagen de una vez, para verla al instante.
            Process.Start(new ProcessStartInfo(rutaImagen) { UseShellExecute = true });
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Carnets y Control de QR";
            Width = 950;
            Height = 650;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            var lblBuscar = new Label { Text = "Buscar:", Location = new Point(15, 18), AutoSize = true };
            _txtBuscar = new TextBox { Location = new Point(85, 15), Size = new Size(210, 26) };
            _txtBuscar.TextChanged += (_, _) => CargarLista();

            _dgvEstudiantes = new DataGridView
            {
                Location = new Point(15, 55),
                Size = new Size(560, 540),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _dgvEstudiantes.SelectionChanged += DgvEstudiantes_SelectionChanged;

            const int xPanel = 595;

            _lblNombreSeleccionado = new Label
            {
                Text = "Selecciona un estudiante de la lista",
                Location = new Point(xPanel, 55),
                Size = new Size(330, 30),
                Font = new Font("Segoe UI", 13, FontStyle.Bold)
            };

            _lblEstadoQr = new Label
            {
                Text = string.Empty,
                Location = new Point(xPanel, 95),
                Size = new Size(330, 40)
            };

            _picQr = new PictureBox
            {
                Location = new Point(xPanel, 145),
                Size = new Size(200, 200),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };

            _btnGenerarQr = new Button
            {
                Text = "Generar QR",
                Location = new Point(xPanel, 365),
                Size = new Size(330, 38),
                Font = new Font("Segoe UI", 9)
            };
            _btnGenerarQr.Click += BtnGenerarQr_Click;

            _btnGenerarCarnet = new Button
            {
                Text = "Generar carnet (imagen + PDF)",
                Location = new Point(xPanel, 415),
                Size = new Size(330, 38),
                Font = new Font("Segoe UI", 9)
            };
            _btnGenerarCarnet.Click += BtnGenerarCarnet_Click;

            Controls.Add(lblBuscar);
            Controls.Add(_txtBuscar);
            Controls.Add(_dgvEstudiantes);
            Controls.Add(_lblNombreSeleccionado);
            Controls.Add(_lblEstadoQr);
            Controls.Add(_picQr);
            Controls.Add(_btnGenerarQr);
            Controls.Add(_btnGenerarCarnet);
        }
    }
}
