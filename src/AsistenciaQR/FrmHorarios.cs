using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Servicios;

namespace AsistenciaQR
{
    /// <summary>
    /// Pantalla 9: Configuracion de Horarios y Lapsos de Tolerancia.
    /// Define la hora de inicio de clases y los minutos de tolerancia
    /// que el sistema usa para clasificar cada asistencia como "A
    /// tiempo" o "Tarde" automaticamente. El cambio solo afecta
    /// registros futuros, nunca reclasifica el historico ya guardado.
    /// </summary>
    public class FrmHorarios : Form
    {
        private readonly ConfiguracionHorarioService _horarioService = new();

        private DateTimePicker _dtpHoraInicio = null!;
        private NumericUpDown _numTolerancia = null!;
        private Label _lblLimite = null!;
        private Button _btnGuardar = null!;
        private Label _lblGuardado = null!;

        public FrmHorarios()
        {
            ConstruirInterfaz();
            Load += (_, _) => CargarConfiguracionActual();
        }

        private void CargarConfiguracionActual()
        {
            var config = _horarioService.ObtenerActual();

            _dtpHoraInicio.Value = DateTime.Today.Add(config.HoraInicio);
            _numTolerancia.Value = Math.Min(
                Math.Max(config.MinutosTolerancia, (int)_numTolerancia.Minimum),
                (int)_numTolerancia.Maximum);

            ActualizarLimite();
        }

        private void ActualizarLimite()
        {
            var horaInicio = _dtpHoraInicio.Value.TimeOfDay;
            var limite = horaInicio + TimeSpan.FromMinutes((double)_numTolerancia.Value);

            _lblLimite.Text =
                $"Los estudiantes que marquen despues de las {DateTime.Today.Add(limite):hh:mm tt} " +
                "quedaran registrados como Tardanza.";
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            _horarioService.Guardar(_dtpHoraInicio.Value.TimeOfDay, (int)_numTolerancia.Value);

            _lblGuardado.ForeColor = Color.FromArgb(46, 160, 67);
            _lblGuardado.Text = "Guardado. Los registros anteriores no cambian, solo aplica de ahora en adelante.";

            ActualizarLimite();
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Horarios y Tolerancia";
            Width = 560;
            Height = 400;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            var lblTitulo = new Label
            {
                Text = "Control de Puntualidad",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 13, FontStyle.Bold)
            };

            var lblSubtitulo = new Label
            {
                Text = "El sistema usa esto para marcar automaticamente quien llego a tiempo o tarde.",
                Location = new Point(20, 55),
                Size = new Size(500, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };

            var lblHoraInicio = new Label
            {
                Text = "Hora de inicio de clases:",
                Location = new Point(20, 95),
                AutoSize = true
            };
            _dtpHoraInicio = new DateTimePicker
            {
                Location = new Point(220, 92),
                Size = new Size(120, 28),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(7)
            };
            _dtpHoraInicio.ValueChanged += (_, _) => ActualizarLimite();

            var lblTolerancia = new Label
            {
                Text = "Minutos de tolerancia:",
                Location = new Point(20, 140),
                AutoSize = true
            };
            _numTolerancia = new NumericUpDown
            {
                Location = new Point(220, 137),
                Size = new Size(70, 28),
                Minimum = 0,
                Maximum = 120,
                Value = 10
            };
            _numTolerancia.ValueChanged += (_, _) => ActualizarLimite();

            _lblLimite = new Label
            {
                Text = string.Empty,
                Location = new Point(20, 185),
                Size = new Size(500, 60),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 90, 150)
            };

            _btnGuardar = new Button
            {
                Text = "Guardar configuracion",
                Location = new Point(20, 260),
                Size = new Size(220, 40),
                Font = new Font("Segoe UI", 10)
            };
            _btnGuardar.Click += BtnGuardar_Click;

            _lblGuardado = new Label
            {
                Text = string.Empty,
                Location = new Point(20, 310),
                Size = new Size(500, 40)
            };

            Controls.Add(lblTitulo);
            Controls.Add(lblSubtitulo);
            Controls.Add(lblHoraInicio);
            Controls.Add(_dtpHoraInicio);
            Controls.Add(lblTolerancia);
            Controls.Add(_numTolerancia);
            Controls.Add(_lblLimite);
            Controls.Add(_btnGuardar);
            Controls.Add(_lblGuardado);
        }
    }
}
