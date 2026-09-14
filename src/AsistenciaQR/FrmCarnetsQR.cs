using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;
using DocumentFormat.OpenXml.Math;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace AsistenciaQR
{
 
    public class FrmCarnetsQR : Form
    {
        private readonly EstudianteService _estudianteService = new();
        private readonly QRService _qrService = new();
        private readonly CarnetService _carnetService = new();
        private readonly QRRepository _qrRepository = new();

        private float _escala = 1f;
        private readonly Dictionary<int, Bitmap> _avataresCache = new();
        private Estudiante? _estudianteSeleccionado;

        private Panel _panelContenido = null!;
        private CampoTexto _campoBuscar = null!;
        private DataGridView _dgvEstudiantes = null!;
        private BotonRegresar _btnRegresar = null!;

        private TarjetaBlanca _tarjetaDetalle = null!;
        private Label _lblNombreSeleccionado = null!;
        private Label _lblEstadoQr = null!;
        private Label _lblFechaQr = null!;
        private PictureBox _picQr = null!;
        private BotonAccion _btnRegenerarQr = null!;
        private BotonAccion _btnGenerarCarnet = null!;
        private Label _lblSinSeleccion = null!;

        public FrmCarnetsQR()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarLista(); AplicarEscala(); };
        }

        private void CargarLista()
        {
            var lista = _estudianteService.Buscar(_campoBuscar.ObtenerValorReal(), null, incluirInactivos: false);

            foreach (var bitmap in _avataresCache.Values) bitmap.Dispose();
            _avataresCache.Clear();

            _dgvEstudiantes.DataSource = null;
            _dgvEstudiantes.DataSource = lista;
            ConfigurarColumnasGrid();
            SincronizarAlturaFilas();
        }

        private void ConfigurarColumnasGrid()
        {
            OcultarColumna("EstudianteId");
            OcultarColumna("FotoRuta");
            OcultarColumna("FechaRegistro");
            OcultarColumna("Correo");
            OcultarColumna("Activo");

            if (_dgvEstudiantes.Columns["NombreCompleto"] is { } colNombre) colNombre.HeaderText = "Nombre Completo";
        }

        private void OcultarColumna(string nombre)
        {
            if (_dgvEstudiantes.Columns[nombre] is { } columna) columna.Visible = false;
        }

        private void SincronizarAlturaFilas()
        {
            int alto = (int)(34 * _escala);
            foreach (DataGridViewRow fila in _dgvEstudiantes.Rows)
            {
                fila.Height = alto;
            }
        }

      
        private Bitmap ObtenerAvatar(Estudiante estudiante)
        {
            if (_avataresCache.TryGetValue(estudiante.EstudianteId, out var existente)) return existente;

            const int lado = 30;
            var avatar = new Bitmap(lado, lado);

            using (var g = Graphics.FromImage(avatar))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var area = new Rectangle(0, 0, lado, lado);

                using var pincel = new SolidBrush(Colores.AzulMarino);
                g.FillEllipse(pincel, area);

                string inicial = string.IsNullOrWhiteSpace(estudiante.NombreCompleto)
                    ? "?"
                    : estudiante.NombreCompleto.Trim()[0].ToString().ToUpperInvariant();

                using var fuente = new Font("Segoe UI", Math.Max(lado * 0.4f, 8f), FontStyle.Bold);

                using var formato = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.DrawString(inicial, fuente, Brushes.White, new Rectangle(0, 1, lado, lado), formato);
            }

            _avataresCache[estudiante.EstudianteId] = avatar;
            return avatar;
        }

        private void DgvEstudiantes_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_dgvEstudiantes.Columns[e.ColumnIndex].Name != "NombreCompleto") return;
            if (_dgvEstudiantes.Rows[e.RowIndex].DataBoundItem is not Estudiante estudiante) return;

            e.PaintBackground(e.CellBounds, true);

            var avatar = ObtenerAvatar(estudiante);
            int margenY = (e.CellBounds.Height - avatar.Height) / 2;
            e.Graphics!.DrawImage(avatar, e.CellBounds.X + 6, e.CellBounds.Y + margenY);

            var areaTexto = new Rectangle(
                e.CellBounds.X + avatar.Width + 14, e.CellBounds.Y,
                Math.Max(e.CellBounds.Width - avatar.Width - 18, 10), e.CellBounds.Height);

            TextRenderer.DrawText(e.Graphics, e.Value?.ToString() ?? string.Empty, e.CellStyle!.Font,
                areaTexto, e.CellStyle.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.Handled = true;
        }

        private void DgvEstudiantes_SelectionChanged(object? sender, EventArgs e)
        {
            if (_dgvEstudiantes.CurrentRow?.DataBoundItem is not Estudiante estudiante)
            {
                _estudianteSeleccionado = null;
                MostrarSinSeleccion();
                return;
            }

            _estudianteSeleccionado = estudiante;
            MostrarDetalle(estudiante);
        }

        private void MostrarSinSeleccion()
        {
            _lblSinSeleccion.Visible = true;
            _lblNombreSeleccionado.Visible = false;
            _lblEstadoQr.Visible = false;
            _lblFechaQr.Visible = false;
            _picQr.Visible = false;
            _btnRegenerarQr.Visible = false;
            _btnGenerarCarnet.Visible = false;
        }

        private void MostrarDetalle(Estudiante estudiante)
        {
            _lblSinSeleccion.Visible = false;
            _lblNombreSeleccionado.Visible = true;
            _lblEstadoQr.Visible = true;
            _btnRegenerarQr.Visible = true;
            _btnGenerarCarnet.Visible = true;

            _lblNombreSeleccionado.Text = estudiante.NombreCompleto;

            var qrActivo = _qrRepository.ObtenerQrActivo(estudiante.EstudianteId);

            _picQr.Image?.Dispose();

            if (qrActivo is not null)
            {
                _lblEstadoQr.Text = "✅  QR ACTIVO";
                _lblEstadoQr.ForeColor = Colores.VerdeEsmeralda;

                _lblFechaQr.Text = $"generado el {qrActivo.FechaGeneracion:dd/MM/yyyy}";
                _lblFechaQr.Visible = true;

                if (!string.IsNullOrEmpty(qrActivo.RutaImagen) && File.Exists(qrActivo.RutaImagen))
                {
                    using var flujo = new MemoryStream(File.ReadAllBytes(qrActivo.RutaImagen));
                    _picQr.Image = new Bitmap(flujo);
                    _picQr.Visible = true;
                }
                else
                {
                    _picQr.Image = null;
                    _picQr.Visible = false;
                }

                _btnRegenerarQr.ActualizarTexto("🔄  Regenerar Codigo QR");
                _btnGenerarCarnet.Enabled = true;
            }
            else
            {
                _lblEstadoQr.Text = "⚠️  SIN CODIGO QR";
                _lblEstadoQr.ForeColor = Color.FromArgb(230, 159, 0);

                _lblFechaQr.Visible = false;
                _picQr.Image = null;
                _picQr.Visible = false;

                _btnRegenerarQr.ActualizarTexto("➕  Generar Codigo QR");
                _btnGenerarCarnet.Enabled = false;
            }

            AplicarEscala();
        }

        private void BtnRegenerarQr_Click(object? sender, EventArgs e)
        {
            if (_estudianteSeleccionado is null) return;

            _qrService.GenerarQrParaEstudiante(_estudianteSeleccionado);
            MostrarDetalle(_estudianteSeleccionado);

            MessageBox.Show(this, "QR generado correctamente.", "Listo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnGenerarCarnet_Click(object? sender, EventArgs e)
        {
            if (_estudianteSeleccionado is null) return;

            var qrActivo = _qrRepository.ObtenerQrActivo(_estudianteSeleccionado.EstudianteId);
            if (qrActivo is null)
            {
                MessageBox.Show(this, "Primero genera un QR para este estudiante.", "Falta el QR",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string rutaImagen = _carnetService.GenerarImagenCarnet(_estudianteSeleccionado, qrActivo);
            _carnetService.ExportarCarnetComoPdf(rutaImagen);

            Process.Start(new ProcessStartInfo(rutaImagen) { UseShellExecute = true });
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
            Text = "AsistenciaQR - Carnets y Control de QR";
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

            _campoBuscar = new CampoTexto("🔍", "Buscar por NIE o Nombre...");
            _campoBuscar.TextoCambiado += _ => CargarLista();

            _dgvEstudiantes = new DataGridView
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
            _dgvEstudiantes.CellPainting += DgvEstudiantes_CellPainting;
            _dgvEstudiantes.SelectionChanged += DgvEstudiantes_SelectionChanged;

            _btnRegresar = new BotonRegresar();
            _btnRegresar.Click += BtnRegresar_Click;

            ConstruirTarjetaDetalle();

            _panelContenido.Controls.AddRange(new Control[]
            {
                _campoBuscar, _dgvEstudiantes, _btnRegresar, _tarjetaDetalle
            });

            Controls.Add(_panelContenido);
        }

        private void ConstruirTarjetaDetalle()
        {
            _tarjetaDetalle = new TarjetaBlanca();

            _lblSinSeleccion = new Label
            {
                Text = "Selecciona un estudiante de la lista\npara ver su codigo QR.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Colores.GrisSecundario,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };


            _lblNombreSeleccionado = new Label
            {
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = false,    
                AutoEllipsis = false, 
                Visible = false
            };

            _lblEstadoQr = new Label
            {
                Font = new Font("Segoe UI Emoji", 11, FontStyle.Bold),
                AutoSize = true,
                Visible = false
            };

            _lblFechaQr = new Label
            {
                Font = new Font("Segoe UI", 9),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                Visible = false
            };

            _picQr = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Colores.BlancoPuro,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            _btnRegenerarQr = new BotonAccion
            {
                ColorFondo = Color.FromArgb(232, 212, 178),
                ColorTexto = Color.FromArgb(110, 74, 34),
                Visible = false
            };
            _btnRegenerarQr.Click += BtnRegenerarQr_Click;

            _btnGenerarCarnet = new BotonAccion
            {
                ColorFondo = Color.FromArgb(18, 116, 101),
                ColorTexto = Color.White,
                Visible = false
            };
            _btnGenerarCarnet.ActualizarTexto("🖼️  Generar Carnet (Imagen + PDF)");
            _btnGenerarCarnet.Click += BtnGenerarCarnet_Click;

            _tarjetaDetalle.Controls.AddRange(new Control[]
            {
                _lblSinSeleccion, _lblNombreSeleccionado, _lblEstadoQr, _lblFechaQr,
                _picQr, _btnRegenerarQr, _btnGenerarCarnet
            });

            MostrarSinSeleccion();
        }

        /// <summary>
        /// Recalcula tamanos, fuentes y posiciones de todo. Escala
        /// basada en ancho Y alto (el mas chico manda).
        /// </summary>
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

            _campoBuscar.Width = (int)(320 * _escala);
            _campoBuscar.Location = new Point(x0, y0);
            _campoBuscar.EscalarA(_escala);

            int y1 = _campoBuscar.Bottom + (int)(18 * _escala);

            int espacioColumnas = (int)(20 * _escala);
            int anchoLista = (int)(anchoDisponible * 0.56);
            int anchoDetalle = anchoDisponible - anchoLista - espacioColumnas;

            int alturaRegresar = (int)(42 * _escala);
            int gapExtra = (int)(14 * _escala);
            int alturaGrid = altoDisponible - (y1 - y0) - alturaRegresar - gapExtra;

            _dgvEstudiantes.Font = new Font("Segoe UI", 9.5f * _escala);
            _dgvEstudiantes.ColumnHeadersHeight = (int)(34 * _escala);
            _dgvEstudiantes.RowTemplate.Height = (int)(34 * _escala);
            _dgvEstudiantes.Location = new Point(x0, y1);
            _dgvEstudiantes.Size = new Size(anchoLista, Math.Max(alturaGrid, 120));
            SincronizarAlturaFilas();

            _btnRegresar.Size = new Size((int)(130 * _escala), alturaRegresar);
            _btnRegresar.Location = new Point(x0, _dgvEstudiantes.Bottom + gapExtra);

            _tarjetaDetalle.Location = new Point(x0 + anchoLista + espacioColumnas, y1);
            _tarjetaDetalle.Size = new Size(anchoDetalle, altoDisponible - (y1 - y0));

            int margen = (int)(24 * _escala);
            int anchoInterno = Math.Max(anchoDetalle - margen * 2, 100);

            _lblSinSeleccion.Location = new Point(margen, margen);
            _lblSinSeleccion.Size = new Size(anchoInterno, (int)(80 * _escala));
            _lblSinSeleccion.Font = new Font("Segoe UI", 10 * _escala);


            
            _lblNombreSeleccionado.Font = new Font("Segoe UI", 15 * _escala, FontStyle.Bold);
            _lblNombreSeleccionado.MaximumSize = new Size(anchoInterno, 0);
            _lblNombreSeleccionado.Location = new Point(margen, margen);

            
            int y = _lblNombreSeleccionado.Bottom + (int)(8 * _escala);

            _lblEstadoQr.Font = new Font("Segoe UI Emoji", 11 * _escala, FontStyle.Bold);
            _lblEstadoQr.Location = new Point(margen, y);
            y = _lblEstadoQr.Bottom + (int)(2 * _escala);

            _lblFechaQr.Font = new Font("Segoe UI", 9 * _escala);
            _lblFechaQr.Location = new Point(margen, y);
            y = (_lblFechaQr.Visible ? _lblFechaQr.Bottom : y + (int)(18 * _escala)) + (int)(16 * _escala);

            
            int ladoQr = Math.Min(anchoInterno, (int)(220 * _escala));
            _picQr.Size = new Size(ladoQr, ladoQr);
            _picQr.Location = new Point(margen + (anchoInterno - ladoQr) / 2, y);

        
            int altoBotonCarnet = (int)(50 * _escala);
            int altoBotonQr = (int)(70 * _escala);
            int gapBotones = (int)(20 * _escala);

            int yBotonCarnet = _tarjetaDetalle.Height - margen - altoBotonCarnet;
            _btnGenerarCarnet.Size = new Size(anchoInterno, altoBotonCarnet);
            _btnGenerarCarnet.Location = new Point(margen, yBotonCarnet);

            int yBotonQr = yBotonCarnet - gapBotones - altoBotonQr;
            _btnRegenerarQr.Size = new Size(anchoInterno, altoBotonQr);
            _btnRegenerarQr.Location = new Point(margen, yBotonQr);
        }
    }
}
