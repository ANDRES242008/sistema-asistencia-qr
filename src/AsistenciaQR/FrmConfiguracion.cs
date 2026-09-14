using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{

    public class FrmConfiguracion : Form
    {
        private const int CooldownMinimo = 1;
        private const int CooldownMaximo = 30;

        private const float AnchoBase = 1120f;
        private const float AltoBase = 880f;
        private const float EscalaMinima = 0.9f;
        private const float EscalaMaxima = 1.6f;

        private static readonly Color ColorPillGrisFondo = Color.FromArgb(241, 245, 249);
        private static readonly Color ColorPillAmbarFondo = Color.FromArgb(254, 243, 199);
        private static readonly Color ColorPillAmbarTexto = Color.FromArgb(180, 120, 10);
        private static readonly Color ColorPillVerdeFondo = Color.FromArgb(209, 250, 229);
        private static readonly Color ColorPillRojoFondo = Color.FromArgb(254, 226, 226);

        private readonly ConfiguracionService _configuracionService = new();
        private CamaraQRService? _camaraPrueba;
        private int _cooldownActual = 4;
        private float _escala = 1f;

        private Panel _panelContenido = null!;

        private TarjetaBlanca _tarjetaHardware = null!;
        private Panel _pnlTituloHardware = null!;
        private Label _lblTituloHardware = null!;
        private Panel _panelTip = null!;
        private Label _lblTip = null!;
        private Label _lblFuenteVideo = null!;
        private ComboBox _cmbCamara = null!;
        private Button _btnProbarCamara = null!;
        private Button _btnDetenerPrueba = null!;
        private Label _lblCamaraUsbTitulo = null!;
        private Panel _pnlPreviewContainer = null!;
        private Panel _pnlEstadoCamara = null!;
        private Label _lblEstadoCamara = null!;
        private PictureBox _picPrueba = null!;

        private Label _lblTituloParametros = null!;
        private Panel _pnlAcentoParametros = null!;

        private TarjetaBlanca _tarjetaCooldown = null!;
        private Label _lblTituloCooldown = null!;
        private Button _btnMenos = null!;
        private Label _lblCooldownValor = null!;
        private Button _btnMas = null!;
        private Label _lblCooldownCaption = null!;

        private TarjetaBlanca _tarjetaSql = null!;
        private Label _lblTituloSql = null!;
        private Button _btnProbarSql = null!;
        private Panel _pnlResultadoSql = null!;
        private Label _lblResultadoSql = null!;

        private Panel _pnlAccionesFinal = null!;
        private Panel _pnlGuardado = null!;
        private Label _lblGuardado = null!;
        private BotonRegresar _btnRegresar = null!;
        private BotonPrimario _btnGuardar = null!;

        public FrmConfiguracion()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarConfiguracionActual(); AplicarEscala(); };
            FormClosing += (_, _) => DetenerPruebaCamara();
        }

        private void CargarConfiguracionActual()
        {
            var config = _configuracionService.Cargar();
            _cmbCamara.SelectedIndex = Math.Min(Math.Max(config.IndiceCamara, 0), _cmbCamara.Items.Count - 1);

            _cooldownActual = Math.Clamp(config.SegundosCooldown, CooldownMinimo, CooldownMaximo);
            ActualizarLabelCooldown();
        }

        private void ActualizarLabelCooldown() => _lblCooldownValor.Text = _cooldownActual.ToString();

        private void BtnMenos_Click(object? sender, EventArgs e)
        {
            _cooldownActual = Math.Max(_cooldownActual - 1, CooldownMinimo);
            ActualizarLabelCooldown();
        }

        private void BtnMas_Click(object? sender, EventArgs e)
        {
            _cooldownActual = Math.Min(_cooldownActual + 1, CooldownMaximo);
            ActualizarLabelCooldown();
        }

        private void BtnProbarCamara_Click(object? sender, EventArgs e)
        {
            DetenerPruebaCamara();
            EstablecerBanner(_pnlEstadoCamara, _lblEstadoCamara, "●  Conectando...", ColorPillAmbarTexto, ColorPillAmbarFondo);

            _camaraPrueba = new CamaraQRService();
            _camaraPrueba.FrameCapturado += OnFrameDePrueba;

            try
            {
                _camaraPrueba.Iniciar(_cmbCamara.SelectedIndex);
                _btnDetenerPrueba.Enabled = true;
            }
            catch (Exception ex)
            {
                _camaraPrueba.Dispose();
                _camaraPrueba = null;

                EstablecerBanner(_pnlEstadoCamara, _lblEstadoCamara, "●  No se pudo abrir esta camara.", Color.Firebrick, ColorPillRojoFondo);
                MessageBox.Show(this, ex.Message, "Error de camara",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnFrameDePrueba(Bitmap frame)
        {
            if (IsDisposed) return;

            BeginInvoke(() =>
            {
                var anterior = _picPrueba.Image;
                _picPrueba.Image = frame;
                anterior?.Dispose();

                EstablecerBanner(_pnlEstadoCamara, _lblEstadoCamara,
                    $"●  Camara activa - {frame.Width}x{frame.Height}", Colores.VerdeEsmeralda, ColorPillVerdeFondo);
            });
        }

        private void BtnDetenerPrueba_Click(object? sender, EventArgs e) => DetenerPruebaCamara();

        private void DetenerPruebaCamara()
        {
            if (_camaraPrueba is null) return;

            _camaraPrueba.Detener();
            _camaraPrueba.Dispose();
            _camaraPrueba = null;

            _picPrueba.Image?.Dispose();
            _picPrueba.Image = null;

            EstablecerBanner(_pnlEstadoCamara, _lblEstadoCamara, "○  Sin senal", Colores.GrisSecundario, ColorPillGrisFondo);
            _btnDetenerPrueba.Enabled = false;
        }

        private void BtnProbarSql_Click(object? sender, EventArgs e)
        {
            bool exito = _configuracionService.ProbarConexionSql(out string mensaje);
            EstablecerBanner(_pnlResultadoSql, _lblResultadoSql, $"●  {mensaje}",
                exito ? Colores.VerdeEsmeralda : Color.Firebrick,
                exito ? ColorPillVerdeFondo : ColorPillRojoFondo);
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            var config = new ConfiguracionSistema
            {
                IndiceCamara = _cmbCamara.SelectedIndex,
                SegundosCooldown = _cooldownActual
            };

            _configuracionService.Guardar(config);

            EstablecerBanner(_pnlGuardado, _lblGuardado,
                "✓  Configuracion guardada. Se aplica la proxima vez que abras el Kiosco.",
                Colores.VerdeEsmeralda, ColorPillVerdeFondo);
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
            Text = "AsistenciaQR - Configuracion del Sistema";
            Width = (int)AnchoBase;
            Height = (int)AltoBase;
            MinimumSize = new Size(1040, 820);
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

            ConstruirTarjetaHardware();
            ConstruirSeccionParametros();
            ConstruirBarraAcciones();

            _panelContenido.Controls.AddRange(new Control[]
            {
                _tarjetaHardware, _pnlAcentoParametros, _lblTituloParametros,
                _tarjetaCooldown, _tarjetaSql, _pnlAccionesFinal
            });

            Controls.Add(_panelContenido);
        }

        private void ConstruirTarjetaHardware()
        {
            _tarjetaHardware = new TarjetaBlanca();

            // Cinta solida azul marino: la seccion 1 (Hardware) es la
            // principal. Lleva una linea mas oscura abajo para dar
            // sensacion de relieve, no solo un rectangulo plano.
            _pnlTituloHardware = new Panel { BackColor = Colores.AzulMarino };
            _lblTituloHardware = new Label
            {
                Text = "1.   Configuracion de Hardware: Webcam USB",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Colores.BlancoPuro,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Dock = DockStyle.Fill
            };
            _pnlTituloHardware.Controls.Add(_lblTituloHardware);
            _pnlTituloHardware.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var sombra = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
                e.Graphics.FillRectangle(sombra, new Rectangle(0, _pnlTituloHardware.Height - 3, _pnlTituloHardware.Width, 3));
            };

            _panelTip = new Panel { BackColor = Colores.AzulInformativo };
            _lblTip = new Label
            {
                Text = "ℹ️  Tip: la camara integrada suele ser la 0. Las camaras USB externas suelen ser 1, 2, etc.",
                Font = new Font("Segoe UI Emoji", 9),
                ForeColor = Colores.GrisOscuro,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            _panelTip.Controls.Add(_lblTip);
            _panelTip.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var area = new Rectangle(0, 0, _panelTip.Width - 1, _panelTip.Height - 1);
                using var ruta = Formas.Redondeada(area, 10);
                using var borde = new Pen(Color.FromArgb(186, 224, 250), 1);
                e.Graphics.DrawPath(borde, ruta);
            };

            _lblFuenteVideo = CrearEtiqueta("Fuente de Video (Webcam):");

            _cmbCamara = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            _cmbCamara.Items.AddRange(new object[] { "Camara 0", "Camara 1", "Camara 2", "Camara 3", "Camara 4" });
            _cmbCamara.SelectedIndex = 0;

            // Probar Camara y Probar Conexion SQL comparten la MISMA
            // clase de boton (degradado azul marino): misma familia
            // visual que Guardar (degradado), solo cambia el color.
            _btnProbarCamara = CrearBotonPildora("▷  Probar Camara", Colores.AzulMarino);
            _btnProbarCamara.Click += BtnProbarCamara_Click;

            _btnDetenerPrueba = CrearBotonPildora("⏹  Detener");
            _btnDetenerPrueba.Enabled = false;
            _btnDetenerPrueba.Click += BtnDetenerPrueba_Click;

            _lblCamaraUsbTitulo = CrearEtiqueta("Vista Previa en Vivo:");

            (_pnlEstadoCamara, _lblEstadoCamara) = CrearBanner("○  Sin senal");
            _pnlEstadoCamara.Dock = DockStyle.Top;

            _picPrueba = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill
            };

            _pnlPreviewContainer = new Panel { BackColor = Color.Black };
            // Fill primero, Top al final (regla de Dock compartido).
            _pnlPreviewContainer.Controls.Add(_picPrueba);
            _pnlPreviewContainer.Controls.Add(_pnlEstadoCamara);

            _tarjetaHardware.Controls.AddRange(new Control[]
            {
                _pnlTituloHardware, _panelTip,
                _lblFuenteVideo, _cmbCamara, _btnProbarCamara, _btnDetenerPrueba,
                _lblCamaraUsbTitulo, _pnlPreviewContainer
            });
        }

        private void ConstruirSeccionParametros()
        {
            _lblTituloParametros = new Label
            {
                Text = "2.   Parametros de Software y Datos",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            // Barra de acento debajo del titulo: le da estructura de
            // "encabezado real" aunque no lleve fondo de color solido.
            _pnlAcentoParametros = new Panel { BackColor = Colores.AzulMarino };

            _tarjetaCooldown = new TarjetaBlanca();
            _lblTituloCooldown = new Label
            {
                Text = "Control de Lecturas (Cooldown)",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            _btnMenos = CrearBotonPildora("−");
            _btnMenos.Click += BtnMenos_Click;

            _lblCooldownValor = new Label
            {
                Text = "4",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            _btnMas = CrearBotonPildora("+");
            _btnMas.Click += BtnMas_Click;

            _lblCooldownCaption = new Label
            {
                Text = "Tiempo de espera (segundos)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true
            };

            _tarjetaCooldown.Controls.AddRange(new Control[]
            {
                _lblTituloCooldown, _btnMenos, _lblCooldownValor, _btnMas, _lblCooldownCaption
            });

            _tarjetaSql = new TarjetaBlanca();
            _lblTituloSql = new Label
            {
                Text = "Conexion a la Base de Datos (SQL Server)",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true
            };

            _btnProbarSql = CrearBotonPildora("🖥️  Probar Conexion", Colores.AzulMarino);
            _btnProbarSql.Click += BtnProbarSql_Click;

            (_pnlResultadoSql, _lblResultadoSql) = CrearBanner("○  Aun no probada");

            _tarjetaSql.Controls.AddRange(new Control[] { _lblTituloSql, _btnProbarSql, _pnlResultadoSql });
        }

        private void ConstruirBarraAcciones()
        {
            _pnlAccionesFinal = new Panel { BackColor = Colores.BlancoPuro };
            _pnlAccionesFinal.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var area = new Rectangle(0, 0, _pnlAccionesFinal.Width - 1, _pnlAccionesFinal.Height - 1);
                using var ruta = Formas.Redondeada(area, 12);
                using var borde = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawPath(borde, ruta);
            };

            // Aviso a la izquierda, en el mismo formato "banner" que
            // camara/SQL: consistencia total entre los 3 indicadores.
            (_pnlGuardado, _lblGuardado) = CrearBanner("ℹ️  Los cambios se aplican al guardar.");

            // Regresar y Guardar quedan JUNTOS, pegados entre si, los
            // dos alineados a la derecha del mismo bloque.
            _btnRegresar = new BotonRegresar();
            _btnRegresar.Click += BtnRegresar_Click;

            _btnGuardar = new BotonPrimario { Text = "💾  Guardar Configuracion" };
            _btnGuardar.Click += BtnGuardar_Click;

            _pnlAccionesFinal.Controls.AddRange(new Control[] { _pnlGuardado, _btnRegresar, _btnGuardar });
        }

        private static Label CrearEtiqueta(string texto) => new()
        {
            Text = texto,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Colores.GrisOscuro,
            AutoSize = true
        };

        /// <summary>
        /// Boton pildora. Sin color -> fantasma/outline (accion
        /// secundaria: Detener, +/-). Con color -> degradado solido
        /// (Probar Camara, Probar Conexion), misma familia visual que
        /// BotonPrimario (Guardar), solo cambia el tono.
        /// </summary>
        private static Button CrearBotonPildora(string texto, Color? colorSolido = null)
        {
            if (colorSolido.HasValue)
            {
                return new BotonAccionSecundaria
                {
                    Text = texto,
                    ColorBase = colorSolido.Value,
                    Font = new Font("Segoe UI Emoji", 9.5f, FontStyle.Bold)
                };
            }

            var boton = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                BackColor = Colores.BlancoPuro,
                ForeColor = Colores.GrisOscuro,
                Font = new Font("Segoe UI Emoji", 9.5f),
                Cursor = Cursors.Hand
            };
            boton.FlatAppearance.BorderSize = 1;
            boton.FlatAppearance.BorderColor = Color.FromArgb(219, 224, 230);
            return boton;
        }

        /// <summary>
        /// Boton con degradado horizontal + esquinas redondeadas.
        /// Usado para acciones "de prueba" (secundarias pero visibles),
        /// para que compartan familia visual con BotonPrimario y no
        /// choquen por ser planas contra un boton con degradado.
        /// </summary>
        private sealed class BotonAccionSecundaria : Button
        {
            private bool _hover;
            private bool _presionado;

            public Color ColorBase { get; set; } = Colores.AzulMarino;

            public BotonAccionSecundaria()
            {
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                ForeColor = Colores.BlancoPuro;
                Cursor = Cursors.Hand;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

                MouseEnter += (_, _) => { _hover = true; Invalidate(); };
                MouseLeave += (_, _) => { _hover = false; _presionado = false; Invalidate(); };
                MouseDown += (_, _) => { _presionado = true; Invalidate(); };
                MouseUp += (_, _) => { _presionado = false; Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs pevent)
            {
                var g = pevent.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var area = new Rectangle(0, 0, Width, Height);
                if (area.Width <= 0 || area.Height <= 0) return;

                float intensidad = _presionado ? 0.05f : (_hover ? 0.28f : 0.14f);
                var colorClaro = ControlPaint.Light(ColorBase, intensidad);
                var colorOscuro = _presionado ? ControlPaint.Dark(ColorBase, 0.05f) : ColorBase;

                using var ruta = Formas.Redondeada(area, Height / 2);
                using (var degradado = new LinearGradientBrush(area, colorClaro, colorOscuro, LinearGradientMode.Horizontal))
                {
                    g.FillPath(degradado, ruta);
                }

                TextRenderer.DrawText(g, Text, Font, area, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        /// <summary>
        /// Franja de estado reutilizada por camara, SQL y guardado:
        /// un solo lenguaje visual para los 3 avisos de la pantalla.
        /// Fondo tenue + franja de acento saturada a la izquierda.
        /// </summary>
        private static (Panel panel, Label lbl) CrearBanner(string textoInicial)
        {
            var panel = new Panel { BackColor = ColorPillGrisFondo, Tag = Colores.GrisSecundario };
            var lbl = new Label
            {
                Text = textoInicial,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Colores.GrisSecundario,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 8, 0),
                Dock = DockStyle.Fill
            };
            panel.Controls.Add(lbl);
            panel.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var acento = panel.Tag is Color c ? c : Colores.GrisSecundario;
                int anchoFranja = Math.Max(panel.Height / 8, 4);
                using var brocha = new SolidBrush(acento);
                e.Graphics.FillRectangle(brocha, new Rectangle(0, 0, anchoFranja, panel.Height));
            };
            return (panel, lbl);
        }

        private static void EstablecerBanner(Panel panel, Label lbl, string texto, Color colorTexto, Color colorFondo)
        {
            lbl.Text = texto;
            lbl.ForeColor = colorTexto;
            panel.BackColor = colorFondo;
            panel.Tag = colorTexto;
            panel.Invalidate();
        }

        private static void AplicarRegionRedondeada(Control control, int radio)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            var area = new Rectangle(0, 0, control.Width, control.Height);
            using var ruta = Formas.Redondeada(area, radio);
            control.Region = new Region(ruta);
        }

        /// <summary>
        /// Recalcula tamanos, fuentes y posiciones. Escala basada en
        /// ancho Y alto (el mas chico manda). AnchoBase/AltoBase estan
        /// alineados con MinimumSize para que nunca se comprima.
        /// </summary>
        private void AplicarEscala()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            float escalaAncho = ClientSize.Width / AnchoBase;
            float escalaAlto = ClientSize.Height / AltoBase;
            _escala = Math.Clamp(Math.Min(escalaAncho, escalaAlto), EscalaMinima, EscalaMaxima);

            int anchoTotal = _panelContenido.ClientSize.Width - _panelContenido.Padding.Horizontal;
            int altoTotal = _panelContenido.ClientSize.Height - _panelContenido.Padding.Vertical;
            if (anchoTotal <= 0 || altoTotal <= 0) return;

            int x0 = _panelContenido.Padding.Left;
            int y0 = _panelContenido.Padding.Top;

            int gapSeccion = (int)(22 * _escala);
            int gapGrupo = (int)(12 * _escala);

            int alturaHardware = (int)(470 * _escala);
            int alturaParametros = (int)(168 * _escala);
            int alturaAccionesFinal = (int)(78 * _escala);

            _tarjetaHardware.Location = new Point(x0, y0);
            _tarjetaHardware.Size = new Size(anchoTotal, alturaHardware);
            PosicionarHardware(anchoTotal);

            int y = y0 + alturaHardware + gapSeccion;

            _lblTituloParametros.Font = new Font("Segoe UI", 13 * _escala, FontStyle.Bold);
            _lblTituloParametros.Location = new Point(x0, y);

            _pnlAcentoParametros.Size = new Size((int)(48 * _escala), (int)(3 * _escala));
            _pnlAcentoParametros.Location = new Point(x0, _lblTituloParametros.Bottom + (int)(4 * _escala));

            y = _pnlAcentoParametros.Bottom + gapGrupo;

            int espacioTarjetas = (int)(18 * _escala);
            int anchoMitad = (anchoTotal - espacioTarjetas) / 2;

            _tarjetaCooldown.Location = new Point(x0, y);
            _tarjetaCooldown.Size = new Size(anchoMitad, alturaParametros);
            PosicionarCooldown(anchoMitad);

            _tarjetaSql.Location = new Point(x0 + anchoMitad + espacioTarjetas, y);
            _tarjetaSql.Size = new Size(anchoMitad, alturaParametros);
            PosicionarSql(anchoMitad);

            y = _tarjetaCooldown.Bottom + gapSeccion;

            _pnlAccionesFinal.Location = new Point(x0, y);
            _pnlAccionesFinal.Size = new Size(anchoTotal, alturaAccionesFinal);
            AplicarRegionRedondeada(_pnlAccionesFinal, 12);
            PosicionarAcciones(anchoTotal, alturaAccionesFinal);
        }

        private void PosicionarHardware(int anchoDisponible)
        {
            int margen = (int)(24 * _escala);
            int anchoInterno = Math.Max(anchoDisponible - margen * 2, 100);

            int gapSeccion = (int)(20 * _escala);
            int gapGrande = (int)(28 * _escala);
            int gapGrupo = (int)(8 * _escala);

            int alturaRibbon = (int)(54 * _escala);
            _lblTituloHardware.Font = new Font("Segoe UI", 14 * _escala, FontStyle.Bold);
            _pnlTituloHardware.Location = new Point(margen, margen);
            _pnlTituloHardware.Size = new Size(anchoInterno, alturaRibbon);
            AplicarRegionRedondeada(_pnlTituloHardware, (int)(10 * _escala));

            int y = _pnlTituloHardware.Bottom + gapSeccion;

            _panelTip.Size = new Size(anchoInterno, (int)(44 * _escala));
            _panelTip.Location = new Point(margen, y);
            _lblTip.Font = new Font("Segoe UI Emoji", 9 * _escala);
            _lblTip.Size = new Size(Math.Max(anchoInterno - (int)(20 * _escala), 60), _panelTip.Height);
            _lblTip.Location = new Point((int)(10 * _escala), 0);

            // Fila de dos columnas: controles a la izquierda, vista
            // previa a la derecha, ARRANCANDO EN LA MISMA FILA (antes
            // estaban apilados y la vista previa quedaba aislada abajo).
            int yColumnas = _panelTip.Bottom + gapGrande;

            int gapColumnas = (int)(24 * _escala);
            int anchoIzq = (int)(anchoInterno * 0.34);
            int anchoDer = anchoInterno - anchoIzq - gapColumnas;
            int xDer = margen + anchoIzq + gapColumnas;

            // --- Columna izquierda: fuente de video + acciones ---
            _lblFuenteVideo.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _lblFuenteVideo.Location = new Point(margen, yColumnas);

            int yIzq = _lblFuenteVideo.Bottom + gapGrupo;
            int altoControl = (int)(38 * _escala);

            _cmbCamara.Font = new Font("Segoe UI", 9.5f * _escala);
            _cmbCamara.Width = anchoIzq;
            _cmbCamara.Location = new Point(margen, yIzq + (altoControl - _cmbCamara.Height) / 2);

            yIzq = yIzq + altoControl + gapSeccion;

            _btnProbarCamara.Font = new Font("Segoe UI Emoji", 9.5f * _escala, FontStyle.Bold);
            _btnProbarCamara.Size = new Size(anchoIzq, (int)(42 * _escala));
            _btnProbarCamara.Location = new Point(margen, yIzq);
            AplicarRegionRedondeada(_btnProbarCamara, _btnProbarCamara.Height / 2);

            yIzq = _btnProbarCamara.Bottom + gapGrupo;

            _btnDetenerPrueba.Font = new Font("Segoe UI Emoji", 9 * _escala);
            _btnDetenerPrueba.Size = new Size(anchoIzq, (int)(36 * _escala));
            _btnDetenerPrueba.Location = new Point(margen, yIzq);
            AplicarRegionRedondeada(_btnDetenerPrueba, _btnDetenerPrueba.Height / 2);

            // --- Columna derecha: vista previa, misma fila de inicio ---
            _lblCamaraUsbTitulo.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _lblCamaraUsbTitulo.Location = new Point(xDer, yColumnas);

            int yDer = _lblCamaraUsbTitulo.Bottom + gapGrupo;

            _lblEstadoCamara.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _pnlEstadoCamara.Height = (int)(34 * _escala);

            int altoPreview = Math.Max(_tarjetaHardware.Height - yDer - margen, (int)(210 * _escala));
            _pnlPreviewContainer.Location = new Point(xDer, yDer);
            _pnlPreviewContainer.Size = new Size(anchoDer, altoPreview);
            AplicarRegionRedondeada(_pnlPreviewContainer, (int)(12 * _escala));
        }

        private void PosicionarCooldown(int anchoDisponible)
        {
            int margen = (int)(20 * _escala);

            _lblTituloCooldown.Font = new Font("Segoe UI", 11 * _escala, FontStyle.Bold);
            _lblTituloCooldown.Location = new Point(margen, margen);

            int y = _lblTituloCooldown.Bottom + (int)(18 * _escala);
            int ladoBoton = (int)(38 * _escala);
            int anchoValor = (int)(72 * _escala);

            _btnMenos.Size = new Size(ladoBoton, ladoBoton);
            _btnMenos.Location = new Point(margen, y);
            AplicarRegionRedondeada(_btnMenos, ladoBoton / 2);

            _lblCooldownValor.Font = new Font("Segoe UI", 17 * _escala, FontStyle.Bold);
            _lblCooldownValor.Size = new Size(anchoValor, ladoBoton);
            _lblCooldownValor.Location = new Point(_btnMenos.Right + (int)(10 * _escala), y);

            _btnMas.Size = new Size(ladoBoton, ladoBoton);
            _btnMas.Location = new Point(_lblCooldownValor.Right + (int)(10 * _escala), y);
            AplicarRegionRedondeada(_btnMas, ladoBoton / 2);

            _lblCooldownCaption.Font = new Font("Segoe UI", 8.5f * _escala);
            _lblCooldownCaption.Location = new Point(margen, _btnMenos.Bottom + (int)(12 * _escala));

            _ = anchoDisponible;
        }

        private void PosicionarSql(int anchoDisponible)
        {
            int margen = (int)(20 * _escala);
            int anchoInterno = Math.Max(anchoDisponible - margen * 2, 100);

            _lblTituloSql.Font = new Font("Segoe UI", 11 * _escala, FontStyle.Bold);
            _lblTituloSql.Location = new Point(margen, margen);

            int y = _lblTituloSql.Bottom + (int)(16 * _escala);

            _btnProbarSql.Font = new Font("Segoe UI Emoji", 9.5f * _escala, FontStyle.Bold);
            _btnProbarSql.Size = new Size((int)(230 * _escala), (int)(38 * _escala));
            _btnProbarSql.Location = new Point(margen, y);
            AplicarRegionRedondeada(_btnProbarSql, _btnProbarSql.Height / 2);

            y = _btnProbarSql.Bottom + (int)(14 * _escala);

            _lblResultadoSql.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _pnlResultadoSql.Size = new Size(anchoInterno, (int)(44 * _escala));
            _pnlResultadoSql.Location = new Point(margen, y);
            AplicarRegionRedondeada(_pnlResultadoSql, (int)(10 * _escala));
        }

        private void PosicionarAcciones(int anchoDisponible, int alturaFila)
        {
            int margen = (int)(18 * _escala);
            int altoBoton = (int)(44 * _escala);
            int centrado = (alturaFila - altoBoton) / 2;

           
            _btnGuardar.Font = new Font("Segoe UI Emoji", 10 * _escala, FontStyle.Bold);
            _btnGuardar.Size = new Size((int)(280 * _escala), altoBoton);
            _btnGuardar.Location = new Point(anchoDisponible - margen - _btnGuardar.Width, centrado);

            int gapBotones = (int)(25 * _escala);
            _btnRegresar.Size = new Size((int)(140 * _escala), altoBoton);
            _btnRegresar.Location = new Point(_btnGuardar.Left - gapBotones - _btnRegresar.Width, centrado);

            int alturaBanner = (int)(38 * _escala);
            int centradoBanner = (alturaFila - alturaBanner) / 2;
            int gapAntesBotones = (int)(16 * _escala);

            _lblGuardado.Font = new Font("Segoe UI", 9 * _escala, FontStyle.Bold);
            _pnlGuardado.Size = new Size(Math.Max(_btnRegresar.Left - margen - gapAntesBotones, 80), alturaBanner);
            _pnlGuardado.Location = new Point(margen, centradoBanner);
            AplicarRegionRedondeada(_pnlGuardado, (int)(9 * _escala));
        }
    }
}
