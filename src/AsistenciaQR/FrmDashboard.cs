using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Servicios;

namespace AsistenciaQR
{
    /// <summary>
    /// Panel de asistencia del dia: cuenta presentes/faltantes,
    /// porcentaje, y lista el detalle, con filtro por fecha y
    /// seccion. Diseno funcional (el estilo visual final con
    /// graficos llega en la Fase 4, combinado con el Kiosco).
    /// </summary>
    public class FrmDashboard : Form
    {
        private readonly DashboardService _dashboardService = new();

        private DateTimePicker _dtpFecha = null!;
        private ComboBox _cmbSeccion = null!;

        private Label _lblPresentesValor = null!;
        private Label _lblFaltantesValor = null!;
        private Label _lblPorcentajeValor = null!;

        private DataGridView _dgvDetalle = null!;

        public FrmDashboard()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarSecciones(); ActualizarDashboard(); };
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
            string? seccion = _cmbSeccion.SelectedIndex > 0
                ? _cmbSeccion.SelectedItem?.ToString()
                : null;

            var resumen = _dashboardService.ObtenerResumen(_dtpFecha.Value.Date, seccion);

            _lblPresentesValor.Text = resumen.Presentes.ToString();
            _lblFaltantesValor.Text = resumen.Faltantes.ToString();
            _lblPorcentajeValor.Text = $"{resumen.Porcentaje:0.0}%";

            _dgvDetalle.DataSource = null;
            _dgvDetalle.DataSource = resumen.Detalle;

            if (_dgvDetalle.Columns["Fecha"] is { } colFecha)
            {
                colFecha.Visible = false;
            }
            if (_dgvDetalle.Columns["Hora"] is { } colHora)
            {
                colHora.DefaultCellStyle.Format = @"hh\:mm\:ss";
            }
            if (_dgvDetalle.Columns["NombreCompleto"] is { } colNombre)
            {
                colNombre.HeaderText = "Nombre completo";
            }
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Panel de Asistencia del Dia";
            Width = 840;
            Height = 660;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            var lblFecha = new Label { Text = "Fecha:", Location = new Point(15, 20), AutoSize = true };
            _dtpFecha = new DateTimePicker
            {
                Location = new Point(75, 17),
                Width = 145,
                Format = DateTimePickerFormat.Short
            };
            _dtpFecha.ValueChanged += (_, _) => ActualizarDashboard();

            var lblSeccion = new Label { Text = "Seccion:", Location = new Point(240, 20), AutoSize = true };
            _cmbSeccion = new ComboBox
            {
                Location = new Point(310, 17),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbSeccion.SelectedIndexChanged += (_, _) => ActualizarDashboard();

            var btnActualizar = new Button
            {
                Text = "Actualizar",
                Location = new Point(445, 15),
                Size = new Size(100, 30)
            };
            btnActualizar.Click += (_, _) => ActualizarDashboard();

            var pnlPresentes = CrearPanelEstadistica(15, 65, "Presentes", out _lblPresentesValor, Color.FromArgb(46, 160, 67));
            var pnlFaltantes = CrearPanelEstadistica(210, 65, "Faltantes", out _lblFaltantesValor, Color.FromArgb(200, 55, 55));
            var pnlPorcentaje = CrearPanelEstadistica(405, 65, "Porcentaje", out _lblPorcentajeValor, Color.FromArgb(40, 90, 150));

            _dgvDetalle = new DataGridView
            {
                Location = new Point(15, 175),
                Size = new Size(790, 430),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            Controls.Add(lblFecha);
            Controls.Add(_dtpFecha);
            Controls.Add(lblSeccion);
            Controls.Add(_cmbSeccion);
            Controls.Add(btnActualizar);
            Controls.Add(pnlPresentes);
            Controls.Add(pnlFaltantes);
            Controls.Add(pnlPorcentaje);
            Controls.Add(_dgvDetalle);
        }

        private static Panel CrearPanelEstadistica(int x, int y, string titulo, out Label lblValor, Color color)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(180, 95),
                BackColor = color
            };

            var lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(15, 12),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };

            var lblValorLocal = new Label
            {
                Text = "0",
                Location = new Point(15, 35),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 26, FontStyle.Bold)
            };

            panel.Controls.Add(lblTitulo);
            panel.Controls.Add(lblValorLocal);

            lblValor = lblValorLocal;
            return panel;
        }
    }
}
