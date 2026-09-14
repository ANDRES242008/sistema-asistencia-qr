using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{

    public class FrmExcepciones : Form
    {
        private readonly EstudianteService _estudianteService = new();
        private readonly AsistenciaService _asistenciaService = new();

        private float _escala = 1f;
        private Estudiante? _estudianteSeleccionado;
        private readonly System.Windows.Forms.Timer _timerReloj = new() { Interval = 1000 };

        private Panel _panelContenido = null!;
        private BotonRegresar _btnRegresar = null!;

        private TarjetaBlanca _tarjetaBusqueda = null!;
        private Label _lblTituloBusqueda = null!;
        private CampoTexto _campoBuscar = null!;
        private DataGridView _dgvEstudiantes = null!;

        private TarjetaBlanca _tarjetaRegistro = null!;
        private Label _lblTituloRegistro = null!;
        private Label _lblSinSeleccion = null!;
        private PictureBox _picAvatar = null!;
        private Label _lblNombreSeleccionado = null!;
        private Label _lblNieSeleccionado = null!;
        private Label _lblMotivoEtiqueta = null!;
        private ComboBox _cmbMotivo = null!;
        private Label _lblNotasEtiqueta = null!;
        private CampoSubrayado _campoNotas = null!;
        private Label _lblFechaHoraEtiqueta = null!;
        private Label _lblFechaHoraValor = null!;
        private BotonPrimario _btnRegistrar = null!;
        private Label _lblResultado = null!;

        public FrmExcepciones()
        {
            ConstruirInterfaz();

            _timerReloj.Tick += (_, _) => ActualizarReloj();

            Load += (_, _) =>
            {
                CargarLista();
                ActualizarReloj();
                _timerReloj.Start();
                AplicarEscala();
            };
            FormClosing += (_, _) => _timerReloj.Stop();
        }

        private void ActualizarReloj()
        {
            _lblFechaHoraValor.Text = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");
        }

        private void CargarLista()
        {
            var lista = _estudianteService.Buscar(_campoBuscar.ObtenerValorReal(), null, incluirInactivos: false);

            _dgvEstudiantes.DataSource = null;
            _dgvEstudiantes.DataSource = lista;
            ConfigurarColumnasGrid();
            SincronizarAlturaFilas();
        }

        private void ConfigurarColumnasGrid()
        {
            OcultarColumna("EstudianteId");
            OcultarColumna("FotoRuta");
            OcultarColumna("FechaRegistro");
            OcultarColumna("Correo");
            OcultarColumna("Activo");

            if (_dgvEstudiantes.Columns["NombreCompleto"] is { } colNombre) colNombre.HeaderText = "Nombre Completo";
        }

        private void OcultarColumna(string nombre)
        {
            if (_dgvEstudiantes.Columns[nombre] is { } columna) columna.Visible = false;
        }

        private void SincronizarAlturaFilas()
        {
            int alto = (int)(30 * _escala);
            foreach (DataGridViewRow fila in _dgvEstudiantes.Rows)
            {
                fila.Height = alto;
            }
        }

        private void DgvEstudiantes_SelectionChanged(object? sender, EventArgs e)
        {
            if (_dgvEstudiantes.CurrentRow?.DataBoundItem is not Estudiante estudiante)
            {
                _estudianteSeleccionado = null;
                MostrarSinSeleccion();
                return;
            }

            _estudianteSeleccionado = estudiante;
            MostrarSeleccion(estudiante);
        }

        private void MostrarSinSeleccion()
        {
            _lblSinSeleccion.Visible = true;
            _picAvatar.Visible = false;
            _lblNombreSeleccionado.Visible = false;
            _lblNieSeleccionado.Visible = false;
        }

        private void MostrarSeleccion(Estudiante estudiante)
        {
            _lblSinSeleccion.Visible = false;
            _picAvatar.Visible = true;
            _lblNombreSeleccionado.Visible = true;
            _lblNieSeleccionado.Visible = true;

            _picAvatar.Image?.Dispose();
            _picAvatar.Image = GenerarAvatarInicial(estudiante.NombreCompleto, _picAvatar.Width);

            _lblNombreSeleccionado.Text = estudiante.NombreCompleto;
            _lblNieSeleccionado.Text = $"NIE {estudiante.NIE}  ·  {estudiante.Grado} - Seccion {estudiante.Seccion}";

            _lblResultado.Text = string.Empty;
        }

        /// <summary>Circulo de color con la inicial del nombre (sin usar la foto real, mas limpio).</summary>
        private static Bitmap GenerarAvatarInicial(string nombreCompleto, int lado)
        {
            var avatar = new Bitmap(lado, lado);

            using var g = Graphics.FromImage(avatar);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var area = new Rectangle(0, 0, lado, lado);

            using var pincel = new SolidBrush(Colores.AzulMarino);
            g.FillEllipse(pincel, area);

            string inicial = string.IsNullOrWhiteSpace(nombreCompleto)
                ? "?"
                : nombreCompleto.Trim()[0].ToString().ToUpperInvariant();

            using var fuente = new Font("Segoe UI", Math.Max(lado * 0.4f, 8f), FontStyle.Bold);
            TextRenderer.DrawText(g, inicial, fuente, area, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            return avatar;
        }

        private void BtnRegistrar_Click(object? sender, EventArgs e)
        {
            // 1. Validar selección de estudiante
            if (_estudianteSeleccionado is null)
            {
                MessageBox.Show(this, "Selecciona un estudiante de la lista.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. Validar que seleccionó un motivo obligatoriamente
            if (_cmbMotivo.SelectedIndex == -1 || string.IsNullOrWhiteSpace(_cmbMotivo.Text))
            {
                MessageBox.Show(this, "Debes seleccionar un tipo de excepción (motivo) antes de registrar.",
                                "Validación Requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cmbMotivo.Focus();
                return;
            }

            string motivo = _cmbMotivo.Text.Trim();
            string notas = _campoNotas.Texto.Trim();
            string observacion = string.IsNullOrWhiteSpace(notas) ? motivo : $"{motivo} - {notas}";

            var resultado = _asistenciaService.RegistrarPorNie(_estudianteSeleccionado.NIE, observacion);

            switch (resultado.Estado)
            {
                case ResultadoEscaneo.Exitoso:
                    _lblResultado.ForeColor = resultado.EsTarde
                        ? Color.FromArgb(230, 159, 0)
                        : Colores.VerdeEsmeralda;
                    _lblResultado.Text = resultado.EsTarde
                        ? $"Registrado como TARDANZA a las {resultado.HoraRegistro:hh:mm tt}."
                        : $"Asistencia registrada a tiempo, {resultado.HoraRegistro:hh:mm tt}.";

            
                    _cmbMotivo.SelectedIndex = -1;
                    _campoNotas.Limpiar();
                    _dgvEstudiantes.ClearSelection();
                    break;

                case ResultadoEscaneo.YaRegistradoHoy:
                    _lblResultado.ForeColor = Color.FromArgb(70, 110, 180);
                    _lblResultado.Text = "Este estudiante ya tiene asistencia registrada hoy.";
                    break;

                case ResultadoEscaneo.CodigoNoValido:
                    _lblResultado.ForeColor = Color.Firebrick;
                    _lblResultado.Text = "No se pudo registrar (estudiante no valido).";
                    break;
            }

            AplicarEscala();
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
            Text = "AsistenciaQR - Centro de Excepciones";
            Width = 1150;
            Height = 760;
            MinimumSize = new Size(980, 640);
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

            ConstruirTarjetaBusqueda();
            ConstruirTarjetaRegistro();

            _btnRegresar = new BotonRegresar();
            _btnRegresar.Click += BtnRegresar_Click;

            _panelContenido.Controls.AddRange(new Control[] { _tarjetaBusqueda, _tarjetaRegistro, _btnRegresar });
            Controls.Add(_panelContenido);
        }

        private void ConstruirTarjetaBusqueda()
        {
            _tarjetaBusqueda = new TarjetaBlanca();

            _lblTituloBusqueda = new Label
            {
                Text = "Busqueda de Estudiantes",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            _campoBuscar = new CampoTexto("🔍", "Ingresar NIE o Nombre para buscar...");
            _campoBuscar.TextoCambiado += _ => CargarLista();

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
            _dgvEstudiantes.SelectionChanged += DgvEstudiantes_SelectionChanged;

            _tarjetaBusqueda.Controls.AddRange(new Control[] { _lblTituloBusqueda, _campoBuscar, _dgvEstudiantes });
        }

        private void ConstruirTarjetaRegistro()
        {
            _tarjetaRegistro = new TarjetaBlanca();

            _lblTituloRegistro = new Label
            {
                Text = "Nuevo Registro Manual",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            _lblSinSeleccion = new Label
            {
                Text = "Selecciona un estudiante de la lista\npara registrar su asistencia manualmente.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Colores.GrisSecundario,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            _picAvatar = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, Visible = false };

            _lblNombreSeleccionado = new Label
            {
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = false,
                AutoEllipsis = true,
                Visible = false
            };

            _lblNieSeleccionado = new Label
            {
                Font = new Font("Segoe UI", 9),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                Visible = false
            };

            _lblMotivoEtiqueta = CrearEtiqueta("Tipo de Excepcion (Motivo)");
            _cmbMotivo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Flat };
            _cmbMotivo.Items.AddRange(new object[]
            {
                "Olvido el carnet",
                "Codigo QR danado",
                "Camara fuera de servicio",
                "Justificacion medica",
                "Autorizacion administrativa"
            });

            _lblNotasEtiqueta = CrearEtiqueta("Observaciones adicionales (opcional)");
            _campoNotas = new CampoSubrayado();

            _lblFechaHoraEtiqueta = CrearEtiqueta("Fecha y Hora de Registro");
            _lblFechaHoraValor = new Label
            {
                Font = new Font("Segoe UI", 10),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            _btnRegistrar = new BotonPrimario { Text = "Confirmar y Registrar Asistencia" };
            _btnRegistrar.Click += BtnRegistrar_Click;

            _lblResultado = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = false
            };

            _tarjetaRegistro.Controls.AddRange(new Control[]
            {
                _lblTituloRegistro, _lblSinSeleccion, _picAvatar, _lblNombreSeleccionado, _lblNieSeleccionado,
                _lblMotivoEtiqueta, _cmbMotivo, _lblNotasEtiqueta, _campoNotas,
                _lblFechaHoraEtiqueta, _lblFechaHoraValor, _btnRegistrar, _lblResultado
            });

            MostrarSinSeleccion();
        }

        private static Label CrearEtiqueta(string texto) => new()
        {
            Text = texto,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Colores.GrisOscuro,
            AutoSize = true
        };

        /// <summary>
        /// Recalcula tamanos, fuentes y posiciones de todo. Escala
        /// basada en ancho Y alto (el mas chico manda).
        /// </summary>
        private void AplicarEscala()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            float escalaAncho = ClientSize.Width / 1150f;
            float escalaAlto = ClientSize.Height / 720f;
            _escala = Math.Clamp(Math.Min(escalaAncho, escalaAlto), 1f, 1.6f);

            int anchoTotal = _panelContenido.ClientSize.Width - _panelContenido.Padding.Horizontal;
            int altoTotal = _panelContenido.ClientSize.Height - _panelContenido.Padding.Vertical;
            if (anchoTotal <= 0 || altoTotal <= 0) return;

            int x0 = _panelContenido.Padding.Left;
            int y0 = _panelContenido.Padding.Top;

            int alturaRegresar = (int)(42 * _escala);
            int gapExtra = (int)(14 * _escala);
            int alturaTarjetas = altoTotal - alturaRegresar - gapExtra;

            int espacioColumnas = (int)(20 * _escala);
            int anchoBusqueda = (int)(anchoTotal * 0.54);
            int anchoRegistro = anchoTotal - anchoBusqueda - espacioColumnas;

            _tarjetaBusqueda.Location = new Point(x0, y0);
            _tarjetaBusqueda.Size = new Size(anchoBusqueda, alturaTarjetas);

            _tarjetaRegistro.Location = new Point(x0 + anchoBusqueda + espacioColumnas, y0);
            _tarjetaRegistro.Size = new Size(anchoRegistro, alturaTarjetas);

            _btnRegresar.Size = new Size((int)(130 * _escala), alturaRegresar);
            _btnRegresar.Location = new Point(x0, y0 + alturaTarjetas + gapExtra);

            // --- Tarjeta de busqueda ---
            int margen = (int)(22 * _escala);
            int anchoInternoBusqueda = Math.Max(anchoBusqueda - margen * 2, 100);

            _lblTituloBusqueda.Font = new Font("Segoe UI", 13 * _escala, FontStyle.Bold);
            _lblTituloBusqueda.Location = new Point(margen, margen);

            int y = _lblTituloBusqueda.Bottom + (int)(14 * _escala);
            _campoBuscar.Width = anchoInternoBusqueda;
            _campoBuscar.Location = new Point(margen, y);
            _campoBuscar.EscalarA(_escala);

            y = _campoBuscar.Bottom + (int)(14 * _escala);
            _dgvEstudiantes.Font = new Font("Segoe UI", 9.5f * _escala);
            _dgvEstudiantes.ColumnHeadersHeight = (int)(32 * _escala);
            _dgvEstudiantes.RowTemplate.Height = (int)(30 * _escala);
            _dgvEstudiantes.Location = new Point(margen, y);
            _dgvEstudiantes.Size = new Size(anchoInternoBusqueda, Math.Max(alturaTarjetas - y - margen, 100));
            SincronizarAlturaFilas();

            
            // --- Tarjeta de registro ---
            int anchoInternoRegistro = Math.Max(anchoRegistro - margen * 2, 100);

            _lblTituloRegistro.Font = new Font("Segoe UI", 13 * _escala, FontStyle.Bold);
            _lblTituloRegistro.Location = new Point(margen, margen);

            y = _lblTituloRegistro.Bottom + (int)(24 * _escala);

            _lblSinSeleccion.Location = new Point(margen, y);
            _lblSinSeleccion.Size = new Size(anchoInternoRegistro, (int)(60 * _escala));
            _lblSinSeleccion.Font = new Font("Segoe UI", 10 * _escala);

            int ladoAvatar = (int)(64 * _escala); 
            _picAvatar.Size = new Size(ladoAvatar, ladoAvatar);
            _picAvatar.Location = new Point(margen, y);

            int xTextoAvatar = margen + ladoAvatar + (int)(16 * _escala);
            int anchoTextoAvatar = Math.Max(anchoInternoRegistro - ladoAvatar - (int)(16 * _escala), 80);
            int altoNombre = (int)(48 * _escala); 

            _lblNombreSeleccionado.Font = new Font("Segoe UI", 12 * _escala, FontStyle.Bold);
            _lblNombreSeleccionado.Size = new Size(anchoTextoAvatar, altoNombre);
            _lblNombreSeleccionado.Location = new Point(xTextoAvatar, y);

           
            _lblNieSeleccionado.Font = new Font("Segoe UI", 9 * _escala);
            _lblNieSeleccionado.Location = new Point(xTextoAvatar, _lblNombreSeleccionado.Bottom);

        
            y = Math.Max(_picAvatar.Bottom, _lblNieSeleccionado.Bottom) + (int)(32 * _escala);


            _lblMotivoEtiqueta.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _lblMotivoEtiqueta.Location = new Point(margen, y);
            y = _lblMotivoEtiqueta.Bottom + (int)(6 * _escala);

            _cmbMotivo.Font = new Font("Segoe UI", 9.5f * _escala);
            _cmbMotivo.Width = anchoInternoRegistro;
            _cmbMotivo.Location = new Point(margen, y);
            y = _cmbMotivo.Bottom + (int)(24 * _escala); 

            _lblNotasEtiqueta.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _lblNotasEtiqueta.Location = new Point(margen, y);
            y = _lblNotasEtiqueta.Bottom + (int)(6 * _escala);

            _campoNotas.Width = anchoInternoRegistro;
            _campoNotas.Location = new Point(margen, y);
            _campoNotas.EscalarA(_escala);
            y = _campoNotas.Bottom + (int)(24 * _escala);

            _lblFechaHoraEtiqueta.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _lblFechaHoraEtiqueta.Location = new Point(margen, y);
            y = _lblFechaHoraEtiqueta.Bottom + (int)(6 * _escala);

            _lblFechaHoraValor.Font = new Font("Segoe UI", 10 * _escala);
            _lblFechaHoraValor.Location = new Point(margen, y);

            
            int altoResultado = (int)(50 * _escala);
            int altoBoton = (int)(44 * _escala);

            int yResultado = alturaTarjetas - margen - altoResultado;
            _lblResultado.Font = new Font("Segoe UI", 10 * _escala, FontStyle.Bold);
            _lblResultado.Size = new Size(anchoInternoRegistro, altoResultado);
            _lblResultado.Location = new Point(margen, yResultado);

            int yBoton = yResultado - altoBoton - (int)(10 * _escala);
            _btnRegistrar.Font = new Font("Segoe UI", 10 * _escala, FontStyle.Bold);
            _btnRegistrar.Size = new Size(anchoInternoRegistro, altoBoton);
            _btnRegistrar.Location = new Point(margen, yBoton);
        }
    }
}
