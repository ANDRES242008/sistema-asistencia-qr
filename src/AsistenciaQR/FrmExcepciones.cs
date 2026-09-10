using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR
{
    public class FrmExcepciones : Form
    {
        private readonly EstudianteService _estudianteService = new();
        private readonly AsistenciaService _asistenciaService = new();

        private TextBox _txtBuscar = null!;
        private DataGridView _dgvEstudiantes = null!;

        private Label _lblSeleccionado = null!;
        private ComboBox _cmbObservacion = null!;
        private Button _btnRegistrar = null!;
        private Label _lblResultado = null!;

        private Estudiante? _estudianteSeleccionado;

        public FrmExcepciones()
        {
            ConstruirInterfaz();
            Load += (_, _) => CargarLista();
        }

        private void CargarLista()
        {
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
            _lblSeleccionado.Text =
                $"{estudiante.NombreCompleto}  ({estudiante.NIE})\n{estudiante.Grado} - Seccion {estudiante.Seccion}";
            _lblResultado.Text = string.Empty;
        }

        private void BtnRegistrar_Click(object? sender, EventArgs e)
        {
            if (_estudianteSeleccionado is null)
            {
                MessageBox.Show(this, "Selecciona un estudiante de la lista.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string observacion = string.IsNullOrWhiteSpace(_cmbObservacion.Text)
                ? "Registro manual"
                : _cmbObservacion.Text.Trim();

            var resultado = _asistenciaService.RegistrarPorNie(_estudianteSeleccionado.NIE, observacion);

            switch (resultado.Estado)
            {
                case ResultadoEscaneo.Exitoso:
                    _lblResultado.ForeColor = resultado.EsTarde
                        ? Color.FromArgb(230, 159, 0)
                        : Color.FromArgb(46, 160, 67);
                    _lblResultado.Text = resultado.EsTarde
                        ? $"Registrado como TARDANZA a las {resultado.HoraRegistro:hh:mm tt}."
                        : $"Asistencia registrada a tiempo, {resultado.HoraRegistro:hh:mm tt}.";
                    _cmbObservacion.Text = string.Empty;
                    break;

                case ResultadoEscaneo.YaRegistradoHoy:
                    _lblResultado.ForeColor = Color.FromArgb(70, 110, 180);
                    _lblResultado.Text = "Este estudiante ya tiene asistencia registrada hoy.";
                    break;

                case ResultadoEscaneo.CodigoNoValido:
                    _lblResultado.ForeColor = Color.FromArgb(200, 55, 55);
                    _lblResultado.Text = "No se pudo registrar (estudiante no valido).";
                    break;
            }
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Registro Manual de Excepciones";
            Width = 950;
            Height = 650;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            var lblBuscar = new Label { Text = "Buscar (NIE o nombre):", Location = new Point(15, 18), AutoSize = true };
            _txtBuscar = new TextBox { Location = new Point(165, 15), Size = new Size(220, 26) };
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

            var lblTitulo = new Label
            {
                Text = "Registro manual (sin carnet / QR danado)",
                Location = new Point(xPanel, 55),
                Size = new Size(330, 45),
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            _lblSeleccionado = new Label
            {
                Text = "Selecciona un estudiante de la lista",
                Location = new Point(xPanel, 105),
                Size = new Size(330, 50)
            };

            var lblObservacion = new Label
            {
                Text = "Motivo:",
                Location = new Point(xPanel, 170),
                AutoSize = true
            };

            _cmbObservacion = new ComboBox
            {
                Location = new Point(xPanel, 190),
                Size = new Size(330, 28),
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _cmbObservacion.Items.AddRange(new object[]
            {
                "Olvido el carnet",
                "Codigo QR danado",
                "Camara fuera de servicio"
            });

            _btnRegistrar = new Button
            {
                Text = "Registrar asistencia",
                Location = new Point(xPanel, 240),
                Size = new Size(330, 40),
                Font = new Font("Segoe UI", 10)
            };
            _btnRegistrar.Click += BtnRegistrar_Click;

            _lblResultado = new Label
            {
                Text = string.Empty,
                Location = new Point(xPanel, 295),
                Size = new Size(330, 60),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            Controls.Add(lblBuscar);
            Controls.Add(_txtBuscar);
            Controls.Add(_dgvEstudiantes);
            Controls.Add(lblTitulo);
            Controls.Add(_lblSeleccionado);
            Controls.Add(lblObservacion);
            Controls.Add(_cmbObservacion);
            Controls.Add(_btnRegistrar);
            Controls.Add(_lblResultado);
        }
    }
}
