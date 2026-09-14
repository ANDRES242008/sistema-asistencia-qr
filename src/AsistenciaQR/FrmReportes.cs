using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    
    public class FrmReportes : Form
    {
        private const int FilasPorPagina = 10;

        private readonly ReporteService _reporteService = new();
        private float _escala = 1f;

        private List<AsistenciaDetalle> _datosCompletos = new();
        private int _paginaActual;

        private Panel _panelContenido = null!;
        private TarjetaBlanca _tarjetaPrincipal = null!;
        private BotonRegresar _btnRegresar = null!;

        private Label _lblDesde = null!;
        private DateTimePicker _dtpInicio = null!;
        private Label _lblHasta = null!;
        private DateTimePicker _dtpFin = null!;
        private Label _lblGrado = null!;
        private ComboBox _cmbGrado = null!;
        private Label _lblSeccion = null!;
        private ComboBox _cmbSeccion = null!;

        private CampoTexto _campoBuscar = null!;
        private Button _btnExportarExcel = null!;
        private Button _btnExportarPdf = null!;

        private DataGridView _dgvResultados = null!;
        private Label _lblPaginacion = null!;
        private Button _btnPaginaAnterior = null!;
        private Button _btnPaginaSiguiente = null!;

        public FrmReportes()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarFiltros(); BuscarHistorico(); AplicarEscala(); };
        }

        private void CargarFiltros()
        {
            _cmbGrado.Items.Clear();
            _cmbGrado.Items.Add("Todos");
            foreach (var grado in _reporteService.ObtenerGrados())
            {
                _cmbGrado.Items.Add(grado);
            }
            _cmbGrado.SelectedIndex = 0;

            _cmbSeccion.Items.Clear();
            _cmbSeccion.Items.Add("Todas");
            foreach (var seccion in _reporteService.ObtenerSecciones())
            {
                _cmbSeccion.Items.Add(seccion);
            }
            _cmbSeccion.SelectedIndex = 0;
        }

        private void BuscarHistorico()
        {
            string? grado = _cmbGrado.SelectedIndex > 0 ? _cmbGrado.SelectedItem?.ToString() : null;
            string? seccion = _cmbSeccion.SelectedIndex > 0 ? _cmbSeccion.SelectedItem?.ToString() : null;

            _datosCompletos = _reporteService.ObtenerHistorico(
                _dtpInicio.Value.Date, _dtpFin.Value.Date, _campoBuscar.ObtenerValorReal(), grado, seccion);

            _paginaActual = 0;
            MostrarPagina();
        }

        private void MostrarPagina()
        {
            var pagina = _datosCompletos.Skip(_paginaActual * FilasPorPagina).Take(FilasPorPagina).ToList();

            _dgvResultados.DataSource = null;
            _dgvResultados.DataSource = pagina;
            ConfigurarColumnasGrid();
            SincronizarAlturaFilas();

            int total = _datosCompletos.Count;
            int inicio = total == 0 ? 0 : _paginaActual * FilasPorPagina + 1;
            int fin = Math.Min((_paginaActual + 1) * FilasPorPagina, total);
            _lblPaginacion.Text = $"Mostrando {inicio}-{fin} de {total} registros";

            _btnPaginaAnterior.Enabled = _paginaActual > 0;
            _btnPaginaSiguiente.Enabled = fin < total;
        }

        private void ConfigurarColumnasGrid()
        {
            if (_dgvResultados.Columns["NombreCompleto"] is { } colNombre) colNombre.HeaderText = "Nombre Completo";
            if (_dgvResultados.Columns["Fecha"] is { } colFecha) colFecha.DefaultCellStyle.Format = "dd/MM/yyyy";
            if (_dgvResultados.Columns["Hora"] is { } colHora) colHora.DefaultCellStyle.Format = @"hh\:mm\:ss";
            if (_dgvResultados.Columns["EstadoPuntualidad"] is { } colEstado) colEstado.HeaderText = "Estado";
        }

        private void SincronizarAlturaFilas()
        {
            int alto = (int)(28 * _escala);
            foreach (DataGridViewRow fila in _dgvResultados.Rows)
            {
                fila.Height = alto;
            }
        }

        private void DgvResultados_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_dgvResultados.Columns[e.ColumnIndex].Name != "EstadoPuntualidad") return;

            e.PaintBackground(e.CellBounds, true);

            string valor = e.Value?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(valor)) { e.Handled = true; return; }

            bool esATiempo = valor == "A tiempo";
            Color color = esATiempo ? Colores.VerdeEsmeralda : Color.FromArgb(230, 159, 0);
            string texto = esATiempo ? "✓ A tiempo" : "🕐 Tarde";

            var g = e.Graphics!;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var fuente = e.CellStyle!.Font;
            var medida = TextRenderer.MeasureText(texto, fuente);
            int anchoPill = medida.Width + 22;
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

        private void BtnExportarExcel_Click(object? sender, EventArgs e)
        {
            if (_datosCompletos.Count == 0)
            {
                MessageBox.Show(this, "No hay datos para exportar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialogo = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"reporte_asistencia_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            _reporteService.ExportarExcel(_datosCompletos, dialogo.FileName);
            MessageBox.Show(this, "Reporte exportado correctamente.", "Listo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExportarPdf_Click(object? sender, EventArgs e)
        {
            if (_datosCompletos.Count == 0)
            {
                MessageBox.Show(this, "No hay datos para exportar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialogo = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"reporte_asistencia_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            _reporteService.ExportarPdf(_datosCompletos, dialogo.FileName);
            MessageBox.Show(this, "Reporte exportado correctamente.", "Listo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            Text = "AsistenciaQR - Reportes de Asistencia";
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

            _tarjetaPrincipal = new TarjetaBlanca();

            _lblDesde = CrearEtiqueta("Desde:");
            _dtpInicio = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };
            _dtpInicio.ValueChanged += (_, _) => BuscarHistorico();

            _lblHasta = CrearEtiqueta("Hasta:");
            _dtpFin = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            _dtpFin.ValueChanged += (_, _) => BuscarHistorico();

            _lblGrado = CrearEtiqueta("Grado:");
            _cmbGrado = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            _cmbGrado.SelectedIndexChanged += (_, _) => BuscarHistorico();

            _lblSeccion = CrearEtiqueta("Seccion:");
            _cmbSeccion = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            _cmbSeccion.SelectedIndexChanged += (_, _) => BuscarHistorico();

            _campoBuscar = new CampoTexto("🔍", "Buscar NIE o Nombre...");
            _campoBuscar.TextoCambiado += _ => BuscarHistorico();

            _btnExportarExcel = CrearBotonSecundario("📊 Exportar a Excel");
            _btnExportarExcel.Click += BtnExportarExcel_Click;

            _btnExportarPdf = CrearBotonSecundario("📄 Exportar a PDF");
            _btnExportarPdf.Click += BtnExportarPdf_Click;

            _dgvResultados = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Colores.BlancoPuro,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            _dgvResultados.CellPainting += DgvResultados_CellPainting;

            _lblPaginacion = new Label { ForeColor = Colores.GrisSecundario, AutoSize = true };

            _btnPaginaAnterior = CrearBotonSecundario("←");
            _btnPaginaAnterior.Click += (_, _) => { _paginaActual--; MostrarPagina(); };

            _btnPaginaSiguiente = CrearBotonSecundario("→");
            _btnPaginaSiguiente.Click += (_, _) => { _paginaActual++; MostrarPagina(); };

            _tarjetaPrincipal.Controls.AddRange(new Control[]
            {
                _lblDesde, _dtpInicio, _lblHasta, _dtpFin, _lblGrado, _cmbGrado, _lblSeccion, _cmbSeccion,
                _campoBuscar, _btnExportarExcel, _btnExportarPdf,
                _dgvResultados, _lblPaginacion, _btnPaginaAnterior, _btnPaginaSiguiente
            });

            _btnRegresar = new BotonRegresar();
            _btnRegresar.Click += BtnRegresar_Click;

            _panelContenido.Controls.AddRange(new Control[] { _tarjetaPrincipal, _btnRegresar });
            Controls.Add(_panelContenido);
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

            int alturaRegresar = (int)(42 * _escala);
            int gapExtra = (int)(14 * _escala);

            _tarjetaPrincipal.Location = new Point(_panelContenido.Padding.Left, _panelContenido.Padding.Top);
            _tarjetaPrincipal.Size = new Size(anchoTotal, altoTotal - alturaRegresar - gapExtra);

            _btnRegresar.Size = new Size((int)(130 * _escala), alturaRegresar);
            _btnRegresar.Location = new Point(_panelContenido.Padding.Left, _tarjetaPrincipal.Bottom + gapExtra);

            int margen = (int)(24 * _escala);
            int anchoInterno = Math.Max(_tarjetaPrincipal.Width - margen * 2, 100);
            int x = margen;
            int y = margen;

            void ColocarFiltro(Label etiqueta, Control control, int anchoControl)
            {
                etiqueta.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
                etiqueta.Location = new Point(x, y);

                control.Font = new Font("Segoe UI", 9.5f * _escala);
                control.Size = new Size(anchoControl, control.Height);
                control.Location = new Point(x, etiqueta.Bottom + (int)(4 * _escala));

                x = control.Right + (int)(20 * _escala);
            }

            ColocarFiltro(_lblDesde, _dtpInicio, (int)(130 * _escala));
            ColocarFiltro(_lblHasta, _dtpFin, (int)(130 * _escala));
            ColocarFiltro(_lblGrado, _cmbGrado, (int)(140 * _escala));
            ColocarFiltro(_lblSeccion, _cmbSeccion, (int)(120 * _escala));

            y = _dtpInicio.Bottom + (int)(20 * _escala);
            x = margen;

            _campoBuscar.Width = (int)(260 * _escala);
            _campoBuscar.Location = new Point(x, y);
            _campoBuscar.EscalarA(_escala);

            _btnExportarPdf.Font = new Font("Segoe UI", 9.5f * _escala);
            _btnExportarPdf.Size = new Size((int)(150 * _escala), (int)(34 * _escala));
            _btnExportarPdf.Location = new Point(margen + anchoInterno - _btnExportarPdf.Width, y + (int)(5 * _escala));

            _btnExportarExcel.Font = new Font("Segoe UI", 9.5f * _escala);
            _btnExportarExcel.Size = new Size((int)(160 * _escala), (int)(34 * _escala));
            _btnExportarExcel.Location = new Point(_btnExportarPdf.Left - _btnExportarExcel.Width - (int)(10 * _escala), _btnExportarPdf.Top);

            y = _campoBuscar.Bottom + (int)(18 * _escala);

            int alturaPaginacion = (int)(34 * _escala);
            int yPaginacion = _tarjetaPrincipal.Height - margen - alturaPaginacion;

            _btnPaginaSiguiente.Size = new Size((int)(36 * _escala), alturaPaginacion);
            _btnPaginaSiguiente.Location = new Point(margen + anchoInterno - _btnPaginaSiguiente.Width, yPaginacion);

            _btnPaginaAnterior.Size = new Size((int)(36 * _escala), alturaPaginacion);
            _btnPaginaAnterior.Location = new Point(_btnPaginaSiguiente.Left - _btnPaginaAnterior.Width - (int)(8 * _escala), yPaginacion);

            _lblPaginacion.Font = new Font("Segoe UI", 9 * _escala);
            _lblPaginacion.Location = new Point(margen, yPaginacion + (int)(8 * _escala));

            int altoTabla = Math.Max(yPaginacion - y - (int)(10 * _escala), 100);
            _dgvResultados.Font = new Font("Segoe UI", 9 * _escala);
            _dgvResultados.ColumnHeadersHeight = (int)(32 * _escala);
            _dgvResultados.RowTemplate.Height = (int)(28 * _escala);
            _dgvResultados.Location = new Point(margen, y);
            _dgvResultados.Size = new Size(anchoInterno, altoTabla);
            SincronizarAlturaFilas();
        }
    }
}
