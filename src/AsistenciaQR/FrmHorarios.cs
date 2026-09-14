using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
  
    public class FrmHorarios : Form
    {
        private const int ToleranciaMinima = 0;
        private const int ToleranciaMaxima = 120;

        private const float AnchoBase = 1080f;
        private const float AltoBase = 760f;
        private const float EscalaMinima = 0.9f;
        private const float EscalaMaxima = 1.6f;

        private static readonly Color ColorPillGrisFondo = Color.FromArgb(241, 245, 249);
        private static readonly Color ColorPillAmbarFondo = Color.FromArgb(254, 243, 199);
        private static readonly Color ColorPillAmbarTexto = Color.FromArgb(180, 120, 10);
        private static readonly Color ColorPillVerdeFondo = Color.FromArgb(209, 250, 229);

        private readonly ConfiguracionHorarioService _horarioService = new();
        private TimeSpan _horaInicioActual = TimeSpan.FromHours(7);
        private int _toleranciaActual = 10;
        private float _escala = 1f;

        private Panel _panelContenido = null!;

        private TarjetaBlanca _tarjeta = null!;
        private Label _lblTitulo = null!;
        private Label _lblSubtitulo = null!;

        private Label _lblEtiquetaHora = null!;
        private DateTimePicker _dtpHoraInicio = null!;
        private Label _lblIconoReloj = null!;

        private Label _lblEtiquetaTolerancia = null!;
        private NumericUpDown _numTolerancia = null!;
        private Label _lblToleranciaCaption = null!;

        private Label _lblCaptionLinea = null!;
        private Panel _pnlLineaTiempo = null!;

        private Panel _pnlLimite = null!;
        private Label _lblLimite = null!;

        private Panel _pnlAccionesFinal = null!;
        private Panel _pnlGuardado = null!;
        private Label _lblGuardado = null!;
        private BotonRegresar _btnRegresar = null!;
        private BotonPrimario _btnGuardar = null!;

        public FrmHorarios()
        {
            ConstruirInterfaz();
            Load += (_, _) => { CargarConfiguracionActual(); AplicarEscala(); };
        }

        private void CargarConfiguracionActual()
        {
            var config = _horarioService.ObtenerActual();

            _horaInicioActual = config.HoraInicio;
            _dtpHoraInicio.Value = DateTime.Today.Add(_horaInicioActual);

            _toleranciaActual = Math.Clamp(config.MinutosTolerancia, ToleranciaMinima, ToleranciaMaxima);
            ActualizarLabelTolerancia();

            ActualizarLimite();
        }

        private void ActualizarLabelTolerancia() => _numTolerancia.Value = _toleranciaActual;

        private void BtnMenos_Click(object? sender, EventArgs e)
        {
            _toleranciaActual = Math.Max(_toleranciaActual - 5, ToleranciaMinima);
            ActualizarLabelTolerancia();
            ActualizarLimite();
        }

        private void BtnMas_Click(object? sender, EventArgs e)
        {
            _toleranciaActual = Math.Min(_toleranciaActual + 5, ToleranciaMaxima);
            ActualizarLabelTolerancia();
            ActualizarLimite();
        }

        private void ActualizarLimite()
        {
            var limite = _horaInicioActual + TimeSpan.FromMinutes(_toleranciaActual);
            EstablecerBanner(_pnlLimite, _lblLimite,
                $"⏰  Los estudiantes que marquen despues de las {DateTime.Today.Add(limite):hh:mm tt} quedaran registrados como Tardanza.",
                ColorPillAmbarTexto, ColorPillAmbarFondo);

            _pnlLineaTiempo.Invalidate();
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            _horarioService.Guardar(_horaInicioActual, _toleranciaActual);

            EstablecerBanner(_pnlGuardado, _lblGuardado,
                "✓  Guardado. Los registros anteriores no cambian, solo aplica de ahora en adelante.",
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
            Text = "AsistenciaQR - Horarios y Tolerancia";
            Width = (int)AnchoBase;
            Height = (int)AltoBase;
            MinimumSize = new Size(1000, 700);
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

            ConstruirTarjeta();
            ConstruirBarraAcciones();

            _panelContenido.Controls.AddRange(new Control[] { _tarjeta, _pnlAccionesFinal });
            Controls.Add(_panelContenido);
        }

        private void ConstruirTarjeta()
        {
            _tarjeta = new TarjetaBlanca();

            _lblTitulo = new Label
            {
                Text = "Control de Puntualidad",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true
            };

            _lblSubtitulo = new Label
            {
                Text = "El sistema usa esto para marcar automaticamente quien llego a tiempo o tarde.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Colores.GrisSecundario,
                AutoSize = false
            };

            // --- Columna izquierda: hora de inicio ---
            _lblEtiquetaHora = CrearEtiqueta("Hora de Inicio de Clases");

            _dtpHoraInicio = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Font = new Font("Segoe UI", 15, FontStyle.Bold)
            };
            _dtpHoraInicio.ValueChanged += (_, _) =>
            {
                _horaInicioActual = _dtpHoraInicio.Value.TimeOfDay;
                ActualizarLimite();
            };

            _lblIconoReloj = new Label
            {
                Text = "🕐",
                Font = new Font("Segoe UI Emoji", 16),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true
            };

            _lblEtiquetaTolerancia = CrearEtiqueta("Minutos de Tolerancia");

            _numTolerancia = new NumericUpDown
            {
                Minimum = ToleranciaMinima,
                Maximum = ToleranciaMaxima,
                Font = new Font("Segoe UI", 15, FontStyle.Bold)
            };
            _numTolerancia.ValueChanged += (_, _) =>
            {
                _toleranciaActual = (int)_numTolerancia.Value;
                ActualizarLimite();
            };

            _lblToleranciaCaption = new Label
            {
                Text = "minutos de gracia despues del inicio",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true
            };

            _lblCaptionLinea = CrearEtiqueta("Vista Rapida del Corte de Tardanza");

            _pnlLineaTiempo = new Panel { BackColor = Colores.BlancoPuro };
            _pnlLineaTiempo.Paint += PintarLineaDeTiempo;

        
            (_pnlLimite, _lblLimite) = CrearBanner(string.Empty);

            _tarjeta.Controls.AddRange(new Control[]
            {
    _lblTitulo, _lblSubtitulo,
    _lblEtiquetaHora, _dtpHoraInicio, _lblIconoReloj,
    _lblEtiquetaTolerancia, _numTolerancia, _lblToleranciaCaption,
    _lblCaptionLinea, _pnlLineaTiempo,
    _pnlLimite
            });
        }

        private void PintarLineaDeTiempo(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = _pnlLineaTiempo.Width;
            int h = _pnlLineaTiempo.Height;
            if (w <= 40 || h <= 30) return;

            int filaEtiquetas = (int)(30 * _escala);
            int altoBarra = Math.Max((int)(14 * _escala), 8);
            int yBarra = filaEtiquetas + (int)(6 * _escala);

            double ventanaMinutos = Math.Max(60.0, _toleranciaActual + 40.0);
            double fraccionLimite = Math.Clamp(_toleranciaActual / ventanaMinutos, 0.08, 0.92);
            int xLimite = (int)(w * fraccionLimite);

            var areaBarra = new Rectangle(0, yBarra, w, altoBarra);
            using var rutaBarra = Formas.Redondeada(areaBarra, altoBarra / 2);

            var clipOriginal = g.Clip;
            g.SetClip(rutaBarra);
            using (var brochaVerde = new SolidBrush(ColorPillVerdeFondo))
                g.FillRectangle(brochaVerde, 0, yBarra, xLimite, altoBarra);
            using (var brochaRoja = new SolidBrush(Color.FromArgb(254, 226, 226)))
                g.FillRectangle(brochaRoja, xLimite, yBarra, w - xLimite, altoBarra);
            g.Clip = clipOriginal;

            using (var borde = new Pen(Color.FromArgb(226, 232, 240), 1))
                g.DrawPath(borde, rutaBarra);

            int radio = Math.Max((int)(6 * _escala), 4);
            int yCentro = yBarra + altoBarra / 2;

            using (var brochaInicio = new SolidBrush(Colores.AzulMarino))
                g.FillEllipse(brochaInicio, 0, yCentro - radio, radio * 2, radio * 2);

            using (var brochaLimite = new SolidBrush(Color.Firebrick))
                g.FillEllipse(brochaLimite, xLimite - radio, yCentro - radio, radio * 2, radio * 2);

            using var fuenteEtiqueta = new Font("Segoe UI", 8.5f * _escala, FontStyle.Bold);
            string horaInicioTxt = DateTime.Today.Add(_horaInicioActual).ToString("hh:mm tt");
            string horaLimiteTxt = DateTime.Today.Add(_horaInicioActual + TimeSpan.FromMinutes(_toleranciaActual)).ToString("hh:mm tt");

            int anchoEtq = (int)(110 * _escala);
            TextRenderer.DrawText(g, $"Inicio  {horaInicioTxt}", fuenteEtiqueta,
                new Rectangle(0, 0, anchoEtq, filaEtiquetas), Colores.AzulMarino,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

          
            int limiteTxtX = Math.Clamp(xLimite - anchoEtq / 2, anchoEtq, Math.Max(w - anchoEtq, anchoEtq));

            TextRenderer.DrawText(g, $"Limite  {horaLimiteTxt}", fuenteEtiqueta,
                new Rectangle(limiteTxtX, 0, anchoEtq, filaEtiquetas),
                Color.Firebrick, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            using var fuenteCaption = new Font("Segoe UI", 8f * _escala);
            int yCaption = areaBarra.Bottom + (int)(6 * _escala);
            int altoCaption = Math.Max(h - yCaption, 14);
            if (xLimite > 6)
                TextRenderer.DrawText(g, "A tiempo", fuenteCaption, new Rectangle(0, yCaption, xLimite, altoCaption),
                    Colores.VerdeEsmeralda, TextFormatFlags.HorizontalCenter);
            if (w - xLimite > 6)
                TextRenderer.DrawText(g, "Tardanza", fuenteCaption, new Rectangle(xLimite, yCaption, w - xLimite, altoCaption),
                    Color.Firebrick, TextFormatFlags.HorizontalCenter);
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

            (_pnlGuardado, _lblGuardado) = CrearBanner("ℹ️  Los cambios se aplican al guardar.");

            _btnRegresar = new BotonRegresar();
            _btnRegresar.Click += BtnRegresar_Click;

            _btnGuardar = new BotonPrimario { Text = "💾  Guardar Configuracion" };
            _btnGuardar.Click += BtnGuardar_Click;

            _pnlAccionesFinal.Controls.AddRange(new Control[] { _pnlGuardado, _btnRegresar, _btnGuardar });
        }

        private static Label CrearEtiqueta(string texto) => new()
        {
            Text = texto,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Colores.GrisOscuro,
            AutoSize = true
        };

        private static Button CrearBotonFantasma(string texto)
        {
            var boton = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                BackColor = Colores.BlancoPuro,
                ForeColor = Colores.GrisOscuro,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            boton.FlatAppearance.BorderSize = 1;
            boton.FlatAppearance.BorderColor = Color.FromArgb(219, 224, 230);
            return boton;
        }

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
        /// Escala basada en ancho Y alto (el mas chico manda).
        /// AnchoBase/AltoBase alineados con MinimumSize. La tarjeta
        /// crece con la ventana (Fill dinamico), asi que al maximizar
        /// la linea de tiempo y los espacios se acoplan de verdad,
        /// igual que en el Login.
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
            int alturaAccionesFinal = (int)(78 * _escala);
            int alturaTarjeta = Math.Max(altoTotal - gapSeccion - alturaAccionesFinal, (int)(420 * _escala));

            _tarjeta.Location = new Point(x0, y0);
            _tarjeta.Size = new Size(anchoTotal, alturaTarjeta);
            PosicionarTarjeta(anchoTotal, alturaTarjeta);

            int y = y0 + alturaTarjeta + gapSeccion;
            _pnlAccionesFinal.Location = new Point(x0, y);
            _pnlAccionesFinal.Size = new Size(anchoTotal, alturaAccionesFinal);
            AplicarRegionRedondeada(_pnlAccionesFinal, 12);
            PosicionarAcciones(anchoTotal, alturaAccionesFinal);
        }

        private void PosicionarTarjeta(int anchoDisponible, int altoDisponible)
        {
            int margen = (int)(32 * _escala);
            int anchoInterno = Math.Max(anchoDisponible - margen * 2, 100);

            int gapSeccion = (int)(26 * _escala);
            int gapGrupo = (int)(8 * _escala);

            _lblTitulo.Font = new Font("Segoe UI", 20 * _escala, FontStyle.Bold);
            _lblTitulo.Location = new Point(margen, margen);

            _lblSubtitulo.Font = new Font("Segoe UI", 9.5f * _escala);
            _lblSubtitulo.Size = new Size(anchoInterno, (int)(40 * _escala));
            _lblSubtitulo.Location = new Point(margen, _lblTitulo.Bottom + (int)(4 * _escala));

            int y = _lblSubtitulo.Bottom + gapSeccion;

            int gapColumnas = (int)(32 * _escala);
            int anchoColumna = (anchoInterno - gapColumnas) / 2;
            int xDer = margen + anchoColumna + gapColumnas;

            // --- Columna izquierda ---
            _lblEtiquetaHora.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _lblEtiquetaHora.Location = new Point(margen, y);

            int yIzq = _lblEtiquetaHora.Bottom + gapGrupo;

            _dtpHoraInicio.Font = new Font("Segoe UI", 15 * _escala, FontStyle.Bold);
            _dtpHoraInicio.Width = (int)(170 * _escala);
            _dtpHoraInicio.Location = new Point(margen, yIzq);

            _lblIconoReloj.Font = new Font("Segoe UI Emoji", 16 * _escala);
            _lblIconoReloj.Location = new Point(_dtpHoraInicio.Right + (int)(12 * _escala),
                yIzq + (_dtpHoraInicio.Height - _lblIconoReloj.Height) / 2);

            // --- Columna derecha ---
            _lblEtiquetaTolerancia.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _lblEtiquetaTolerancia.Location = new Point(xDer, y);

            int yDer = _lblEtiquetaTolerancia.Bottom + gapGrupo;
            int ladoBoton = (int)(38 * _escala);


            _numTolerancia.Width = (int)(90 * _escala);
            _numTolerancia.Location = new Point(xDer, yDer);

            _lblToleranciaCaption.Location = new Point(xDer, _numTolerancia.Bottom + (int)(8 * _escala));




            _lblToleranciaCaption.Font = new Font("Segoe UI", 8.5f * _escala);
           

            y = Math.Max(_dtpHoraInicio.Bottom, _lblToleranciaCaption.Bottom) + gapSeccion;

            // --- Linea de tiempo: usa el espacio restante disponible ---
            _lblCaptionLinea.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _lblCaptionLinea.Location = new Point(margen, y);

            y = _lblCaptionLinea.Bottom + gapGrupo;

            int alturaBannerLimite = (int)(56 * _escala);
            int alturaTimeline = Math.Max(altoDisponible - y - gapSeccion - alturaBannerLimite - margen,
                (int)(70 * _escala));

            _pnlLineaTiempo.Location = new Point(margen, y);
            _pnlLineaTiempo.Size = new Size(anchoInterno, alturaTimeline);

            y = _pnlLineaTiempo.Bottom + gapSeccion;

            _lblLimite.Font = new Font("Segoe UI", 10.5f * _escala, FontStyle.Bold);
            _pnlLimite.Size = new Size(anchoInterno, alturaBannerLimite);
            _pnlLimite.Location = new Point(margen, y);
            AplicarRegionRedondeada(_pnlLimite, (int)(10 * _escala));
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
