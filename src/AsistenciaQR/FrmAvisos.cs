using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Avisos a padres por correo (solo Administrador): configuracion del
    /// correo del colegio, hora del envio diario, prueba, envio manual de
    /// los avisos de hoy e historial.
    /// </summary>
    public class FrmAvisos : Form
    {
        private readonly AvisosService _avisosService = new();

        private CheckBox _chkActivo = null!;
        private DateTimePicker _dtpHora = null!;
        private CheckBox _chkTardanzas = null!;
        private CheckBox _chkAusencias = null!;
        private TextBox _txtServidor = null!;
        private NumericUpDown _numPuerto = null!;
        private CheckBox _chkSsl = null!;
        private TextBox _txtUsuario = null!;
        private TextBox _txtContrasena = null!;
        private Label _lblEquipo = null!;
        private TextBox _txtDestinoPrueba = null!;
        private Label _lblPendientes = null!;
        private Button _btnEnviarHoy = null!;
        private DataGridView _dgvHistorial = null!;
        private TableLayoutPanel _panelIzquierdo = null!;

        public FrmAvisos()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarConfiguracion(); CargarHoy(); };
        }

        private void CargarConfiguracion()
        {
            var config = _avisosService.ObtenerConfiguracion();
            _chkActivo.Checked = config.Activo;
            _dtpHora.Value = DateTime.Today.Add(config.HoraEnvio);
            _chkTardanzas.Checked = config.AvisarTardanzas;
            _chkAusencias.Checked = config.AvisarAusencias;
            _txtServidor.Text = config.ServidorSmtp;
            _numPuerto.Value = config.Puerto;
            _chkSsl.Checked = config.UsarSsl;
            _txtUsuario.Text = config.Usuario;
            _txtContrasena.Text = config.Contrasena ?? string.Empty;

            bool otraComputadora = !string.IsNullOrEmpty(config.EquipoQueEnvia) &&
                                   !string.Equals(config.EquipoQueEnvia, AvisosService.EquipoActual, StringComparison.OrdinalIgnoreCase);
            _lblEquipo.Text = otraComputadora
                ? $"⚠️  Hoy envia la computadora \"{config.EquipoQueEnvia}\". Si guardas aqui, pasara a enviar esta ({AvisosService.EquipoActual})."
                : $"💻  Los avisos los envia esta computadora ({AvisosService.EquipoActual}) mientras el programa este abierto.";
            _lblEquipo.ForeColor = otraComputadora ? Color.FromArgb(180, 83, 9) : Colores.GrisSecundario;
        }

        private void CargarHoy()
        {
            var config = LeerFormulario();
            try
            {
                var pendientes = _avisosService.ObtenerPendientes(DateTime.Today, config);
                int tardanzas = pendientes.Count(p => p.Tipo == TipoAviso.Tardanza);
                _lblPendientes.Text = pendientes.Count == 0
                    ? "Hoy no hay avisos pendientes."
                    : $"Pendientes hoy: {pendientes.Count} ({tardanzas} tardanzas, {pendientes.Count - tardanzas} ausencias hasta ahora)";
                _btnEnviarHoy.Enabled = pendientes.Count > 0;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _lblPendientes.Text = "No se pudo consultar: " + ex.Message;
                _btnEnviarHoy.Enabled = false;
            }

            _dgvHistorial.Rows.Clear();
            foreach (var r in _avisosService.ListarHistorial(DateTime.Today.AddDays(-14), DateTime.Today))
            {
                int fila = _dgvHistorial.Rows.Add(r.Fecha.ToString("dd/MM"), r.Estudiante, r.Tipo, r.Correo,
                    r.Enviado ? "✔ Enviado" : "✖ " + r.Error);
                if (!r.Enviado) _dgvHistorial.Rows[fila].DefaultCellStyle.ForeColor = Color.Firebrick;
            }
        }

        private ConfiguracionCorreo LeerFormulario() => new()
        {
            Activo = _chkActivo.Checked,
            HoraEnvio = new TimeSpan(_dtpHora.Value.Hour, _dtpHora.Value.Minute, 0),
            AvisarTardanzas = _chkTardanzas.Checked,
            AvisarAusencias = _chkAusencias.Checked,
            ServidorSmtp = _txtServidor.Text.Trim(),
            Puerto = (int)_numPuerto.Value,
            UsarSsl = _chkSsl.Checked,
            Usuario = _txtUsuario.Text.Trim(),
            Contrasena = _txtContrasena.Text
        };

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            var resultado = _avisosService.GuardarConfiguracion(LeerFormulario());
            MessageBox.Show(this, resultado.Mensaje, resultado.Exitoso ? "Listo" : "Revisa los datos",
                MessageBoxButtons.OK, resultado.Exitoso ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (resultado.Exitoso) { CargarConfiguracion(); CargarHoy(); }
        }

        private async void BtnProbar_Click(object? sender, EventArgs e)
        {
            var config = LeerFormulario();
            string destino = _txtDestinoPrueba.Text;

            var resultado = await EjecutarOcupado(() => _avisosService.Probar(config, destino));
            MessageBox.Show(this, resultado.Mensaje, resultado.Exitoso ? "Prueba enviada" : "La prueba fallo",
                MessageBoxButtons.OK, resultado.Exitoso ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private async void BtnEnviarHoy_Click(object? sender, EventArgs e)
        {
            var confirmar = MessageBox.Show(this,
                "Se enviaran ahora los avisos pendientes de hoy (los ya enviados no se repiten).\n\n" +
                "Nota: los estudiantes que todavia no llegan apareceran como ausentes. Continuar?",
                "Enviar avisos de hoy", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmar != DialogResult.Yes) return;

            var resumen = await EjecutarOcupado(() => _avisosService.EnviarPendientes(DateTime.Today, "Envio manual"));
            CargarHoy();

            string mensaje = resumen.PrimerError is not null && resumen.Enviados + resumen.Fallidos == 0
                ? resumen.PrimerError
                : $"Enviados: {resumen.Enviados}.  Con error: {resumen.Fallidos}." +
                  (resumen.PrimerError is null ? string.Empty : $"\n\nPrimer error: {resumen.PrimerError}");
            MessageBox.Show(this, mensaje, "Avisos de hoy", MessageBoxButtons.OK,
                resumen.Fallidos > 0 || resumen.Enviados == 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private async Task<T> EjecutarOcupado<T>(Func<T> operacion)
        {
            Enabled = false;
            UseWaitCursor = true;
            try
            {
                return await Task.Run(operacion);
            }
            finally
            {
                Enabled = true;
                UseWaitCursor = false;
            }
        }

        private void ConstruirInterfaz()
        {
            int E(int pixeles) => Escala.Px(this, pixeles);

            Text = "AsistenciaQR - Avisos a padres por correo";
            Size = Escala.Ventana(this, 1180, 700);
            MinimumSize = Escala.Px(this, 980, 600);
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Colores.GrisClaro;
            Padding = new Padding(E(20));

            // Izquierda: configuracion. Derecha: avisos de hoy e historial.
            var columnas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            columnas.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, E(500)));
            columnas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _panelIzquierdo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoScroll = true,
                BackColor = Colores.BlancoPuro,
                Padding = new Padding(E(18))
            };
            _panelIzquierdo.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _panelIzquierdo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AgregarFilaCompleta(new Label
            {
                Text = "Avisos a padres",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true
            });
            AgregarFilaCompleta(new Label
            {
                Text = "Una vez al dia, a la hora elegida, se manda un correo al encargado de cada estudiante " +
                       "que llego tarde o que no registro asistencia. Solo se avisa a quienes tienen " +
                       "\"Correo del encargado\" en Estudiantes.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                MaximumSize = new Size(E(450), 0),
                Margin = new Padding(3, 0, 3, E(10))
            });

            _chkActivo = new CheckBox { Text = "Enviar avisos automaticamente (lunes a viernes)", AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            AgregarFilaCompleta(_chkActivo);

            _dtpHora = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = E(130) };
            AgregarFila("Hora del envio:", _dtpHora);

            var tipos = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
            _chkTardanzas = new CheckBox { Text = "Tardanzas", AutoSize = true };
            _chkAusencias = new CheckBox { Text = "Ausencias", AutoSize = true };
            tipos.Controls.AddRange(new Control[] { _chkTardanzas, _chkAusencias });
            AgregarFila("Avisar:", tipos);

            AgregarFilaCompleta(new Label
            {
                Text = "Correo del colegio",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true,
                Margin = new Padding(3, E(14), 3, E(4))
            });

            _txtServidor = new TextBox { Dock = DockStyle.Fill };
            AgregarFila("Servidor SMTP:", _txtServidor);

            var puerto = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
            _numPuerto = new NumericUpDown { Minimum = 1, Maximum = 65535, Width = E(90) };
            _chkSsl = new CheckBox { Text = "Conexion segura (SSL/TLS)", AutoSize = true };
            puerto.Controls.AddRange(new Control[] { _numPuerto, _chkSsl });
            AgregarFila("Puerto:", puerto);

            _txtUsuario = new TextBox { Dock = DockStyle.Fill };
            AgregarFila("Correo (usuario):", _txtUsuario);

            _txtContrasena = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
            AgregarFila("Contrasena:", _txtContrasena);

            AgregarFilaCompleta(new Label
            {
                Text = "Gmail: activa la verificacion en dos pasos y crea una \"contrasena de aplicacion\" " +
                       "en myaccount.google.com/apppasswords. Usa esa, no la contrasena normal. " +
                       "Se guarda cifrada y solo esta computadora puede leerla.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                MaximumSize = new Size(E(450), 0)
            });

            _lblEquipo = new Label { AutoSize = true, MaximumSize = new Size(E(450), 0), Font = new Font("Segoe UI Emoji", 8.5f), Margin = new Padding(3, E(8), 3, E(8)) };
            AgregarFilaCompleta(_lblEquipo);

            var prueba = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty, WrapContents = false };
            _txtDestinoPrueba = new TextBox { Width = E(210), PlaceholderText = "tu-correo@ejemplo.com" };
            var btnProbar = Escala.Boton("✉️  Enviar prueba", altoDiseno: 30);
            btnProbar.Click += BtnProbar_Click;
            prueba.Controls.AddRange(new Control[] { _txtDestinoPrueba, btnProbar });
            AgregarFila("Probar con:", prueba);

            var botones = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, E(14), 0, 0) };
            var btnGuardar = new BotonPrimario
            {
                Text = "Guardar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Padding = new Padding(E(30), 0, E(30), 0),
                MinimumSize = Escala.Px(this, 140, 40)
            };
            btnGuardar.Click += BtnGuardar_Click;
            var btnCerrar = Escala.Boton("Cerrar", altoDiseno: 40);
            btnCerrar.Margin = new Padding(E(10), 0, 0, 0);
            btnCerrar.Click += (_, _) => Close();
            botones.Controls.AddRange(new Control[] { btnGuardar, btnCerrar });
            AgregarFilaCompleta(botones);

            // Columna derecha
            var panelDerecho = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(E(16), 0, 0, 0)
            };
            panelDerecho.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panelDerecho.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panelDerecho.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panelDerecho.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _lblPendientes = new Label { AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Colores.GrisOscuro };
            _btnEnviarHoy = Escala.Boton("📨  Enviar avisos de hoy ahora", altoDiseno: 38);
            _btnEnviarHoy.Margin = new Padding(3, E(8), 3, E(16));
            _btnEnviarHoy.Click += BtnEnviarHoy_Click;

            _dgvHistorial = new DataGridView
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
                Font = new Font("Segoe UI Emoji", 9),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            _dgvHistorial.RowTemplate.Height = E(26);
            _dgvHistorial.Columns.Add("Fecha", "Fecha");
            _dgvHistorial.Columns.Add("Estudiante", "Estudiante");
            _dgvHistorial.Columns.Add("Tipo", "Tipo");
            _dgvHistorial.Columns.Add("Correo", "Correo");
            _dgvHistorial.Columns.Add("Estado", "Estado");
            _dgvHistorial.Columns["Fecha"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            _dgvHistorial.Columns["Tipo"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

            panelDerecho.Controls.Add(_lblPendientes, 0, 0);
            panelDerecho.Controls.Add(_btnEnviarHoy, 0, 1);
            panelDerecho.Controls.Add(new Label
            {
                Text = "Historial (ultimas 2 semanas)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                Margin = new Padding(3, 0, 3, E(6))
            }, 0, 2);
            panelDerecho.Controls.Add(_dgvHistorial, 0, 3);

            columnas.Controls.Add(_panelIzquierdo, 0, 0);
            columnas.Controls.Add(panelDerecho, 1, 0);
            Controls.Add(columnas);
        }

        private void AgregarFila(string etiqueta, Control control)
        {
            int fila = _panelIzquierdo.RowCount++;
            _panelIzquierdo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _panelIzquierdo.Controls.Add(new Label
            {
                Text = etiqueta,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Colores.GrisOscuro
            }, 0, fila);
            control.Margin = new Padding(3, Escala.Px(this, 4), 3, Escala.Px(this, 4));
            _panelIzquierdo.Controls.Add(control, 1, fila);
        }

        private void AgregarFilaCompleta(Control control)
        {
            int fila = _panelIzquierdo.RowCount++;
            _panelIzquierdo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _panelIzquierdo.Controls.Add(control, 0, fila);
            _panelIzquierdo.SetColumnSpan(control, 2);
        }
    }
}
