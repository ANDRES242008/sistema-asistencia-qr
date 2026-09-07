using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR
{
    /// <summary>
    /// Pantalla de gestion de estudiantes: lista con busqueda y
    /// filtro por seccion a la izquierda, formulario de alta/edicion
    /// con foto a la derecha. Diseno funcional (el estilo visual
    /// final llega en la Fase 4).
    /// </summary>
    public class FrmEstudiantes : Form
    {
        private readonly EstudianteService _estudianteService = new();

        private TextBox _txtBuscar = null!;
        private ComboBox _cmbSeccionFiltro = null!;
        private CheckBox _chkMostrarInactivos = null!;
        private DataGridView _dgvEstudiantes = null!;

        private TextBox _txtNie = null!;
        private TextBox _txtNombre = null!;
        private TextBox _txtGrado = null!;
        private TextBox _txtSeccion = null!;
        private TextBox _txtCorreo = null!;
        private PictureBox _picFoto = null!;
        private Button _btnElegirFoto = null!;
        private Button _btnNuevo = null!;
        private Button _btnGuardar = null!;
        private Button _btnDarBaja = null!;
        private Button _btnReactivar = null!;

        private string? _rutaFotoSeleccionada;
        private string? _fotoRutaActual;
        private int? _estudianteIdSeleccionado;

        public FrmEstudiantes()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarSecciones(); CargarLista(); };
        }

        private void CargarSecciones()
        {
            _cmbSeccionFiltro.Items.Clear();
            _cmbSeccionFiltro.Items.Add("Todas");
            foreach (var seccion in _estudianteService.ObtenerSecciones())
            {
                _cmbSeccionFiltro.Items.Add(seccion);
            }
            _cmbSeccionFiltro.SelectedIndex = 0;
        }

        private void CargarLista()
        {
            string? seccion = _cmbSeccionFiltro.SelectedIndex > 0
                ? _cmbSeccionFiltro.SelectedItem?.ToString()
                : null;

            var lista = _estudianteService.Buscar(_txtBuscar.Text, seccion, _chkMostrarInactivos.Checked);

            _dgvEstudiantes.DataSource = null;
            _dgvEstudiantes.DataSource = lista;

            ConfigurarColumnas();
        }

        private void ConfigurarColumnas()
        {
            OcultarColumna("EstudianteId");
            OcultarColumna("FotoRuta");
            OcultarColumna("FechaRegistro");
            OcultarColumna("Correo");

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

            _estudianteIdSeleccionado = estudiante.EstudianteId;
            _rutaFotoSeleccionada = null;
            _fotoRutaActual = estudiante.FotoRuta;

            _txtNie.Text = estudiante.NIE;
            _txtNie.Enabled = false;
            _txtNombre.Text = estudiante.NombreCompleto;
            _txtGrado.Text = estudiante.Grado;
            _txtSeccion.Text = estudiante.Seccion;
            _txtCorreo.Text = estudiante.Correo ?? string.Empty;

            MostrarFoto(estudiante.FotoRuta);

            _btnDarBaja.Visible = estudiante.Activo;
            _btnReactivar.Visible = !estudiante.Activo;
        }

        private void MostrarFoto(string? rutaFoto)
        {
            _picFoto.Image?.Dispose();

            if (!string.IsNullOrEmpty(rutaFoto) && File.Exists(rutaFoto))
            {
                // OJO: Image.FromFile() deja el archivo bloqueado
                // mientras la imagen este en pantalla. Leyendo los
                // bytes primero y creando el Bitmap desde memoria,
                // el archivo queda libre de inmediato - asi se puede
                // reemplazar la foto de este mismo estudiante despues
                // sin que de error de "archivo en uso".
                using var flujo = new MemoryStream(File.ReadAllBytes(rutaFoto));
                _picFoto.Image = new Bitmap(flujo);
            }
            else
            {
                _picFoto.Image = null;
            }
        }

        private void BtnNuevo_Click(object? sender, EventArgs e) => LimpiarFormulario();

        private void LimpiarFormulario()
        {
            _estudianteIdSeleccionado = null;
            _rutaFotoSeleccionada = null;
            _fotoRutaActual = null;

            _txtNie.Clear();
            _txtNie.Enabled = true;
            _txtNombre.Clear();
            _txtGrado.Clear();
            _txtSeccion.Clear();
            _txtCorreo.Clear();

            _picFoto.Image?.Dispose();
            _picFoto.Image = null;

            _btnDarBaja.Visible = false;
            _btnReactivar.Visible = false;

            _txtNie.Focus();
        }

        private void BtnElegirFoto_Click(object? sender, EventArgs e)
        {
            using var dialogo = new OpenFileDialog
            {
                Filter = "Imagenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
            };

            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            _rutaFotoSeleccionada = dialogo.FileName;
            MostrarFoto(_rutaFotoSeleccionada);
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            var estudiante = new Estudiante
            {
                EstudianteId = _estudianteIdSeleccionado ?? 0,
                NIE = _txtNie.Text.Trim(),
                NombreCompleto = _txtNombre.Text.Trim(),
                Grado = _txtGrado.Text.Trim(),
                Seccion = _txtSeccion.Text.Trim(),
                Correo = string.IsNullOrWhiteSpace(_txtCorreo.Text) ? null : _txtCorreo.Text.Trim(),

                // Si no se elige una foto nueva, se conserva la que ya
                // tenia (antes esto se quedaba en null y borraba la
                // foto guardada cada vez que editabas sin tocarla).
                FotoRuta = _fotoRutaActual
            };

            var resultado = _estudianteIdSeleccionado is null
                ? _estudianteService.Registrar(estudiante, _rutaFotoSeleccionada)
                : _estudianteService.Editar(estudiante, _rutaFotoSeleccionada);

            MessageBox.Show(this, resultado.Mensaje, resultado.Exitoso ? "Listo" : "Revisa los datos",
                MessageBoxButtons.OK, resultado.Exitoso ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            if (!resultado.Exitoso) return;

            LimpiarFormulario();
            CargarLista();
        }

        private void BtnDarBaja_Click(object? sender, EventArgs e)
        {
            if (_estudianteIdSeleccionado is not int id) return;

            var confirmar = MessageBox.Show(this,
                "Esto marca al estudiante como inactivo, pero conserva su historial de asistencia. Continuar?",
                "Confirmar baja", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmar != DialogResult.Yes) return;

            _estudianteService.DarDeBaja(id);
            LimpiarFormulario();
            CargarLista();
        }

        private void BtnReactivar_Click(object? sender, EventArgs e)
        {
            if (_estudianteIdSeleccionado is not int id) return;

            _estudianteService.Reactivar(id);
            LimpiarFormulario();
            CargarLista();
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Gestion de Estudiantes";
            Width = 1080;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            var lblBuscar = new Label { Text = "Buscar:", Location = new Point(15, 18), AutoSize = true };
            _txtBuscar = new TextBox { Location = new Point(75, 15), Size = new Size(180, 26) };
            _txtBuscar.TextChanged += (_, _) => CargarLista();

            var lblSeccion = new Label { Text = "Seccion:", Location = new Point(275, 18), AutoSize = true };
            _cmbSeccionFiltro = new ComboBox
            {
                Location = new Point(340, 15),
                Size = new Size(110, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbSeccionFiltro.SelectedIndexChanged += (_, _) => CargarLista();

            _chkMostrarInactivos = new CheckBox
            {
                Text = "Mostrar inactivos",
                Location = new Point(465, 18),
                AutoSize = true
            };
            _chkMostrarInactivos.CheckedChanged += (_, _) => CargarLista();

            _dgvEstudiantes = new DataGridView
            {
                Location = new Point(15, 55),
                Size = new Size(600, 590),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _dgvEstudiantes.SelectionChanged += DgvEstudiantes_SelectionChanged;

            const int xPanel = 635;

            var lblNie = new Label { Text = "NIE:", Location = new Point(xPanel, 55), AutoSize = true };
            _txtNie = new TextBox { Location = new Point(xPanel, 75), Size = new Size(180, 26) };

            var lblNombre = new Label { Text = "Nombre completo:", Location = new Point(xPanel, 115), AutoSize = true };
            _txtNombre = new TextBox { Location = new Point(xPanel, 135), Size = new Size(360, 26) };

            var lblGrado = new Label { Text = "Grado:", Location = new Point(xPanel, 175), AutoSize = true };
            _txtGrado = new TextBox { Location = new Point(xPanel, 195), Size = new Size(160, 26) };

            var lblSeccionForm = new Label { Text = "Seccion:", Location = new Point(xPanel + 180, 175), AutoSize = true };
            _txtSeccion = new TextBox { Location = new Point(xPanel + 180, 195), Size = new Size(100, 26) };

            var lblCorreo = new Label { Text = "Correo (opcional):", Location = new Point(xPanel, 235), AutoSize = true };
            _txtCorreo = new TextBox { Location = new Point(xPanel, 255), Size = new Size(360, 26) };

            _picFoto = new PictureBox
            {
                Location = new Point(xPanel, 300),
                Size = new Size(140, 140),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };

            _btnElegirFoto = new Button
            {
                Text = "Elegir foto...",
                Location = new Point(xPanel + 160, 350),
                Size = new Size(140, 34)
            };
            _btnElegirFoto.Click += BtnElegirFoto_Click;

            _btnNuevo = new Button
            {
                Text = "Nuevo",
                Location = new Point(xPanel, 460),
                Size = new Size(110, 36)
            };
            _btnNuevo.Click += BtnNuevo_Click;

            _btnGuardar = new Button
            {
                Text = "Guardar",
                Location = new Point(xPanel + 120, 460),
                Size = new Size(110, 36)
            };
            _btnGuardar.Click += BtnGuardar_Click;

            _btnDarBaja = new Button
            {
                Text = "Dar de baja",
                Location = new Point(xPanel, 510),
                Size = new Size(160, 36),
                Visible = false
            };
            _btnDarBaja.Click += BtnDarBaja_Click;

            _btnReactivar = new Button
            {
                Text = "Reactivar",
                Location = new Point(xPanel, 510),
                Size = new Size(160, 36),
                Visible = false
            };
            _btnReactivar.Click += BtnReactivar_Click;

            Controls.Add(lblBuscar);
            Controls.Add(_txtBuscar);
            Controls.Add(lblSeccion);
            Controls.Add(_cmbSeccionFiltro);
            Controls.Add(_chkMostrarInactivos);
            Controls.Add(_dgvEstudiantes);
            Controls.Add(lblNie);
            Controls.Add(_txtNie);
            Controls.Add(lblNombre);
            Controls.Add(_txtNombre);
            Controls.Add(lblGrado);
            Controls.Add(_txtGrado);
            Controls.Add(lblSeccionForm);
            Controls.Add(_txtSeccion);
            Controls.Add(lblCorreo);
            Controls.Add(_txtCorreo);
            Controls.Add(_picFoto);
            Controls.Add(_btnElegirFoto);
            Controls.Add(_btnNuevo);
            Controls.Add(_btnGuardar);
            Controls.Add(_btnDarBaja);
            Controls.Add(_btnReactivar);
        }
    }
}
