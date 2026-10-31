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
            Size = new Size(1150, 720);
            MinimumSize = new Size(900, 520);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Colores.GrisClaro;
            Padding = new Padding(24);

            var panelSuperior = new Panel { Dock = DockStyle.Top, Height = 118 };

            var lblTitulo = new Label
            {
                Text = "Bitacora de auditoria",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var lblAyuda = new Label
            {
                Text = "Quien hizo cada accion importante y cuando. Los escaneos de QR no aparecen aqui; estan en Reportes.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                Location = new Point(2, 36)
            };

            const int yFiltros = 70;
            _dtpDesde = CrearFecha(DateTime.Today.AddDays(-30), new Point(0, yFiltros + 8));
            _dtpHasta = CrearFecha(DateTime.Today, new Point(150, yFiltros + 8));

            _cmbUsuario = CrearCombo(new Point(300, yFiltros + 8), 150);
            _cmbAccion = CrearCombo(new Point(462, yFiltros + 8), 210);

            _campoBuscar = new CampoTexto("🔍", "Buscar en el detalle (NIE, nombre...)")
            {
                Location = new Point(686, yFiltros),
                Width = 260
            };
            _campoBuscar.TextoCambiado += _ => CargarRegistros();

            var btnExportar = new Button
            {
                Text = "📊  Exportar a Excel",
                FlatStyle = FlatStyle.Flat,
                BackColor = Colores.BlancoPuro,
                ForeColor = Colores.GrisOscuro,
                Font = new Font("Segoe UI Emoji", 9.5f),
                Size = new Size(170, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExportar.FlatAppearance.BorderColor = Color.FromArgb(219, 224, 230);
            btnExportar.Click += BtnExportar_Click;
            panelSuperior.Resize += (_, _) => btnExportar.Location = new Point(panelSuperior.Width - btnExportar.Width, yFiltros + 2);

            panelSuperior.Controls.AddRange(new Control[]
            {
                lblTitulo, lblAyuda, _dtpDesde, _dtpHasta, _cmbUsuario, _cmbAccion, _campoBuscar, btnExportar
            });

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
                Font = new Font("Segoe UI", 9.5f)
            };
            _dgvRegistros.RowTemplate.Height = 28;
            _dgvRegistros.ColumnHeadersHeight = 32;
            _dgvRegistros.Columns.Add("Fecha", "Fecha");
            _dgvRegistros.Columns.Add("Usuario", "Usuario");
            _dgvRegistros.Columns.Add("Accion", "Accion");
            _dgvRegistros.Columns.Add("Detalle", "Detalle");
            _dgvRegistros.Columns["Fecha"]!.FillWeight = 70;
            _dgvRegistros.Columns["Usuario"]!.FillWeight = 50;
            _dgvRegistros.Columns["Accion"]!.FillWeight = 70;
            _dgvRegistros.Columns["Detalle"]!.FillWeight = 260;

            var panelInferior = new Panel { Dock = DockStyle.Bottom, Height = 56 };

            var btnRegresar = new BotonRegresar { Size = new Size(160, 42), Location = new Point(0, 12) };
            btnRegresar.Click += (_, _) => Close();

            _lblTotal = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                Location = new Point(180, 24)
            };

            panelInferior.Controls.AddRange(new Control[] { btnRegresar, _lblTotal });

            Controls.Add(_dgvRegistros);
            Controls.Add(panelInferior);
            Controls.Add(panelSuperior);
        }

        private DateTimePicker CrearFecha(DateTime valor, Point ubicacion)
        {
            var selector = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = valor,
                Width = 140,
                Location = ubicacion,
                Font = new Font("Segoe UI", 9.5f)
            };
            selector.ValueChanged += (_, _) => CargarRegistros();
            return selector;
        }

        private ComboBox CrearCombo(Point ubicacion, int ancho)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Width = ancho,
                Location = ubicacion,
                Font = new Font("Segoe UI", 9.5f)
            };
            combo.SelectedIndexChanged += (_, _) => CargarRegistros();
            return combo;
        }
    }
}
