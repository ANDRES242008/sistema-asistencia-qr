using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Aparece cuando el programa no puede usar la base de datos (por
    /// ejemplo, recien instalado). Encuentra SQL Server, crea la base y
    /// guarda la conexion. Devuelve OK cuando todo quedo listo.
    /// </summary>
    public class FrmConfigurarBaseDatos : Form
    {
        private readonly string? _detalleError;

        private ComboBox _cmbServidor = null!;
        private CheckBox _chkTodos = null!;
        private Label _lblEstado = null!;
        private Button _btnPreparar = null!;
        private TableLayoutPanel _panel = null!;

        public FrmConfigurarBaseDatos(string? detalleError)
        {
            _detalleError = detalleError;
            ConstruirInterfaz();
            Load += (_, _) => CargarInstancias();
        }

        private void CargarInstancias()
        {
            var instancias = BaseDatosService.DetectarInstancias();
            _cmbServidor.Items.Clear();
            _cmbServidor.Items.AddRange(instancias.Cast<object>().ToArray());

            if (instancias.Count > 0)
            {
                _cmbServidor.SelectedIndex = 0;
                MostrarEstado($"Se encontro SQL Server en esta computadora ({instancias.Count} instancia(s)).", Colores.GrisSecundario);
            }
            else
            {
                _cmbServidor.Text = @".\SQLEXPRESS";
                MostrarEstado("No se encontro SQL Server instalado. Descarga SQL Server Express (enlace de abajo), " +
                              "instalalo y vuelve a abrir el programa. Si esta en otra computadora, escribe su nombre.",
                    Color.FromArgb(180, 83, 9));
            }
        }

        private async void BtnPreparar_Click(object? sender, EventArgs e)
        {
            string servidor = _cmbServidor.Text;
            bool todos = _chkTodos.Checked;

            _btnPreparar.Enabled = false;
            UseWaitCursor = true;
            MostrarEstado("⏳  Conectando y preparando la base de datos...", Colores.GrisSecundario);

            var resultado = await Task.Run(() => BaseDatosService.Preparar(servidor, todos));

            UseWaitCursor = false;
            _btnPreparar.Enabled = true;

            if (!resultado.Exitoso)
            {
                MostrarEstado("✖  " + resultado.Mensaje, Color.Firebrick);
                return;
            }

            MostrarEstado("✔  " + resultado.Mensaje, Colores.VerdeEsmeralda);
            MessageBox.Show(this, resultado.Mensaje, "Base de datos lista", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void MostrarEstado(string texto, Color color)
        {
            _lblEstado.Text = texto;
            _lblEstado.ForeColor = color;
        }

        private void ConstruirInterfaz()
        {
            int E(int pixeles) => Escala.Px(this, pixeles);

            Text = "AsistenciaQR - Configurar base de datos";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Colores.BlancoPuro;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(E(28));

            _panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, MaximumSize = new Size(E(560), 0) };
            int ancho = E(540);

            Agregar(new Label
            {
                Text = "Configurar base de datos",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true
            });

            Agregar(new Label
            {
                Text = "AsistenciaQR guarda sus datos en SQL Server. Elige el servidor y el programa creara " +
                       "la base de datos con todas sus tablas. Solo se hace una vez por computadora.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true,
                MaximumSize = new Size(ancho, 0),
                Margin = new Padding(3, E(6), 3, E(8))
            });

            if (!string.IsNullOrWhiteSpace(_detalleError))
            {
                Agregar(new Label
                {
                    Text = "Motivo: " + _detalleError,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Colores.GrisSecundario,
                    AutoSize = true,
                    MaximumSize = new Size(ancho, 0),
                    Margin = new Padding(3, 0, 3, E(12))
                });
            }

            Agregar(new Label { Text = "Servidor de SQL Server:", AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
            _cmbServidor = new ComboBox { Width = ancho, Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDown };
            Agregar(_cmbServidor);

            _chkTodos = new CheckBox
            {
                Text = "Permitir que las demas cuentas de Windows usen el programa",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(3, E(12), 3, 0)
            };
            Agregar(_chkTodos);

            Agregar(new Label
            {
                Text = "Recomendado si el kiosco o los docentes entran a Windows con otra cuenta. " +
                       "Podran registrar y consultar asistencia; respaldar y restaurar sigue siendo solo para quien instalo SQL Server.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                MaximumSize = new Size(ancho, 0),
                Margin = new Padding(E(22), 0, 3, E(4))
            });

            var enlace = new LinkLabel { Text = "¿No tienes SQL Server? Descarga SQL Server Express (gratis)", AutoSize = true, Margin = new Padding(3, E(8), 3, E(8)) };
            enlace.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(BaseDatosService.UrlDescargaSqlExpress) { UseShellExecute = true });
            Agregar(enlace);

            _lblEstado = new Label { AutoSize = true, MaximumSize = new Size(ancho, 0), Font = new Font("Segoe UI Emoji", 9.5f), Margin = new Padding(3, E(4), 3, E(12)) };
            Agregar(_lblEstado);

            var botones = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
            _btnPreparar = new BotonPrimario
            {
                Text = "Conectar y preparar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Padding = new Padding(E(24), 0, E(24), 0),
                MinimumSize = Escala.Px(this, 200, 42)
            };
            _btnPreparar.Click += BtnPreparar_Click;
            AcceptButton = _btnPreparar;

            var btnSalir = Escala.Boton("Salir", altoDiseno: 42);
            btnSalir.Margin = new Padding(E(10), 0, 0, 0);
            btnSalir.DialogResult = DialogResult.Cancel;
            CancelButton = btnSalir;

            botones.Controls.AddRange(new Control[] { _btnPreparar, btnSalir });
            Agregar(botones);

            Controls.Add(_panel);
        }

        private void Agregar(Control control)
        {
            _panel.RowCount++;
            _panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _panel.Controls.Add(control, 0, _panel.RowCount - 1);
        }
    }
}
