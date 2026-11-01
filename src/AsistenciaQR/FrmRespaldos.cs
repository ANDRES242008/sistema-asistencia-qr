using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Respaldos de la base de datos (solo Administrador): crear uno
    /// ahora, copiarlo a una USB, restaurar uno de la lista o un .bak
    /// traido de otra parte. Las operaciones corren en segundo plano.
    /// </summary>
    public class FrmRespaldos : Form
    {
        private readonly RespaldoService _respaldoService = new();
        private List<RespaldoInfo> _respaldos = new();

        private Label _lblEstado = null!;
        private DataGridView _dgvRespaldos = null!;
        private Panel _panelBotones = null!;
        private Button _btnCopiar = null!;
        private Button _btnRestaurar = null!;

        public FrmRespaldos()
        {
            ConstruirInterfaz();
            Load += (_, _) => CargarLista();
        }

        private RespaldoInfo? Seleccionado =>
            _dgvRespaldos.CurrentRow?.Index is int i && i >= 0 && i < _respaldos.Count ? _respaldos[i] : null;

        private void CargarLista()
        {
            _respaldos = _respaldoService.Listar();

            _dgvRespaldos.Rows.Clear();
            foreach (var r in _respaldos)
            {
                _dgvRespaldos.Rows.Add(r.Fecha.ToString("dd/MM/yyyy  hh:mm tt"), r.TipoTexto, FormatearTamano(r.TamanoBytes));
            }

            var ultimo = _respaldos.FirstOrDefault();
            if (ultimo is null)
            {
                _lblEstado.Text = "⚠️  Todavia no hay respaldos. Crea uno ahora.";
                _lblEstado.ForeColor = Color.FromArgb(180, 83, 9);
            }
            else
            {
                int dias = (DateTime.Today - ultimo.Fecha.Date).Days;
                _lblEstado.Text = dias switch
                {
                    0 => $"✔  Ultimo respaldo: hoy a las {ultimo.Fecha:hh:mm tt}",
                    1 => "✔  Ultimo respaldo: ayer",
                    _ => $"⚠️  Ultimo respaldo: hace {dias} dias"
                };
                _lblEstado.ForeColor = dias <= 1 ? Colores.VerdeEsmeralda : Color.FromArgb(180, 83, 9);
            }

            bool haySeleccion = _respaldos.Count > 0;
            _btnCopiar.Enabled = haySeleccion;
            _btnRestaurar.Enabled = haySeleccion;
        }

        private async void BtnCrear_Click(object? sender, EventArgs e)
        {
            var creado = await EjecutarOcupado("Creando respaldo...", () => _respaldoService.Crear(TipoRespaldo.Manual));
            if (creado is null) return;

            CargarLista();
            MessageBox.Show(this,
                $"Respaldo creado ({FormatearTamano(creado.TamanoBytes)}).\n\n" +
                "Recomendacion: copialo a una USB de vez en cuando. Si se dana esta computadora, " +
                "los respaldos guardados en ella tambien se pierden.",
                "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCopiar_Click(object? sender, EventArgs e)
        {
            if (Seleccionado is not { } respaldo) return;

            using var dialogo = new SaveFileDialog
            {
                Filter = "Respaldo de SQL Server (*.bak)|*.bak",
                FileName = respaldo.NombreArchivo
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                _respaldoService.CopiarA(respaldo, dialogo.FileName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"No se pudo copiar el respaldo.\n{ex.Message}", "Error al copiar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(this, "Copia guardada.", "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestaurar_Click(object? sender, EventArgs e)
        {
            if (Seleccionado is { } respaldo) Restaurar(respaldo.Ruta, $"del {respaldo.Fecha:dd/MM/yyyy hh:mm tt}");
        }

        private void BtnRestaurarArchivo_Click(object? sender, EventArgs e)
        {
            using var dialogo = new OpenFileDialog
            {
                Filter = "Respaldo de SQL Server (*.bak)|*.bak",
                Title = "Elegir respaldo"
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            Restaurar(dialogo.FileName, $"\"{Path.GetFileName(dialogo.FileName)}\"");
        }

        private async void Restaurar(string ruta, string descripcion)
        {
            var confirmar = MessageBox.Show(this,
                $"Se reemplazaran TODOS los datos actuales por los del respaldo {descripcion}.\n\n" +
                "Antes se guardara automaticamente un respaldo de los datos actuales, por si necesitas volver atras.\n" +
                "Cualquier otra computadora que este usando el sistema se desconectara.\n\nContinuar?",
                "Restaurar respaldo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (confirmar != DialogResult.Yes) return;

            var resultado = await EjecutarOcupado("Restaurando...", () => _respaldoService.Restaurar(ruta));
            if (resultado is null) return;

            if (!resultado.Exitoso)
            {
                MessageBox.Show(this, resultado.Mensaje, "No se restauro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                CargarLista();
                return;
            }

            MessageBox.Show(this, resultado.Mensaje + "\n\nEl programa se reiniciara para cargar los datos restaurados.",
                "Restauracion completa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Program.Reiniciar();
        }

        /// <summary>
        /// Corre la operacion en segundo plano con la ventana bloqueada.
        /// Si falla, muestra el error y devuelve null.
        /// </summary>
        private async Task<T?> EjecutarOcupado<T>(string mensaje, Func<T> operacion) where T : class
        {
            string estadoAnterior = _lblEstado.Text;
            _panelBotones.Enabled = false;
            _dgvRespaldos.Enabled = false;
            UseWaitCursor = true;
            _lblEstado.Text = "⏳  " + mensaje;

            try
            {
                return await Task.Run(operacion);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"No se pudo completar la operacion.\n\n{ex.Message}\n\n" +
                    "Si dice \"acceso denegado\" o \"permiso\", la cuenta de Windows que usa el programa necesita " +
                    "ser administradora de SQL Server.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblEstado.Text = estadoAnterior;
                return null;
            }
            finally
            {
                _panelBotones.Enabled = true;
                _dgvRespaldos.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private static string FormatearTamano(long bytes) =>
            bytes >= 1024 * 1024 ? $"{bytes / 1024.0 / 1024.0:0.0} MB" : $"{Math.Max(bytes / 1024, 1)} KB";

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Respaldos de la base de datos";
            Size = Escala.Ventana(this, 900, 600);
            MinimumSize = Escala.Px(this, 700, 420);
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Colores.GrisClaro;
            Padding = new Padding(Escala.Px(this, 20));

            // Titulo, ayuda y estado apilados: cada uno toma el alto de su texto.
            var panelSuperior = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, Escala.Px(this, 10))
            };

            panelSuperior.Controls.Add(new Label
            {
                Text = "Respaldos",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true
            });

            panelSuperior.Controls.Add(new Label
            {
                Text = $"Respaldo automatico diario (se conservan los ultimos {RespaldoService.AutomaticosAConservar}). " +
                       "Los manuales no se borran solos.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                Margin = new Padding(3, 0, 3, Escala.Px(this, 8))
            });

            _lblEstado = new Label
            {
                Font = new Font("Segoe UI Emoji", 10, FontStyle.Bold),
                AutoSize = true
            };
            panelSuperior.Controls.Add(_lblEstado);

            _dgvRespaldos = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Colores.BlancoPuro,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            _dgvRespaldos.RowTemplate.Height = Escala.Px(this, 30);
            _dgvRespaldos.Columns.Add("Fecha", "Fecha");
            _dgvRespaldos.Columns.Add("Tipo", "Tipo");
            _dgvRespaldos.Columns.Add("Tamano", "Tamano");

            // Botones a la medida de su texto; si no caben, pasan a otra fila.
            _panelBotones = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                WrapContents = true,
                Padding = new Padding(0, Escala.Px(this, 12), 0, 0)
            };

            var btnCrear = new BotonPrimario
            {
                Text = "💾  Crear respaldo ahora",
                Font = new Font("Segoe UI Emoji", 10, FontStyle.Bold),
                AutoSize = true,
                Padding = new Padding(Escala.Px(this, 16), 0, Escala.Px(this, 16), 0),
                MinimumSize = new Size(0, Escala.Px(this, 40))
            };
            btnCrear.Click += BtnCrear_Click;

            _btnCopiar = CrearBoton("📤  Copiar a USB...");
            _btnCopiar.Click += BtnCopiar_Click;

            _btnRestaurar = CrearBoton("↩  Restaurar seleccionado", peligro: true);
            _btnRestaurar.Click += BtnRestaurar_Click;

            var btnRestaurarArchivo = CrearBoton("📂  Restaurar desde archivo...", peligro: true);
            btnRestaurarArchivo.Click += BtnRestaurarArchivo_Click;

            _panelBotones.Controls.AddRange(new Control[] { btnCrear, _btnCopiar, _btnRestaurar, btnRestaurarArchivo });

            Controls.Add(_dgvRespaldos);
            Controls.Add(_panelBotones);
            Controls.Add(panelSuperior);
        }

        private Button CrearBoton(string texto, bool peligro = false)
        {
            var boton = Escala.Boton(texto, peligro, altoDiseno: 40);
            boton.Margin = new Padding(Escala.Px(this, 10), 0, 0, Escala.Px(this, 6));
            return boton;
        }
    }
}
