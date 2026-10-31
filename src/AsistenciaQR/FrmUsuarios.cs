using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Gestion de cuentas (solo Administrador): crear docentes y
    /// administradores, cambiar rol, desactivar y resetear contrasena.
    /// Las contrasenas iniciales y reseteadas las genera el sistema;
    /// el usuario debe cambiarlas al iniciar sesion.
    /// </summary>
    public class FrmUsuarios : Form
    {
        private readonly AuthService _authService = new();
        private float _escala = 1f;

        private int? _usuarioIdSeleccionado;

        private Panel _panelContenido = null!;
        private BotonRegresar _btnRegresar = null!;

        private CampoTexto _campoBuscar = null!;
        private CheckBox _chkMostrarInactivos = null!;
        private DataGridView _dgvUsuarios = null!;

        private TarjetaBlanca _tarjetaInfo = null!;
        private Label _lblTituloInfo = null!;
        private Label _lblUsuarioEtiqueta = null!;
        private CampoSubrayado _campoUsuario = null!;
        private Label _lblNombreEtiqueta = null!;
        private CampoSubrayado _campoNombre = null!;
        private Label _lblRolEtiqueta = null!;
        private ComboBox _cmbRol = null!;
        private Label _lblAyuda = null!;

        private Button _btnGuardar = null!;
        private Button _btnNuevo = null!;
        private Button _btnResetear = null!;
        private Button _btnDesactivar = null!;
        private Button _btnReactivar = null!;

        public FrmUsuarios()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarLista(); LimpiarFormulario(); AplicarEscala(); };
        }

        private void CargarLista()
        {
            var usuarios = _authService.ListarUsuarios(_campoBuscar.ObtenerValorReal(), _chkMostrarInactivos.Checked);

            _dgvUsuarios.DataSource = null;
            _dgvUsuarios.DataSource = usuarios;
            ConfigurarColumnasGrid();
            SincronizarAlturaFilas();
            _dgvUsuarios.ClearSelection();
        }

        private void ConfigurarColumnasGrid()
        {
            string[] visibles = { "NombreUsuario", "NombreCompleto", "Rol", "Activo" };
            foreach (DataGridViewColumn columna in _dgvUsuarios.Columns)
            {
                columna.Visible = visibles.Contains(columna.Name);
            }

            if (_dgvUsuarios.Columns["NombreUsuario"] is { } colUsuario) colUsuario.HeaderText = "Usuario";
            if (_dgvUsuarios.Columns["NombreCompleto"] is { } colNombre) colNombre.HeaderText = "Nombre";
            if (_dgvUsuarios.Columns["Activo"] is { } colActivo) colActivo.HeaderText = "Estado";
        }

        private void SincronizarAlturaFilas()
        {
            int alto = (int)(28 * _escala);
            foreach (DataGridViewRow fila in _dgvUsuarios.Rows)
            {
                fila.Height = alto;
            }
        }

        private void DgvUsuarios_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_dgvUsuarios.Columns[e.ColumnIndex].Name != "Activo") return;
            if (_dgvUsuarios.Rows[e.RowIndex].DataBoundItem is not Usuario usuario) return;

            e.PaintBackground(e.CellBounds, true);

            var (texto, color) = !usuario.Activo
                ? ("Inactivo", Colores.GrisSecundario)
                : AuthService.EstaBloqueado(usuario, DateTime.Now)
                    ? ("Bloqueado", Color.FromArgb(217, 119, 6))
                    : ("Activo", Colores.VerdeEsmeralda);

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

        private void DgvUsuarios_SelectionChanged(object? sender, EventArgs e)
        {
            if (_dgvUsuarios.CurrentRow?.DataBoundItem is not Usuario usuario) return;

            _usuarioIdSeleccionado = usuario.UsuarioId;

            _campoUsuario.Texto = usuario.NombreUsuario;
            _campoUsuario.Habilitado = false;
            _campoNombre.Texto = usuario.NombreCompleto;
            _cmbRol.SelectedItem = usuario.Rol;
            _lblTituloInfo.Text = "Editar Usuario";
            _lblAyuda.Text = AuthService.EstaBloqueado(usuario, DateTime.Now)
                ? "Cuenta bloqueada por intentos fallidos. Resetear la contrasena la desbloquea."
                : "Si el usuario olvido su contrasena, reseteala y entregale la temporal.";

            _btnResetear.Visible = usuario.Activo;
            _btnDesactivar.Visible = usuario.Activo;
            _btnReactivar.Visible = !usuario.Activo;
        }

        private void LimpiarFormulario()
        {
            _usuarioIdSeleccionado = null;

            _campoUsuario.Texto = string.Empty;
            _campoUsuario.Habilitado = true;
            _campoNombre.Texto = string.Empty;
            _cmbRol.SelectedItem = AuthService.RolDocente;
            _lblTituloInfo.Text = "Nuevo Usuario";
            _lblAyuda.Text = "Al guardar, el sistema genera una contrasena temporal. " +
                             "El usuario debera cambiarla la primera vez que inicie sesion.";

            _btnResetear.Visible = false;
            _btnDesactivar.Visible = false;
            _btnReactivar.Visible = false;

            _dgvUsuarios.ClearSelection();
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            string rol = _cmbRol.SelectedItem?.ToString() ?? string.Empty;

            if (_usuarioIdSeleccionado is int id)
            {
                var resultado = _authService.ActualizarDatos(id, _campoNombre.Texto, rol);
                MostrarResultado(resultado);
                if (!resultado.Exitoso) return;
            }
            else
            {
                string usuario = _campoUsuario.Texto.Trim();
                string temporal = AuthService.GenerarContrasenaTemporal();
                var resultado = _authService.Registrar(usuario, _campoNombre.Texto, temporal, rol, debeCambiarContrasena: true);

                if (!resultado.Exitoso)
                {
                    MostrarResultado(resultado);
                    return;
                }

                MostrarContrasenaTemporal(usuario, temporal, "Usuario creado");
            }

            LimpiarFormulario();
            CargarLista();
        }

        private void BtnResetear_Click(object? sender, EventArgs e)
        {
            if (_usuarioIdSeleccionado is not int id) return;

            var confirmar = MessageBox.Show(this,
                $"Se generara una contrasena temporal para \"{_campoUsuario.Texto}\" y la actual dejara de funcionar. Continuar?",
                "Resetear contrasena", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmar != DialogResult.Yes) return;

            string temporal = AuthService.GenerarContrasenaTemporal();
            var resultado = _authService.ResetearContrasena(id, temporal);
            if (!resultado.Exitoso)
            {
                MostrarResultado(resultado);
                return;
            }

            MostrarContrasenaTemporal(_campoUsuario.Texto, temporal, "Contrasena reseteada");
            CargarLista();
        }

        private void BtnDesactivar_Click(object? sender, EventArgs e)
        {
            if (_usuarioIdSeleccionado is not int id) return;

            var confirmar = MessageBox.Show(this,
                "El usuario ya no podra iniciar sesion. Su informacion se conserva. Continuar?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmar != DialogResult.Yes) return;

            var resultado = _authService.Desactivar(id, SesionActual.NombreUsuario);
            MostrarResultado(resultado);
            if (!resultado.Exitoso) return;

            LimpiarFormulario();
            CargarLista();
        }

        private void BtnReactivar_Click(object? sender, EventArgs e)
        {
            if (_usuarioIdSeleccionado is not int id) return;

            _authService.Reactivar(id);
            LimpiarFormulario();
            CargarLista();
        }

        private void MostrarResultado(ResultadoOperacion resultado)
        {
            MessageBox.Show(this, resultado.Mensaje, resultado.Exitoso ? "Listo" : "Revisa los datos",
                MessageBoxButtons.OK, resultado.Exitoso ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void MostrarContrasenaTemporal(string usuario, string temporal, string titulo)
        {
            // La contrasena se copia al portapapeles para no tener que
            // transcribirla; solo se muestra esta vez.
            try { Clipboard.SetText(temporal); } catch { }

            MessageBox.Show(this,
                $"Usuario: {usuario}\nContrasena temporal: {temporal}\n\n" +
                "Ya esta copiada al portapapeles. Entregala al usuario en persona: " +
                "debera cambiarla al iniciar sesion. No se volvera a mostrar.",
                titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            Text = "AsistenciaQR - Gestion de Usuarios";
            Width = 1150;
            Height = 760;
            MinimumSize = new Size(950, 640);
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

            _btnRegresar = new BotonRegresar();
            _btnRegresar.Click += BtnRegresar_Click;

            _campoBuscar = new CampoTexto("🔍", "Buscar usuario/nombre");
            _campoBuscar.TextoCambiado += _ => CargarLista();

            _chkMostrarInactivos = new CheckBox
            {
                Text = "Mostrar inactivos",
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };
            _chkMostrarInactivos.CheckedChanged += (_, _) => CargarLista();

            _dgvUsuarios = new DataGridView
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
            _dgvUsuarios.CellPainting += DgvUsuarios_CellPainting;
            _dgvUsuarios.CellClick += (_, e) => { if (e.RowIndex >= 0) DgvUsuarios_SelectionChanged(null, EventArgs.Empty); };

            ConstruirTarjetaInfo();

            _panelContenido.Controls.AddRange(new Control[]
            {
                _btnRegresar, _campoBuscar, _chkMostrarInactivos, _dgvUsuarios, _tarjetaInfo
            });

            Controls.Add(_panelContenido);
        }

        private void ConstruirTarjetaInfo()
        {
            _tarjetaInfo = new TarjetaBlanca();

            _lblTituloInfo = new Label
            {
                Text = "Nuevo Usuario",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            _lblUsuarioEtiqueta = CrearEtiqueta("Usuario (para iniciar sesion):");
            _campoUsuario = new CampoSubrayado();

            _lblNombreEtiqueta = CrearEtiqueta("Nombre completo:");
            _campoNombre = new CampoSubrayado();

            _lblRolEtiqueta = CrearEtiqueta("Rol:");
            _cmbRol = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            _cmbRol.Items.AddRange(new object[] { AuthService.RolDocente, AuthService.RolAdministrador });

            _lblAyuda = new Label
            {
                ForeColor = Colores.GrisSecundario,
                AutoSize = true
            };

            _btnGuardar = new BotonPrimario { Text = "Guardar" };
            _btnGuardar.Click += BtnGuardar_Click;

            _btnNuevo = CrearBotonSecundario("Nuevo");
            _btnNuevo.Click += (_, _) => LimpiarFormulario();

            _btnResetear = CrearBotonSecundario("🔑  Resetear contrasena");
            _btnResetear.Click += BtnResetear_Click;

            _btnDesactivar = CrearBotonPeligro("Desactivar");
            _btnDesactivar.Click += BtnDesactivar_Click;

            _btnReactivar = CrearBotonExito("Reactivar");
            _btnReactivar.Click += BtnReactivar_Click;

            _tarjetaInfo.Controls.AddRange(new Control[]
            {
                _lblTituloInfo,
                _lblUsuarioEtiqueta, _campoUsuario,
                _lblNombreEtiqueta, _campoNombre,
                _lblRolEtiqueta, _cmbRol, _lblAyuda,
                _btnGuardar, _btnNuevo, _btnResetear, _btnDesactivar, _btnReactivar
            });
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

        private static Button CrearBotonPeligro(string texto)
        {
            var boton = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 240, 240),
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            boton.FlatAppearance.BorderColor = Color.FromArgb(240, 200, 200);
            return boton;
        }

        private static Button CrearBotonExito(string texto)
        {
            var boton = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 250, 240),
                ForeColor = Colores.VerdeEsmeralda,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            boton.FlatAppearance.BorderColor = Color.FromArgb(180, 230, 200);
            return boton;
        }

        private void AplicarEscala()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            float escalaAncho = ClientSize.Width / 1150f;
            float escalaAlto = ClientSize.Height / 720f;
            _escala = Math.Clamp(Math.Min(escalaAncho, escalaAlto), 1f, 1.6f);

            int anchoDisponible = _panelContenido.ClientSize.Width - _panelContenido.Padding.Horizontal;
            int altoDisponible = _panelContenido.ClientSize.Height - _panelContenido.Padding.Vertical;
            if (anchoDisponible <= 0 || altoDisponible <= 0) return;

            int x0 = _panelContenido.Padding.Left;
            int y0 = _panelContenido.Padding.Top;

            _campoBuscar.Width = (int)(260 * _escala);
            _campoBuscar.Location = new Point(x0, y0);
            _campoBuscar.EscalarA(_escala);

            _chkMostrarInactivos.Font = new Font("Segoe UI", 9.5f * _escala);
            _chkMostrarInactivos.Location = new Point(_campoBuscar.Right + (int)(16 * _escala), y0 + (int)(12 * _escala));

            int y1 = _campoBuscar.Bottom + (int)(20 * _escala);

            int espacioColumnas = (int)(20 * _escala);
            int anchoLista = (int)(anchoDisponible * 0.55);
            int anchoInfo = anchoDisponible - anchoLista - espacioColumnas;

            int alturaRegresar = (int)(42 * _escala);
            int gapExtra = (int)(14 * _escala);
            int alturaGrid = altoDisponible - (y1 - y0) - alturaRegresar - gapExtra;

            _dgvUsuarios.Font = new Font("Segoe UI", 9 * _escala);
            _dgvUsuarios.ColumnHeadersHeight = (int)(32 * _escala);
            _dgvUsuarios.RowTemplate.Height = (int)(28 * _escala);
            _dgvUsuarios.Location = new Point(x0, y1);
            _dgvUsuarios.Size = new Size(anchoLista, Math.Max(alturaGrid, 100));
            SincronizarAlturaFilas();

            _btnRegresar.Size = new Size((int)(160 * _escala), alturaRegresar);
            _btnRegresar.Location = new Point(x0, _dgvUsuarios.Bottom + gapExtra);

            _tarjetaInfo.Location = new Point(x0 + anchoLista + espacioColumnas, y1);
            _tarjetaInfo.Size = new Size(anchoInfo, altoDisponible - (y1 - y0));

            int margenInfo = (int)(22 * _escala);
            int yInfo = margenInfo;
            int anchoCampo = Math.Max(anchoInfo - margenInfo * 2, 100);

            _lblTituloInfo.Font = new Font("Segoe UI", 13 * _escala, FontStyle.Bold);
            _lblTituloInfo.Location = new Point(margenInfo, yInfo);
            yInfo = _lblTituloInfo.Bottom + (int)(14 * _escala);

            void ColocarEtiqueta(Label etiqueta)
            {
                etiqueta.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
                etiqueta.Location = new Point(margenInfo, yInfo);
                yInfo = etiqueta.Bottom + (int)(4 * _escala);
            }

            void ColocarCampo(Label etiqueta, CampoSubrayado campo)
            {
                ColocarEtiqueta(etiqueta);
                campo.Width = anchoCampo;
                campo.Location = new Point(margenInfo, yInfo);
                campo.EscalarA(_escala);
                yInfo = campo.Bottom + (int)(12 * _escala);
            }

            ColocarCampo(_lblUsuarioEtiqueta, _campoUsuario);
            ColocarCampo(_lblNombreEtiqueta, _campoNombre);

            ColocarEtiqueta(_lblRolEtiqueta);
            _cmbRol.Font = new Font("Segoe UI", 9.5f * _escala);
            _cmbRol.Width = anchoCampo;
            _cmbRol.Location = new Point(margenInfo, yInfo);
            yInfo = _cmbRol.Bottom + (int)(14 * _escala);

            _lblAyuda.Font = new Font("Segoe UI", 8.5f * _escala);
            _lblAyuda.MaximumSize = new Size(anchoCampo, 0);
            _lblAyuda.Location = new Point(margenInfo, yInfo);
            yInfo = _lblAyuda.Bottom + (int)(18 * _escala);

            int altoBoton = (int)(36 * _escala);
            int anchoBotonDoble = (anchoCampo - (int)(16 * _escala)) / 2;
            _btnGuardar.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _btnGuardar.Size = new Size(anchoBotonDoble, altoBoton);
            _btnGuardar.Location = new Point(margenInfo, yInfo);

            _btnNuevo.Font = new Font("Segoe UI", 9.5f * _escala);
            _btnNuevo.Size = new Size(anchoBotonDoble, altoBoton);
            _btnNuevo.Location = new Point(margenInfo + anchoBotonDoble + (int)(16 * _escala), yInfo);
            yInfo = _btnGuardar.Bottom + (int)(10 * _escala);

            _btnResetear.Font = new Font("Segoe UI Emoji", 9.5f * _escala);
            _btnResetear.Size = new Size(anchoCampo, altoBoton);
            _btnResetear.Location = new Point(margenInfo, yInfo);
            yInfo = _btnResetear.Bottom + (int)(10 * _escala);

            _btnDesactivar.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _btnDesactivar.Size = new Size(anchoCampo, altoBoton);
            _btnDesactivar.Location = new Point(margenInfo, yInfo);

            _btnReactivar.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _btnReactivar.Size = new Size(anchoCampo, altoBoton);
            _btnReactivar.Location = new Point(margenInfo, yInfo);
        }
    }
}
