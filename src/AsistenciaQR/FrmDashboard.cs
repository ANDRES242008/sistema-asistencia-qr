using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Panel de Asistencia del Dia, rediseñado en la Fase 4:
    /// menu lateral de navegacion, tarjetas de estadisticas,
    /// busqueda en vivo, tabla paginada con badges de color para
    /// la puntualidad, y exportacion a Excel / vista de impresion.
    /// </summary>
    public class FrmDashboard : Form
    {
        private const int FilasPorPagina = 10;

        private readonly DashboardService _dashboardService = new();
        private float _escala = 1f;

        private List<AsistenciaDetalle> _ultimoResultadoCompleto = new();
        private List<AsistenciaDetalle> _datosFiltrados = new();
        private int _paginaActual;

        private BarraLateral _barraLateral = null!;
        private Panel _panelContenido = null!;

        private DateTimePicker _dtpFecha = null!;
        private ComboBox _cmbSeccion = null!;
        private BotonPrimario _btnActualizar = null!;

        private TarjetaEstadistica _tarjetaPresentes = null!;
        private TarjetaEstadistica _tarjetaFaltantes = null!;
        private TarjetaEstadistica _tarjetaTardanzas = null!;
        private TarjetaEstadistica _tarjetaPorcentaje = null!;

        private CampoTexto _campoBuscar = null!;
        private Button _btnExportarExcel = null!;
        private Button _btnImprimir = null!;

        private DataGridView _dgvDetalle = null!;
        private Label _lblPaginacion = null!;
        private Button _btnPaginaAnterior = null!;
        private Button _btnPaginaSiguiente = null!;

        public FrmDashboard()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarSecciones(); ActualizarDashboard(); AplicarEscala(); };
        }

        private void CargarSecciones()
        {
            _cmbSeccion.Items.Clear();
            _cmbSeccion.Items.Add("Todas");
            foreach (var seccion in _dashboardService.ObtenerSecciones())
            {
                _cmbSeccion.Items.Add(seccion);
            }
            _cmbSeccion.SelectedIndex = 0;
        }

        private void ActualizarDashboard()
        {
            string? seccion = _cmbSeccion.SelectedIndex > 0 ? _cmbSeccion.SelectedItem?.ToString() : null;
            var resumen = _dashboardService.ObtenerResumen(_dtpFecha.Value.Date, seccion);

            _tarjetaPresentes.Valor = resumen.Presentes.ToString();
            _tarjetaFaltantes.Valor = resumen.Faltantes.ToString();
            _tarjetaTardanzas.Valor = resumen.Tardanzas.ToString();
            _tarjetaPorcentaje.Valor = $"{resumen.Porcentaje:0.0}%";

            _ultimoResultadoCompleto = resumen.Detalle;
            AplicarBusqueda();
        }

        private void AplicarBusqueda()
        {
            string filtro = _campoBuscar.ObtenerValorReal().Trim();

            _datosFiltrados = string.IsNullOrEmpty(filtro)
                ? _ultimoResultadoCompleto
                : _ultimoResultadoCompleto.Where(d =>
                    d.NombreCompleto.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    d.NIE.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            _paginaActual = 0;
            MostrarPagina();
        }

        private void MostrarPagina()
        {
            var pagina = _datosFiltrados.Skip(_paginaActual * FilasPorPagina).Take(FilasPorPagina).ToList();

            _dgvDetalle.DataSource = null;
            _dgvDetalle.DataSource = pagina;
            ConfigurarColumnasGrid();
            SincronizarAlturaFilas();

            int total = _datosFiltrados.Count;
            int inicio = total == 0 ? 0 : _paginaActual * FilasPorPagina + 1;
            int fin = Math.Min((_paginaActual + 1) * FilasPorPagina, total);
            _lblPaginacion.Text = $"{inicio}-{fin} de {total}";

            _btnPaginaAnterior.Enabled = _paginaActual > 0;
            _btnPaginaSiguiente.Enabled = fin < total;
        }

        private void ConfigurarColumnasGrid()
        {
            OcultarColumna("Observacion");

            if (_dgvDetalle.Columns["NombreCompleto"] is { } colNombre) colNombre.HeaderText = "Nombre Completo";
            if (_dgvDetalle.Columns["EstadoPuntualidad"] is { } colEstado) colEstado.HeaderText = "Estado";
            if (_dgvDetalle.Columns["Hora"] is { } colHora) colHora.DefaultCellStyle.Format = @"hh\:mm";
        }

        private void OcultarColumna(string nombre)
        {
            if (_dgvDetalle.Columns[nombre] is { } columna) columna.Visible = false;
        }

        /// <summary>
        /// RowTemplate.Height solo afecta filas NUEVAS, no las que ya
        /// existen en la tabla. Por eso "A tiempo" se veia cortado
        /// despues de cambiar el tamano de la ventana - aqui se
        /// fuerza a cada fila visible a usar el alto correcto.
        /// </summary>
        private void SincronizarAlturaFilas()
        {
            int alto = (int)(28 * _escala);
            foreach (DataGridViewRow fila in _dgvDetalle.Rows)
            {
                fila.Height = alto;
            }
        }

        private void DgvDetalle_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_dgvDetalle.Columns[e.ColumnIndex].Name != "EstadoPuntualidad") return;

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

        private void BtnExportarExcel_Click(object? sender, EventArgs e)
        {
            if (_datosFiltrados.Count == 0)
            {
                MessageBox.Show(this, "No hay datos para exportar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialogo = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"asistencia_{_dtpFecha.Value:yyyyMMdd}.xlsx"
            };

            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            new ReporteService().ExportarExcel(_datosFiltrados, dialogo.FileName);
            MessageBox.Show(this, "Exportado correctamente.", "Listo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnImprimir_Click(object? sender, EventArgs e)
        {
            if (_datosFiltrados.Count == 0)
            {
                MessageBox.Show(this, "No hay datos para imprimir.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int fila = 0;
            using var documento = new PrintDocument();

            documento.PrintPage += (_, ev) =>
            {
                var g = ev.Graphics!;
                using var fuenteTitulo = new Font("Segoe UI", 14, FontStyle.Bold);
                using var fuenteEncabezado = new Font("Segoe UI", 9, FontStyle.Bold);
                using var fuenteTexto = new Font("Segoe UI", 9);

                float y = ev.MarginBounds.Top;
                g.DrawString("Reporte de Asistencia del Dia", fuenteTitulo, Brushes.Black, ev.MarginBounds.Left, y);
                y += 28;
                g.DrawString($"Fecha: {_dtpFecha.Value:dd/MM/yyyy}", fuenteTexto, Brushes.Black, ev.MarginBounds.Left, y);
                y += 26;

                string[] encabezados = { "NIE", "Nombre", "Grado", "Seccion", "Hora", "Estado" };
                float[] anchos = { 80, 200, 60, 60, 70, 90 };

                void dibujarFila(string[] valores, Font fuente)
                {
                    float x = ev.MarginBounds.Left;
                    for (int i = 0; i < valores.Length; i++)
                    {
                        g.DrawString(valores[i], fuente, Brushes.Black, x, y);
                        x += anchos[i];
                    }
                    y += 20;
                }

                dibujarFila(encabezados, fuenteEncabezado);

                while (fila < _datosFiltrados.Count && y < ev.MarginBounds.Bottom)
                {
                    var d = _datosFiltrados[fila];
                    dibujarFila(new[] { d.NIE, d.NombreCompleto, d.Grado, d.Seccion, d.Hora.ToString(@"hh\:mm"), d.EstadoPuntualidad ?? "" }, fuenteTexto);
                    fila++;
                }

                ev.HasMorePages = fila < _datosFiltrados.Count;
            };

            try
            {
                using var vista = new PrintPreviewDialog { Document = documento, Width = 850, Height = 650 };
                vista.ShowDialog(this);
            }
            catch (System.Drawing.Printing.InvalidPrinterException)
            {
                MessageBox.Show(this,
                    "No se encontró ninguna impresora instalada en el sistema. Por favor, instala al menos una impresora virtual (como PDF) para ver la vista previa.",
                    "Impresora no disponible",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void AbrirSeccion(SeccionMenu seccion)
        {
            Form? destino = seccion switch
            {
                SeccionMenu.Dashboard => null,
                SeccionMenu.Estudiantes => new FrmEstudiantes(),
                SeccionMenu.Kiosco => new FrmKioscoEscaneo(),
                SeccionMenu.CarnetsQR => new FrmCarnetsQR(),
                SeccionMenu.Reportes => new FrmReportes(),
                SeccionMenu.Excepciones => new FrmExcepciones(),
                SeccionMenu.Configuracion => new FrmConfiguracion(),
                SeccionMenu.Horarios => new FrmHorarios(),
                _ => null
            };

            destino?.Show();
        }

        private void CerrarSesion()
        {
            var confirmar = MessageBox.Show(this, "Deseas cerrar sesion?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmar == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Panel de Asistencia del Dia";
            Width = 1200;
            Height = 760;
            MinimumSize = new Size(1000, 620);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Colores.GrisClaro;
            AutoScaleMode = AutoScaleMode.Dpi;

            ConstruirPanelContenido();

            _barraLateral = new BarraLateral(SeccionMenu.Dashboard);
            _barraLateral.SeccionSeleccionada += AbrirSeccion;
            _barraLateral.CerrarSesionSolicitado += CerrarSesion;
            Controls.Add(_barraLateral);

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

            _dtpFecha = new DateTimePicker { Format = DateTimePickerFormat.Short };
            _dtpFecha.ValueChanged += (_, _) => ActualizarDashboard();

            _cmbSeccion = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            _cmbSeccion.SelectedIndexChanged += (_, _) => ActualizarDashboard();

            _btnActualizar = new BotonPrimario { Text = "🔄 Actualizar" };
            _btnActualizar.Click += (_, _) => ActualizarDashboard();

            _tarjetaPresentes = new TarjetaEstadistica { Titulo = "Presentes", ColorFondo = Colores.VerdeEsmeralda };
            _tarjetaFaltantes = new TarjetaEstadistica { Titulo = "Faltantes", ColorFondo = Color.FromArgb(200, 55, 55) };
            _tarjetaTardanzas = new TarjetaEstadistica { Titulo = "Tardanzas", ColorFondo = Color.FromArgb(230, 159, 0) };
            _tarjetaPorcentaje = new TarjetaEstadistica { Titulo = "Porcentaje", ColorFondo = Colores.AzulMarino };

            _campoBuscar = new CampoTexto("🔍", "Buscar estudiante...");
            _campoBuscar.TextoCambiado += _ => AplicarBusqueda();

            _btnExportarExcel = CrearBotonSecundario("📄 Exportar Excel");
            _btnExportarExcel.Click += BtnExportarExcel_Click;

            _btnImprimir = CrearBotonSecundario("🖨️ Imprimir");
            _btnImprimir.Click += BtnImprimir_Click;

            _dgvDetalle = new DataGridView
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
            _dgvDetalle.CellPainting += DgvDetalle_CellPainting;

            _lblPaginacion = new Label { ForeColor = Colores.GrisSecundario, AutoSize = true };

            _btnPaginaAnterior = CrearBotonSecundario("←");
            _btnPaginaAnterior.Click += (_, _) => { _paginaActual--; MostrarPagina(); };

            _btnPaginaSiguiente = CrearBotonSecundario("→");
            _btnPaginaSiguiente.Click += (_, _) => { _paginaActual++; MostrarPagina(); };

            _panelContenido.Controls.AddRange(new Control[]
            {
                _dtpFecha, _cmbSeccion, _btnActualizar,
                _tarjetaPresentes, _tarjetaFaltantes, _tarjetaTardanzas, _tarjetaPorcentaje,
                _campoBuscar, _btnExportarExcel, _btnImprimir,
                _dgvDetalle, _lblPaginacion, _btnPaginaAnterior, _btnPaginaSiguiente
            });

            Controls.Add(_panelContenido);
        }

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
        /// Recalcula tamanos, fuentes y posiciones de TODO (incluido
        /// el menu lateral) segun el tamano de la ventana.
        /// </summary>
        private void AplicarEscala()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            float escalaAncho = ClientSize.Width / 1160f;
            float escalaAlto = ClientSize.Height / 720f;
            _escala = Math.Clamp(Math.Min(escalaAncho, escalaAlto), 1f, 1.7f);

            _barraLateral.EscalarA(_escala);

            int anchoTotal = _panelContenido.ClientSize.Width - _panelContenido.Padding.Horizontal;
            if (anchoTotal <= 0) return;

            int x = _panelContenido.Padding.Left;
            int y = _panelContenido.Padding.Top;

            _dtpFecha.Font = new Font("Segoe UI", 9.5f * _escala);
            _dtpFecha.Width = (int)(140 * _escala);
            _dtpFecha.Location = new Point(x, y);

            _cmbSeccion.Font = new Font("Segoe UI", 9.5f * _escala);
            _cmbSeccion.Width = (int)(140 * _escala);
            _cmbSeccion.Location = new Point(_dtpFecha.Right + (int)(16 * _escala), y);

            _btnActualizar.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _btnActualizar.Size = new Size((int)(130 * _escala), (int)(34 * _escala));
            _btnActualizar.Location = new Point(x + anchoTotal - _btnActualizar.Width, y - 2);

            y = Math.Max(_dtpFecha.Bottom, _cmbSeccion.Bottom) + (int)(20 * _escala);

            int espacioTarjetas = (int)(16 * _escala);
            int anchoTarjeta = (anchoTotal - espacioTarjetas * 3) / 4;
            int altoTarjeta = (int)(110 * _escala);

            var tarjetas = new[] { _tarjetaPresentes, _tarjetaFaltantes, _tarjetaTardanzas, _tarjetaPorcentaje };
            for (int i = 0; i < tarjetas.Length; i++)
            {
                tarjetas[i].Size = new Size(anchoTarjeta, altoTarjeta);
                tarjetas[i].Location = new Point(x + i * (anchoTarjeta + espacioTarjetas), y);
                tarjetas[i].FuenteTitulo = new Font("Segoe UI", 10 * _escala);
                tarjetas[i].FuenteValor = new Font("Segoe UI", 26 * _escala, FontStyle.Bold);
            }

            y += altoTarjeta + (int)(20 * _escala);

            _campoBuscar.Width = (int)(280 * _escala);
            _campoBuscar.Location = new Point(x, y);
            _campoBuscar.EscalarA(_escala);

            _btnImprimir.Font = new Font("Segoe UI", 9.5f * _escala);
            _btnImprimir.Size = new Size((int)(110 * _escala), (int)(34 * _escala));
            _btnImprimir.Location = new Point(x + anchoTotal - _btnImprimir.Width, y + (int)(5 * _escala));

            _btnExportarExcel.Font = new Font("Segoe UI", 9.5f * _escala);
            _btnExportarExcel.Size = new Size((int)(150 * _escala), (int)(34 * _escala));
            _btnExportarExcel.Location = new Point(_btnImprimir.Left - _btnExportarExcel.Width - (int)(10 * _escala), _btnImprimir.Top);

            y = _campoBuscar.Bottom + (int)(16 * _escala);

            int alturaPaginacion = (int)(30 * _escala);
            int yPaginacion = _panelContenido.ClientSize.Height - _panelContenido.Padding.Bottom - alturaPaginacion;

            _btnPaginaSiguiente.Size = new Size((int)(36 * _escala), alturaPaginacion);
            _btnPaginaSiguiente.Location = new Point(x + anchoTotal - _btnPaginaSiguiente.Width, yPaginacion);

            _btnPaginaAnterior.Size = new Size((int)(36 * _escala), alturaPaginacion);
            _btnPaginaAnterior.Location = new Point(_btnPaginaSiguiente.Left - _btnPaginaAnterior.Width - (int)(8 * _escala), yPaginacion);

            _lblPaginacion.Font = new Font("Segoe UI", 9 * _escala);
            _lblPaginacion.Location = new Point(x, yPaginacion + (int)(6 * _escala));

            int altoTabla = Math.Max(yPaginacion - y - (int)(10 * _escala), 100);
            _dgvDetalle.Font = new Font("Segoe UI", 9 * _escala);
            _dgvDetalle.ColumnHeadersHeight = (int)(32 * _escala);
            _dgvDetalle.RowTemplate.Height = (int)(28 * _escala);
            _dgvDetalle.Location = new Point(x, y);
            _dgvDetalle.Size = new Size(anchoTotal, altoTabla);
            SincronizarAlturaFilas();
        }
    }
}
