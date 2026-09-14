using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
   
    public class FrmEstudiantes : Form
    {
        private const int FilasPorPagina = 8;

        private readonly EstudianteService _estudianteService = new();
        private float _escala = 1f;

        private List<Estudiante> _datosFiltrados = new();
        private int _paginaActual;

        private string? _rutaFotoSeleccionada;
        private string? _fotoRutaActual;
        private int? _estudianteIdSeleccionado;

        private Panel _panelContenido = null!;
        private BotonRegresar _btnRegresar = null!;

        private CampoTexto _campoBuscar = null!;
        private ComboBox _cmbSeccionFiltro = null!;
        private CheckBox _chkMostrarInactivos = null!;

        private DataGridView _dgvEstudiantes = null!;
        private Panel _panelPaginacion = null!;

        private TarjetaBlanca _tarjetaInfo = null!;
        private Label _lblTituloInfo = null!;

        private Label _lblNieEtiqueta = null!;
        private CampoSubrayado _campoNie = null!;
        private Label _lblNombreEtiqueta = null!;
        private CampoSubrayado _campoNombre = null!;
        private Label _lblGradoEtiqueta = null!;
        private CampoSubrayado _campoGrado = null!;
        private Label _lblSeccionEtiqueta = null!;
        private CampoSubrayado _campoSeccion = null!;
        private Label _lblCorreoEtiqueta = null!;
        private CampoSubrayado _campoCorreo = null!;

        private PictureBox _picFoto = null!;
        private Button _btnElegirFoto = null!;
        private Button _btnGuardar = null!;
        private Button _btnNuevo = null!;
        private Button _btnDarBaja = null!;
        private Button _btnReactivar = null!;

        public FrmEstudiantes()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarSecciones(); CargarLista(); AplicarEscala(); };
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

            _datosFiltrados = _estudianteService.Buscar(_campoBuscar.ObtenerValorReal(), seccion, _chkMostrarInactivos.Checked);
            _paginaActual = 0;
            MostrarPagina();
        }

        private void MostrarPagina()
        {
            var pagina = _datosFiltrados.Skip(_paginaActual * FilasPorPagina).Take(FilasPorPagina).ToList();

            _dgvEstudiantes.DataSource = null;
            _dgvEstudiantes.DataSource = pagina;
            ConfigurarColumnasGrid();
            SincronizarAlturaFilas();
            ActualizarPaginacion();
        }

        private void ConfigurarColumnasGrid()
        {
            OcultarColumna("EstudianteId");
            OcultarColumna("FotoRuta");
            OcultarColumna("FechaRegistro");
            OcultarColumna("Correo");

            if (_dgvEstudiantes.Columns["NombreCompleto"] is { } colNombre) colNombre.HeaderText = "Nombre";
            if (_dgvEstudiantes.Columns["Activo"] is { } colActivo) colActivo.HeaderText = "Estado";
        }

        private void OcultarColumna(string nombre)
        {
            if (_dgvEstudiantes.Columns[nombre] is { } columna) columna.Visible = false;
        }

        private void SincronizarAlturaFilas()
        {
            int alto = (int)(28 * _escala);
            foreach (DataGridViewRow fila in _dgvEstudiantes.Rows)
            {
                fila.Height = alto;
            }
        }

        private void DgvEstudiantes_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_dgvEstudiantes.Columns[e.ColumnIndex].Name != "Activo") return;

            e.PaintBackground(e.CellBounds, true);

            bool activo = e.Value is bool b && b;
            Color color = activo ? Colores.VerdeEsmeralda : Colores.GrisSecundario;
            string texto = activo ? "Activo" : "Inactivo";

            var g = e.Graphics!;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var fuente = e.CellStyle!.Font;
            var medida = TextRenderer.MeasureText(texto, fuente);
            int anchoPill = medida.Width + 24;
            int altoPill = Math.Max(Math.Min(e.CellBounds.Height - 6, 28), medida.Height + 6);
            int x = e.CellBounds.X + (e.CellBounds.Width - anchoPill) / 2;
            int y = e.CellBounds.Y + (e.CellBounds.Height - altoPill) / 2;

            var rectPill = new Rectangle(x, y, anchoPill, altoPill);
            using var rutaPill = Formas.Redondeada(rectPill, altoPill / 2);
            using var pincelPill = new SolidBrush(Color.FromArgb(35, color));
            g.FillPath(pincelPill, rutaPill);

            TextRenderer.DrawText(g, texto, fuente, rectPill, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }

        private void ActualizarPaginacion()
        {
            _panelPaginacion.Controls.Clear();

            int totalPaginas = Math.Max((int)Math.Ceiling(_datosFiltrados.Count / (double)FilasPorPagina), 1);
            int alturaBoton = (int)(30 * _escala);

            var controles = new List<Control>();

            var btnAnterior = CrearBotonSecundario("‹");
            btnAnterior.Size = new Size((int)(32 * _escala), alturaBoton);
            btnAnterior.Enabled = _paginaActual > 0;
            btnAnterior.Click += (_, _) => { _paginaActual--; MostrarPagina(); };
            controles.Add(btnAnterior);

            for (int i = 0; i < totalPaginas; i++)
            {
                int numeroPagina = i;
                bool esActual = i == _paginaActual;

                var btnPagina = new Button
                {
                    Text = (i + 1).ToString(),
                    Size = new Size((int)(34 * _escala), alturaBoton),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = esActual ? Colores.AzulMarino : Colores.BlancoPuro,
                    ForeColor = esActual ? Color.White : Colores.GrisOscuro,
                    Font = new Font("Segoe UI", 9 * _escala, esActual ? FontStyle.Bold : FontStyle.Regular)
                };
                btnPagina.FlatAppearance.BorderColor = Color.FromArgb(219, 224, 230);
                btnPagina.Click += (_, _) => { _paginaActual = numeroPagina; MostrarPagina(); };
                controles.Add(btnPagina);
            }

            var btnSiguiente = CrearBotonSecundario("›");
            btnSiguiente.Size = new Size((int)(32 * _escala), alturaBoton);
            btnSiguiente.Enabled = _paginaActual < totalPaginas - 1;
            btnSiguiente.Click += (_, _) => { _paginaActual++; MostrarPagina(); };
            controles.Add(btnSiguiente);

            int anchoTotal = controles.Sum(c => c.Width) + (controles.Count - 1) * (int)(6 * _escala);
            int x = Math.Max((_panelPaginacion.Width - anchoTotal) / 2, 0);
            foreach (var control in controles)
            {
                control.Location = new Point(x, 0);
                x += control.Width + (int)(6 * _escala);
                _panelPaginacion.Controls.Add(control);
            }
        }

        private void DgvEstudiantes_SelectionChanged(object? sender, EventArgs e)
        {
            if (_dgvEstudiantes.CurrentRow?.DataBoundItem is not Estudiante estudiante) return;

            _estudianteIdSeleccionado = estudiante.EstudianteId;
            _rutaFotoSeleccionada = null;
            _fotoRutaActual = estudiante.FotoRuta;

            _campoNie.Texto = estudiante.NIE;
            _campoNie.Habilitado = false;
            _campoNombre.Texto = estudiante.NombreCompleto;
            _campoGrado.Texto = estudiante.Grado;
            _campoSeccion.Texto = estudiante.Seccion;
            _campoCorreo.Texto = estudiante.Correo ?? string.Empty;

            MostrarFoto(estudiante.FotoRuta);

            _btnDarBaja.Visible = estudiante.Activo;
            _btnReactivar.Visible = !estudiante.Activo;
        }

        private void MostrarFoto(string? rutaFoto)
        {
            _picFoto.Image?.Dispose();

            if (!string.IsNullOrEmpty(rutaFoto) && File.Exists(rutaFoto))
            {
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

            _campoNie.Texto = string.Empty;
            _campoNie.Habilitado = true;
            _campoNombre.Texto = string.Empty;
            _campoGrado.Texto = string.Empty;
            _campoSeccion.Texto = string.Empty;
            _campoCorreo.Texto = string.Empty;

            _picFoto.Image?.Dispose();
            _picFoto.Image = null;

            _btnDarBaja.Visible = false;
            _btnReactivar.Visible = false;
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
                NIE = _campoNie.Texto.Trim(),
                NombreCompleto = _campoNombre.Texto.Trim(),
                Grado = _campoGrado.Texto.Trim(),
                Seccion = _campoSeccion.Texto.Trim(),
                Correo = string.IsNullOrWhiteSpace(_campoCorreo.Texto) ? null : _campoCorreo.Texto.Trim(),
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
                "Esto marca al estudiante como inactivo, pero conserva su historial. Continuar?",
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

        private void BtnRegresar_Click(object? sender, EventArgs e)
        {
            var dashboard = Application.OpenForms.OfType<FrmDashboard>().FirstOrDefault();
            if (dashboard is not null)
            {
                dashboard.Show();
                dashboard.BringToFront();
            }
            else
            {
                new FrmDashboard().Show();
            }
            Close();
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Gestion de Estudiantes";
            Width = 1150;
            Height = 760;
            MinimumSize = new Size(950, 640);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Colores.GrisClaro;
            AutoScaleMode = AutoScaleMode.Dpi;

            ConstruirPanelContenido();
            Resize += (_, _) => AplicarEscala();
        }

        private void ConstruirPanelContenido()
        {
            _panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Colores.GrisClaro,
                Padding = new Padding(24)
            };

            _btnRegresar = new BotonRegresar();
            _btnRegresar.Click += BtnRegresar_Click;

            _campoBuscar = new CampoTexto("🔍", "Buscar NIE/Nombre");
            _campoBuscar.TextoCambiado += _ => CargarLista();

            _cmbSeccionFiltro = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            _cmbSeccionFiltro.SelectedIndexChanged += (_, _) => CargarLista();

            _chkMostrarInactivos = new CheckBox
            {
                Text = "Mostrar inactivos",
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };
            _chkMostrarInactivos.CheckedChanged += (_, _) => CargarLista();

            _dgvEstudiantes = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Colores.BlancoPuro,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            _dgvEstudiantes.CellPainting += DgvEstudiantes_CellPainting;
            _dgvEstudiantes.SelectionChanged += DgvEstudiantes_SelectionChanged;

            _panelPaginacion = new Panel { BackColor = Colores.GrisClaro };

            ConstruirTarjetaInfo();

            _panelContenido.Controls.AddRange(new Control[]
            {
                _btnRegresar,
                _campoBuscar, _cmbSeccionFiltro, _chkMostrarInactivos,
                _dgvEstudiantes, _panelPaginacion, _tarjetaInfo
            });

            Controls.Add(_panelContenido);
        }

        private void ConstruirTarjetaInfo()
        {
            _tarjetaInfo = new TarjetaBlanca();

            _lblTituloInfo = new Label
            {
                Text = "Informacion del Estudiante",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true 
            };

            _lblNieEtiqueta = CrearEtiqueta("NIE:");
            _campoNie = new CampoSubrayado();

            _lblNombreEtiqueta = CrearEtiqueta("Nombre completo:");
            _campoNombre = new CampoSubrayado();

            _lblGradoEtiqueta = CrearEtiqueta("Grado:");
            _campoGrado = new CampoSubrayado();

            _lblSeccionEtiqueta = CrearEtiqueta("Seccion:");
            _campoSeccion = new CampoSubrayado();

            _lblCorreoEtiqueta = CrearEtiqueta("Correo (opcional):");
            _campoCorreo = new CampoSubrayado();

            _picFoto = new PictureBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Colores.GrisClaro
            };

            _btnElegirFoto = CrearBotonSecundario("Elegir foto...");
            _btnElegirFoto.Click += BtnElegirFoto_Click;

            _btnGuardar = new BotonPrimario { Text = "Guardar" };
            _btnGuardar.Click += BtnGuardar_Click;

            _btnNuevo = CrearBotonSecundario("Nuevo");
            _btnNuevo.Click += BtnNuevo_Click;

            _btnDarBaja = CrearBotonPeligro("Dar de baja");
            _btnDarBaja.Click += BtnDarBaja_Click;

            _btnReactivar = CrearBotonExito("Reactivar");
            _btnReactivar.Click += BtnReactivar_Click;

            _tarjetaInfo.Controls.AddRange(new Control[]
            {
                _lblTituloInfo,
                _lblNieEtiqueta, _campoNie,
                _lblNombreEtiqueta, _campoNombre,
                _lblGradoEtiqueta, _campoGrado,
                _lblSeccionEtiqueta, _campoSeccion,
                _lblCorreoEtiqueta, _campoCorreo,
                _picFoto, _btnElegirFoto,
                _btnGuardar, _btnNuevo, _btnDarBaja, _btnReactivar
            });
        }

        private static Label CrearEtiqueta(string texto) => new()
        {
            Text = texto,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Colores.GrisOscuro,
            AutoSize = true
        };

        private static Button CrearBotonSecundario(string texto)
        {
            var boton = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                BackColor = Colores.BlancoPuro,
                ForeColor = Colores.GrisOscuro,
                Font = new Font("Segoe UI", 9.5f)
            };
            boton.FlatAppearance.BorderColor = Color.FromArgb(219, 224, 230);
            return boton;
        }

        private static Button CrearBotonPeligro(string texto)
        {
            var boton = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 240, 240),
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            boton.FlatAppearance.BorderColor = Color.FromArgb(240, 200, 200);
            return boton;
        }

        private static Button CrearBotonExito(string texto)
        {
            var boton = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 250, 240),
                ForeColor = Colores.VerdeEsmeralda,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            boton.FlatAppearance.BorderColor = Color.FromArgb(180, 230, 200);
            return boton;
        }

        
        private void AplicarEscala()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            float escalaAncho = ClientSize.Width / 1150f;
            float escalaAlto = ClientSize.Height / 720f;
            _escala = Math.Clamp(Math.Min(escalaAncho, escalaAlto), 1f, 1.6f);

            int anchoDisponible = _panelContenido.ClientSize.Width - _panelContenido.Padding.Horizontal;
            int altoDisponible = _panelContenido.ClientSize.Height - _panelContenido.Padding.Vertical;
            if (anchoDisponible <= 0 || altoDisponible <= 0) return;

            int x0 = _panelContenido.Padding.Left;
            int y0 = _panelContenido.Padding.Top;

            
            _campoBuscar.Width = (int)(240 * _escala);
            _campoBuscar.Location = new Point(x0, y0);
            _campoBuscar.EscalarA(_escala);

            _cmbSeccionFiltro.Font = new Font("Segoe UI", 9.5f * _escala);
            _cmbSeccionFiltro.Width = (int)(150 * _escala);
            _cmbSeccionFiltro.Location = new Point(_campoBuscar.Right + (int)(14 * _escala), y0 + (int)(8 * _escala));

            _chkMostrarInactivos.Font = new Font("Segoe UI", 9.5f * _escala);
            _chkMostrarInactivos.Location = new Point(_cmbSeccionFiltro.Right + (int)(16 * _escala), y0 + (int)(12 * _escala));

            int y1 = _campoBuscar.Bottom + (int)(20 * _escala);

            
            int espacioColumnas = (int)(20 * _escala);
            int anchoLista = (int)(anchoDisponible * 0.55);
            int anchoInfo = anchoDisponible - anchoLista - espacioColumnas;

            int alturaPaginacion = (int)(38 * _escala);
            int alturaRegresar = (int)(42 * _escala);
            int gapExtra = (int)(14 * _escala);

            int alturaGrid = altoDisponible - (y1 - y0) - alturaRegresar - gapExtra;

            _dgvEstudiantes.Font = new Font("Segoe UI", 9 * _escala);
            _dgvEstudiantes.ColumnHeadersHeight = (int)(32 * _escala);
            _dgvEstudiantes.RowTemplate.Height = (int)(28 * _escala);
            _dgvEstudiantes.Location = new Point(x0, y1);
            _dgvEstudiantes.Size = new Size(anchoLista, Math.Max(alturaGrid, 100));
            SincronizarAlturaFilas();


            int yFilaInferior = _dgvEstudiantes.Bottom + gapExtra;

            _btnRegresar.Size = new Size((int)(160 * _escala), alturaRegresar);
            _btnRegresar.Location = new Point(x0, yFilaInferior);

           
            int yPaginacion = yFilaInferior + (alturaRegresar - alturaPaginacion) / 2;
            _panelPaginacion.Location = new Point(x0, yPaginacion);
            _panelPaginacion.Size = new Size(anchoLista, alturaPaginacion);
            ActualizarPaginacion();

            _tarjetaInfo.Location = new Point(x0 + anchoLista + espacioColumnas, y1);
            _tarjetaInfo.Size = new Size(anchoInfo, altoDisponible - (y1 - y0));

            int margenInfo = (int)(22 * _escala);
            int yInfo = margenInfo;
            int anchoCampo = Math.Max(anchoInfo - margenInfo * 2, 100);

            _lblTituloInfo.Font = new Font("Segoe UI", 13 * _escala, FontStyle.Bold);
            _lblTituloInfo.Location = new Point(margenInfo, yInfo);
            yInfo = _lblTituloInfo.Bottom + (int)(14 * _escala);

            void ColocarCampo(Label etiqueta, CampoSubrayado campo, int ancho)
            {
                etiqueta.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
                etiqueta.Location = new Point(margenInfo, yInfo);
                yInfo = etiqueta.Bottom + (int)(4 * _escala);

                campo.Width = ancho;
                campo.Location = new Point(margenInfo, yInfo);
                campo.EscalarA(_escala);
                yInfo = campo.Bottom + (int)(12 * _escala);
            }

            ColocarCampo(_lblNieEtiqueta, _campoNie, anchoCampo);
            ColocarCampo(_lblNombreEtiqueta, _campoNombre, anchoCampo);

            int anchoMitad = (anchoCampo - (int)(16 * _escala)) / 2;
            _lblGradoEtiqueta.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _lblGradoEtiqueta.Location = new Point(margenInfo, yInfo);
            _lblSeccionEtiqueta.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _lblSeccionEtiqueta.Location = new Point(margenInfo + anchoMitad + (int)(16 * _escala), yInfo);
            yInfo = Math.Max(_lblGradoEtiqueta.Bottom, _lblSeccionEtiqueta.Bottom) + (int)(4 * _escala);

            _campoGrado.Width = anchoMitad;
            _campoGrado.Location = new Point(margenInfo, yInfo);
            _campoGrado.EscalarA(_escala);
            _campoSeccion.Width = anchoMitad;
            _campoSeccion.Location = new Point(margenInfo + anchoMitad + (int)(16 * _escala), yInfo);
            _campoSeccion.EscalarA(_escala);
            yInfo = Math.Max(_campoGrado.Bottom, _campoSeccion.Bottom) + (int)(12 * _escala);

            ColocarCampo(_lblCorreoEtiqueta, _campoCorreo, anchoCampo);

            int ladoFoto = (int)(90 * _escala);
            _picFoto.Size = new Size(ladoFoto, ladoFoto);
            _picFoto.Location = new Point(margenInfo, yInfo);

            _btnElegirFoto.Font = new Font("Segoe UI", 9 * _escala);
            _btnElegirFoto.Size = new Size(Math.Max(anchoCampo - ladoFoto - (int)(14 * _escala), 60), (int)(32 * _escala));
            _btnElegirFoto.Location = new Point(margenInfo + ladoFoto + (int)(14 * _escala), yInfo + (ladoFoto - _btnElegirFoto.Height) / 2);

            yInfo += ladoFoto + (int)(16 * _escala);

            int anchoBotonDoble = (anchoCampo - (int)(16 * _escala)) / 2;
            _btnGuardar.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _btnGuardar.Size = new Size(anchoBotonDoble, (int)(36 * _escala));
            _btnGuardar.Location = new Point(margenInfo, yInfo);

            _btnNuevo.Font = new Font("Segoe UI", 9.5f * _escala);
            _btnNuevo.Size = new Size(anchoBotonDoble, (int)(36 * _escala));
            _btnNuevo.Location = new Point(margenInfo + anchoBotonDoble + (int)(16 * _escala), yInfo);

            yInfo = _btnGuardar.Bottom + (int)(10 * _escala);

            _btnDarBaja.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _btnDarBaja.Size = new Size(anchoCampo, (int)(36 * _escala));
            _btnDarBaja.Location = new Point(margenInfo, yInfo);

            _btnReactivar.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _btnReactivar.Size = new Size(anchoCampo, (int)(36 * _escala));
            _btnReactivar.Location = new Point(margenInfo, yInfo);
        }
    }
}
