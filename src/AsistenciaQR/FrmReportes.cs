using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;

namespace AsistenciaQR
{
    /// <summary>
    /// Modulo de Reportes: un solo filtro combinado sirve tanto para
    /// ver el historico general (fecha, grado, seccion) como el
    /// historico de un estudiante especifico (usando el campo Buscar).
    /// Diseno funcional (el estilo visual final llega en la Fase 4).
    /// </summary>
    public class FrmReportes : Form
    {
        private readonly ReporteService _reporteService = new();

        private DateTimePicker _dtpInicio = null!;
        private DateTimePicker _dtpFin = null!;
        private ComboBox _cmbGrado = null!;
        private ComboBox _cmbSeccion = null!;
        private TextBox _txtBuscar = null!;
        private DataGridView _dgvResultados = null!;

        public FrmReportes()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarFiltros(); BuscarHistorico(); };
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

            var datos = _reporteService.ObtenerHistorico(
                _dtpInicio.Value.Date, _dtpFin.Value.Date, _txtBuscar.Text, grado, seccion);

            _dgvResultados.DataSource = null;
            _dgvResultados.DataSource = datos;

            if (_dgvResultados.Columns["NombreCompleto"] is { } colNombre)
            {
                colNombre.HeaderText = "Nombre completo";
            }
            if (_dgvResultados.Columns["Fecha"] is { } colFecha)
            {
                colFecha.DefaultCellStyle.Format = "dd/MM/yyyy";
            }
            if (_dgvResultados.Columns["Hora"] is { } colHora)
            {
                colHora.DefaultCellStyle.Format = @"hh\:mm\:ss";
            }
        }

        private void BtnExportarExcel_Click(object? sender, EventArgs e)
        {
            if (_dgvResultados.DataSource is not List<AsistenciaDetalle> datos || datos.Count == 0)
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

            _reporteService.ExportarExcel(datos, dialogo.FileName);
            MessageBox.Show(this, "Reporte exportado correctamente.", "Listo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExportarPdf_Click(object? sender, EventArgs e)
        {
            if (_dgvResultados.DataSource is not List<AsistenciaDetalle> datos || datos.Count == 0)
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

            _reporteService.ExportarPdf(datos, dialogo.FileName);
            MessageBox.Show(this, "Reporte exportado correctamente.", "Listo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Reportes";
            Width = 990;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            var lblInicio = new Label { Text = "Desde:", Location = new Point(15, 20), AutoSize = true };
            _dtpInicio = new DateTimePicker
            {
                Location = new Point(85, 17),
                Width = 130,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };
            _dtpInicio.ValueChanged += (_, _) => BuscarHistorico();

            var lblFin = new Label { Text = "Hasta:", Location = new Point(230, 20), AutoSize = true };
            _dtpFin = new DateTimePicker
            {
                Location = new Point(300, 17),
                Width = 130,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            _dtpFin.ValueChanged += (_, _) => BuscarHistorico();

            var lblGrado = new Label { Text = "Grado:", Location = new Point(445, 20), AutoSize = true };
            _cmbGrado = new ComboBox
            {
                Location = new Point(515, 17),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbGrado.SelectedIndexChanged += (_, _) => BuscarHistorico();

            var lblSeccion = new Label { Text = "Seccion:", Location = new Point(670, 20), AutoSize = true };
            _cmbSeccion = new ComboBox
            {
                Location = new Point(750, 17),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbSeccion.SelectedIndexChanged += (_, _) => BuscarHistorico();

            var lblBuscar = new Label { Text = "Buscar:", Location = new Point(15, 65), AutoSize = true };
            _txtBuscar = new TextBox { Location = new Point(95, 62), Width = 230 };
            _txtBuscar.TextChanged += (_, _) => BuscarHistorico();

            var btnExportarExcel = new Button
            {
                Text = "Exportar a Excel",
                Location = new Point(600, 60),
                Size = new Size(170, 34),
                Font = new Font("Segoe UI", 9)
            };
            btnExportarExcel.Click += BtnExportarExcel_Click;

            var btnExportarPdf = new Button
            {
                Text = "Exportar a PDF",
                Location = new Point(790, 60),
                Size = new Size(170, 34),
                Font = new Font("Segoe UI", 9)
            };
            btnExportarPdf.Click += BtnExportarPdf_Click;

            _dgvResultados = new DataGridView
            {
                Location = new Point(15, 115),
                Size = new Size(945, 535),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            Controls.Add(lblInicio);
            Controls.Add(_dtpInicio);
            Controls.Add(lblFin);
            Controls.Add(_dtpFin);
            Controls.Add(lblGrado);
            Controls.Add(_cmbGrado);
            Controls.Add(lblSeccion);
            Controls.Add(_cmbSeccion);
            Controls.Add(lblBuscar);
            Controls.Add(_txtBuscar);
            Controls.Add(btnExportarExcel);
            Controls.Add(btnExportarPdf);
            Controls.Add(_dgvResultados);
        }
    }
}
