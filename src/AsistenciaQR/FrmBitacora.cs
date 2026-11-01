using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Consulta de la bitacora de auditoria (solo Administrador):
    /// quien hizo que y cuando, con filtros y exportacion a Excel.
    /// Es de solo lectura: nadie puede editar ni borrar registros
    /// desde el programa.
    /// </summary>
    public class FrmBitacora : Form
    {
        private const string Todos = "Todos";

        private List<RegistroBitacora> _registros = new();
        private bool _cargando;

        private DateTimePicker _dtpDesde = null!;
        private DateTimePicker _dtpHasta = null!;
        private ComboBox _cmbUsuario = null!;
        private ComboBox _cmbAccion = null!;
        private CampoTexto _campoBuscar = null!;
        private DataGridView _dgvRegistros = null!;
        private Label _lblTotal = null!;

        public FrmBitacora()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarFiltros(); CargarRegistros(); };
        }

        private void CargarFiltros()
        {
            _cargando = true;
            Rellenar(_cmbUsuario, BitacoraService.ObtenerUsuarios());
            Rellenar(_cmbAccion, BitacoraService.ObtenerAcciones());
            _cargando = false;

            static void Rellenar(ComboBox combo, List<string> valores)
            {
                combo.Items.Clear();
                combo.Items.Add(Todos);
                combo.Items.AddRange(valores.Cast<object>().ToArray());
                combo.SelectedIndex = 0;
            }
        }

        private void CargarRegistros()
        {
            if (_cargando) return;

            string? Elegido(ComboBox combo) => combo.SelectedIndex > 0 ? combo.SelectedItem?.ToString() : null;

            _registros = BitacoraService.Listar(_dtpDesde.Value, _dtpHasta.Value,
                Elegido(_cmbUsuario), Elegido(_cmbAccion), _campoBuscar.ObtenerValorReal());

            _dgvRegistros.Rows.Clear();
            foreach (var r in _registros)
            {
                _dgvRegistros.Rows.Add(r.Fecha.ToString("dd/MM/yyyy hh:mm:ss tt"), r.Usuario, r.Accion, r.Detalle);
            }

            _lblTotal.Text = _registros.Count >= Repositorios.BitacoraRepository.MaximoResultados
                ? $"Mostrando los {_registros.Count} mas recientes. Reduce el rango de fechas para ver el resto."
                : $"{_registros.Count} registros";
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {
            if (_registros.Count == 0)
            {
                MessageBox.Show(this, "No hay registros para exportar.", "Bitacora",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialogo = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"bitacora_{_dtpDesde.Value:yyyy-MM-dd}_a_{_dtpHasta.Value:yyyy-MM-dd}.xlsx"
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                using var libro = new XLWorkbook();
                var hoja = libro.Worksheets.Add("Bitacora");
                string[] encabezados = { "Fecha", "Usuario", "Accion", "Detalle" };
                for (int i = 0; i < encabezados.Length; i++)
                {
                    hoja.Cell(1, i + 1).Value = encabezados[i];
                    hoja.Cell(1, i + 1).Style.Font.Bold = true;
                }

                for (int f = 0; f < _registros.Count; f++)
                {
                    var r = _registros[f];
                    hoja.Cell(f + 2, 1).Value = r.Fecha;
                    hoja.Cell(f + 2, 1).Style.DateFormat.Format = "dd/MM/yyyy hh:mm:ss AM/PM";
                    hoja.Cell(f + 2, 2).Value = r.Usuario;
                    hoja.Cell(f + 2, 3).Value = r.Accion;
                    hoja.Cell(f + 2, 4).Value = r.Detalle;
                }

                hoja.Columns(1, 3).AdjustToContents();
                hoja.Column(4).Width = 100;
                libro.SaveAs(dialogo.FileName);
            }
            catch (IOException)
            {
                MessageBox.Show(this, "El archivo esta abierto en otro programa. Cierralo o guarda con otro nombre.",
                    "Archivo en uso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Bitacora de auditoria";
            Size = Escala.Ventana(this, 1150, 720);
            MinimumSize = Escala.Px(this, 900, 480);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Colores.GrisClaro;
            Padding = new Padding(Escala.Px(this, 24));

            // Titulo y ayuda apilados; debajo, los filtros en una fila que
            // se acomoda sola (y pasa a una segunda fila si no cabe).
            var panelSuperior = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };

            panelSuperior.Controls.Add(new Label
            {
                Text = "Bitacora de auditoria",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true
            });

            panelSuperior.Controls.Add(new Label
            {
                Text = "Quien hizo cada accion importante y cuando. Los escaneos de QR no aparecen aqui; estan en Reportes.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                Margin = new Padding(3, 0, 3, Escala.Px(this, 10))
            });

            var filaFiltros = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, Escala.Px(this, 10)),
                MaximumSize = new Size(Escala.Px(this, 1100), 0)
            };

            _dtpDesde = CrearFecha(DateTime.Today.AddDays(-30));
            _dtpHasta = CrearFecha(DateTime.Today);
            _cmbUsuario = CrearCombo(150);
            _cmbAccion = CrearCombo(210);

            _campoBuscar = new CampoTexto("🔍", "Buscar (NIE, nombre...)")
            {
                Width = Escala.Px(this, 240),
                Margin = new Padding(Escala.Px(this, 4), 0, Escala.Px(this, 10), 0)
            };
            _campoBuscar.TextoCambiado += _ => CargarRegistros();

            var btnExportar = Escala.Boton("📊  Exportar a Excel");
            btnExportar.Click += BtnExportar_Click;

            filaFiltros.Controls.AddRange(new Control[] { _dtpDesde, _dtpHasta, _cmbUsuario, _cmbAccion, _campoBuscar, btnExportar });
            panelSuperior.Controls.Add(filaFiltros);
            panelSuperior.Resize += (_, _) => filaFiltros.MaximumSize = new Size(Math.Max(panelSuperior.Width, 200), 0);

            _dgvRegistros = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Colores.BlancoPuro,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9.5f),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            _dgvRegistros.RowTemplate.Height = Escala.Px(this, 28);
            _dgvRegistros.Columns.Add("Fecha", "Fecha");
            _dgvRegistros.Columns.Add("Usuario", "Usuario");
            _dgvRegistros.Columns.Add("Accion", "Accion");
            _dgvRegistros.Columns.Add("Detalle", "Detalle");
            // Fecha, usuario y accion a la medida de su texto; el detalle ocupa el resto.
            foreach (var columna in new[] { "Fecha", "Usuario", "Accion" })
            {
                _dgvRegistros.Columns[columna]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            }

            var panelInferior = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(0, Escala.Px(this, 12), 0, 0)
            };

            var btnRegresar = new BotonRegresar { Size = Escala.Px(this, 160, 42) };
            btnRegresar.Click += (_, _) => Close();

            _lblTotal = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(Escala.Px(this, 16), Escala.Px(this, 12), 0, 0)
            };

            panelInferior.Controls.AddRange(new Control[] { btnRegresar, _lblTotal });

            Controls.Add(_dgvRegistros);
            Controls.Add(panelInferior);
            Controls.Add(panelSuperior);
        }

        private DateTimePicker CrearFecha(DateTime valor)
        {
            var selector = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = valor,
                Width = Escala.Px(this, 140),
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, Escala.Px(this, 6), Escala.Px(this, 10), 0)
            };
            selector.ValueChanged += (_, _) => CargarRegistros();
            return selector;
        }

        private ComboBox CrearCombo(int anchoDiseno)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Width = Escala.Px(this, anchoDiseno),
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, Escala.Px(this, 6), Escala.Px(this, 10), 0)
            };
            combo.SelectedIndexChanged += (_, _) => CargarRegistros();
            return combo;
        }
    }
}
