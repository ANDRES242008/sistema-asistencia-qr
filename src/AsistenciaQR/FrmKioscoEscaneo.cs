using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
   
    public class FrmKioscoEscaneo : Form
    {
        private static readonly string[] Meses =
        {
            "enero", "febrero", "marzo", "abril", "mayo", "junio",
            "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
        };

        private readonly CamaraQRService _camaraService = new();
        private readonly AsistenciaService _asistenciaService = new();
        private readonly ConfiguracionService _configuracionService = new();
        private readonly System.Windows.Forms.Timer _timerReloj = new() { Interval = 1000 };

        private float _escala = 1f;

        private Panel _panelPrincipal = null!;
        private Panel _panelIzquierdo = null!;
        private Panel _panelDerecho = null!;
        private Panel _panelInferior = null!;
        private Panel _panelSuperior = null!;
        private Label _lblFechaHora = null!;

        private TarjetaConEncabezado _tarjetaCamara = null!;
        private PictureBox _pictureCamara = null!;
        private Label _lblEstadoCamara = null!;

        private TarjetaConEncabezado _tarjetaEstado = null!;
        private Label _lblEstadoGrande = null!;
        private Label _lblEstadoSubtitulo = null!;

        private TarjetaConEncabezado _tarjetaRespaldo = null!;
        private Label _lblInstruccionRespaldo = null!;
        private CampoTexto _campoNieManual = null!;
        private BotonPrimario _btnRegistrarManual = null!;

        private Label _lblMarca = null!;
        private Label _lblMarcaSubtitulo = null!;
        private BotonRegresar _btnRegresar = null!;

        public FrmKioscoEscaneo()
        {
            ConstruirInterfaz();

            _camaraService.FrameCapturado += ActualizarFrame;
            _camaraService.CodigoDetectado += ProcesarCodigoDetectado;
            _timerReloj.Tick += (_, _) => ActualizarReloj();

            Load += (_, _) =>
            {
                IniciarCamara();
                ActualizarReloj();
                _timerReloj.Start();
                AplicarEscala();
            };
            FormClosing += (_, _) =>
            {
                _timerReloj.Stop();
                _camaraService.Detener();
            };
        }

        private void ActualizarReloj()
        {
            var ahora = DateTime.Now;
            _lblFechaHora.Text = $"{ahora:hh:mm tt} | {ahora.Day} de {Meses[ahora.Month - 1]}, {ahora.Year}";

      
            ReposicionarReloj();
        }

        private void ReposicionarReloj()
        {
           
            if (_panelSuperior == null || _lblFechaHora == null)
            {
                return;
            }

            _lblFechaHora.Location = new Point(_panelSuperior.Width - _lblFechaHora.Width - 20, 12);
        }

        private void IniciarCamara()
        {
            var configuracion = _configuracionService.Cargar();
            _camaraService.SegundosCooldown = configuracion.SegundosCooldown;

            try
            {
                _camaraService.Iniciar(indiceCamara: configuracion.IndiceCamara);
                _lblEstadoCamara.Text = "🟢  Camara Activa";
                _lblEstadoCamara.ForeColor = Colores.VerdeEsmeralda;
            }
            catch (Exception ex)
            {
                _lblEstadoCamara.Text = "🔴  Camara no disponible";
                _lblEstadoCamara.ForeColor = Color.Firebrick;
                MessageBox.Show(this, ex.Message, "Error de camara",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ActualizarFrame(Bitmap frame)
        {
            if (IsDisposed) return;

            DibujarMarcoDeEnfoque(frame);

            BeginInvoke(() =>
            {
                var anterior = _pictureCamara.Image;
                _pictureCamara.Image = frame;
                anterior?.Dispose();
            });
        }

        private static void DibujarMarcoDeEnfoque(Bitmap frame)
        {
            using var g = Graphics.FromImage(frame);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int anchoMarco = (int)(frame.Width * 0.55);
            int altoMarco = (int)(frame.Height * 0.65);
            int x = (frame.Width - anchoMarco) / 2;
            int y = (frame.Height - altoMarco) / 2;

            const int largo = 26;
            using var pluma = new Pen(Colores.VerdeEsmeralda, 4);

            g.DrawLine(pluma, x, y, x + largo, y);
            g.DrawLine(pluma, x, y, x, y + largo);

            g.DrawLine(pluma, x + anchoMarco, y, x + anchoMarco - largo, y);
            g.DrawLine(pluma, x + anchoMarco, y, x + anchoMarco, y + largo);

            g.DrawLine(pluma, x, y + altoMarco, x + largo, y + altoMarco);
            g.DrawLine(pluma, x, y + altoMarco, x, y + altoMarco - largo);

            g.DrawLine(pluma, x + anchoMarco, y + altoMarco, x + anchoMarco - largo, y + altoMarco);
            g.DrawLine(pluma, x + anchoMarco, y + altoMarco, x + anchoMarco, y + altoMarco - largo);
        }

        private void ProcesarCodigoDetectado(string codigoQr)
        {
            var resultado = _asistenciaService.RegistrarPorCodigoQr(codigoQr);
            MostrarResultado(resultado);
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

        private void BtnRegistrarManual_Click(object? sender, EventArgs e)
        {
            string nie = _campoNieManual.ObtenerValorReal().Trim();
            if (string.IsNullOrEmpty(nie))
            {
                MessageBox.Show(this, "Escribe un NIE valido.", "Dato faltante",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var resultado = _asistenciaService.RegistrarPorNie(nie, "Respaldo manual desde Kiosco");
            MostrarResultado(resultado);
        }

        private void MostrarResultado(ResultadoAsistencia resultado)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => MostrarResultado(resultado));
                return;
            }

            switch (resultado.Estado)
            {
                case ResultadoEscaneo.Exitoso when resultado.EsTarde:
                    _lblEstadoGrande.ForeColor = Color.FromArgb(230, 159, 0);
                    _lblEstadoGrande.Text = "LLEGADA TARDIA";
                    SystemSounds.Asterisk.Play();
                    break;

                case ResultadoEscaneo.Exitoso:
                    _lblEstadoGrande.ForeColor = Colores.VerdeEsmeralda;
                    _lblEstadoGrande.Text = "ASISTENCIA A TIEMPO";
                    SystemSounds.Asterisk.Play();
                    break;

                case ResultadoEscaneo.YaRegistradoHoy:
                    _lblEstadoGrande.ForeColor = Color.FromArgb(70, 110, 180);
                    _lblEstadoGrande.Text = "YA REGISTRADO HOY";
                    SystemSounds.Hand.Play();
                    break;

                case ResultadoEscaneo.CodigoNoValido:
                    _lblEstadoGrande.ForeColor = Color.Firebrick;
                    _lblEstadoGrande.Text = "CODIGO NO VALIDO";
                    SystemSounds.Hand.Play();
                    break;
            }

            _lblEstadoSubtitulo.Text = resultado.Estudiante is not null
                ? $"{resultado.Estudiante.NombreCompleto}  ·  NIE {resultado.Estudiante.NIE}  ·  {(resultado.HoraRegistro ?? DateTime.Now):hh:mm tt}"
                : "Verifica el carnet e intenta de nuevo.";

            _campoNieManual.Limpiar();
            AplicarEscala();
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Kiosco de Escaneo";
            Width = 1080;
            Height = 740;
            MinimumSize = new Size(920, 620);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Colores.GrisClaro;
            AutoScaleMode = AutoScaleMode.Dpi;

            // Fill (contenido) SIEMPRE antes que el chrome (Top/Bottom) -
            // mismo bug que ya se corrigio en BarraLateral/Dashboard.
            // Antes Superior(Top) se agregaba primero y podia tapar
            // el contenido principal.
            ConstruirPanelPrincipal();
            ConstruirPanelSuperior();
            ConstruirPanelInferior();
        }

        private void ConstruirPanelSuperior()
        {
            _panelSuperior = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Colores.GrisClaro };

            _lblFechaHora = new Label
            {
                Text = string.Empty,
                ForeColor = Colores.GrisSecundario,
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true
            };

            _panelSuperior.Controls.Add(_lblFechaHora);
            _panelSuperior.Resize += (_, _) => ReposicionarReloj();

            Controls.Add(_panelSuperior);
        }

        private void ConstruirPanelInferior()
        {
            _panelInferior = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Colores.GrisClaro };

            _lblMarca = new Label
            {
                Text = "AsistenciaQR",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true
            };

            _lblMarcaSubtitulo = new Label
            {
                Text = "Portal de Control Escolar",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true
            };

            // Nunca tuvo BotonRegresar (se construyo antes de esa
            // decision). Va en su propia esquina, no estorba la marca
            // centrada.
            _btnRegresar = new BotonRegresar();
            _btnRegresar.Click += BtnRegresar_Click;

            _panelInferior.Controls.Add(_lblMarca);
            _panelInferior.Controls.Add(_lblMarcaSubtitulo);
            _panelInferior.Controls.Add(_btnRegresar);
            _panelInferior.Resize += (_, _) => { CentrarMarca(); PosicionarBotonRegresar(); };
            Controls.Add(_panelInferior);
        }

        private void CentrarMarca()
        {
       
            if (_panelInferior == null || _lblMarca == null || _lblMarcaSubtitulo == null)
            {
                return; 
            }

            
            _lblMarca.Location = new Point((_panelInferior.Width - _lblMarca.Width) / 2, 2);
            _lblMarcaSubtitulo.Location = new Point((_panelInferior.Width - _lblMarcaSubtitulo.Width) / 2, _lblMarca.Bottom);
        }

        private void PosicionarBotonRegresar()
        {
            if (_btnRegresar == null || _panelInferior == null) return;

            _btnRegresar.Size = new Size((int)(120 * _escala), (int)(36 * _escala));
            _btnRegresar.Location = new Point(
                (int)(20 * _escala),
                (_panelInferior.Height - _btnRegresar.Height) / 2);
        }

        private void ConstruirPanelPrincipal()
        {
            _panelPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Colores.GrisClaro,
                Padding = new Padding(28) // antes 20 - mas aire alrededor de las tarjetas
            };
            _panelPrincipal.Resize += (_, _) => AplicarEscala();

            ConstruirTarjetaCamara();
            ConstruirTarjetaEstado();
            ConstruirTarjetaRespaldo();

            _panelIzquierdo = new Panel { BackColor = Colores.GrisClaro };
            _panelIzquierdo.Controls.Add(_tarjetaCamara);

            _panelDerecho = new Panel { BackColor = Colores.GrisClaro };
            _panelDerecho.Controls.Add(_tarjetaEstado);
            _panelDerecho.Controls.Add(_tarjetaRespaldo);

            _panelPrincipal.Controls.Add(_panelDerecho);
            _panelPrincipal.Controls.Add(_panelIzquierdo);

            Controls.Add(_panelPrincipal);
        }

        private void ConstruirTarjetaCamara()
        {
            _tarjetaCamara = new TarjetaConEncabezado
            {
                TextoEncabezado = "Lector de Asistencia (Camara)",
                ColorEncabezado = Colores.AzulMarino
            };

            _pictureCamara = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Black
            };

            _lblEstadoCamara = new Label
            {
                Text = "Conectando...",
                ForeColor = Colores.GrisSecundario,
                Font = new Font("Segoe UI", 9.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 34
            };

            _tarjetaCamara.ContenidoPanel.Controls.Add(_pictureCamara);
            _tarjetaCamara.ContenidoPanel.Controls.Add(_lblEstadoCamara);
            _tarjetaCamara.ContenidoPanel.Resize += (_, _) =>
            {
                _pictureCamara.Location = new Point(8, 8);
                _pictureCamara.Size = new Size(
                    Math.Max(_tarjetaCamara.ContenidoPanel.Width - 16, 10),
                    Math.Max(_tarjetaCamara.ContenidoPanel.Height - _lblEstadoCamara.Height - 16, 10));
            };
        }

        private void ConstruirTarjetaEstado()
        {
            _tarjetaEstado = new TarjetaConEncabezado
            {
                TextoEncabezado = "Estado del Escaner",
                ColorEncabezado = Colores.AzulMarino
            };

            _lblEstadoGrande = new Label
            {
                Text = "LISTO PARA ESCANEAR",
                ForeColor = Colores.VerdeEsmeralda,
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                AutoSize = true
            };

            _lblEstadoSubtitulo = new Label
            {
                Text = "Presente su carnet con el codigo QR frente a la camara.",
                ForeColor = Colores.GrisSecundario,
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                MaximumSize = new Size(320, 0)
            };

            _tarjetaEstado.ContenidoPanel.Controls.Add(_lblEstadoGrande);
            _tarjetaEstado.ContenidoPanel.Controls.Add(_lblEstadoSubtitulo);
        }

        private void ConstruirTarjetaRespaldo()
        {
            _tarjetaRespaldo = new TarjetaConEncabezado
            {
                TextoEncabezado = "Respaldo Manual (NIE)",
                ColorEncabezado = Colores.AzulMarino
            };

            _lblInstruccionRespaldo = new Label
            {
                Text = "Ingrese su NIE o Numero de Identificacion",
                ForeColor = Colores.GrisOscuro,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true
            };

            _campoNieManual = new CampoTexto("\U0001F194", "NIE del estudiante");
            _campoNieManual.EnterPresionado += () => BtnRegistrarManual_Click(this, EventArgs.Empty);

            _btnRegistrarManual = new BotonPrimario { Text = "Registrar" };
            _btnRegistrarManual.Click += BtnRegistrarManual_Click;

            _tarjetaRespaldo.ContenidoPanel.Controls.Add(_lblInstruccionRespaldo);
            _tarjetaRespaldo.ContenidoPanel.Controls.Add(_campoNieManual);
            _tarjetaRespaldo.ContenidoPanel.Controls.Add(_btnRegistrarManual);
        }

        /// <summary>
        /// Recalcula tamanos y fuentes de TODO el Kiosco segun el
        /// tamano actual de la ventana. Al maximizar, el contenido
        /// crece de verdad, incluyendo la caja de "Camara Activa"
        /// (antes solo crecia la letra, no su contenedor, y se cortaba).
        /// </summary>
        private void AplicarEscala()
        {
            if (_panelPrincipal.Width <= 0 || _panelPrincipal.Height <= 0) return;

            // Antes solo se usaba el ancho: en un monitor ancho pero
            // no muy alto esto hacia crecer el contenido mas de lo
            // que cabia verticalmente. Ahora manda el mas chico.
            float escalaAncho = _panelPrincipal.Width / 1040f;
            float escalaAlto = _panelPrincipal.Height / 640f;
            _escala = Math.Clamp(Math.Min(escalaAncho, escalaAlto), 1f, 1.8f);

            int anchoDisponible = _panelPrincipal.ClientSize.Width - _panelPrincipal.Padding.Horizontal;
            int altoDisponible = _panelPrincipal.ClientSize.Height - _panelPrincipal.Padding.Vertical;
            if (anchoDisponible <= 0 || altoDisponible <= 0) return;

            int espacioEntreColumnas = (int)(26 * _escala); // antes 20
            int anchoIzquierdo = (int)(anchoDisponible * 0.56);
            int anchoDerecho = Math.Max(anchoDisponible - anchoIzquierdo - espacioEntreColumnas, 200);

            _panelIzquierdo.Location = new Point(_panelPrincipal.Padding.Left, _panelPrincipal.Padding.Top);
            _panelIzquierdo.Size = new Size(anchoIzquierdo, altoDisponible);
            _tarjetaCamara.Location = Point.Empty;
            _tarjetaCamara.Size = _panelIzquierdo.Size;
            _tarjetaCamara.AlturaEncabezado = (int)(54 * _escala); // antes 46, mas aire
            _tarjetaCamara.FuenteEncabezado = new Font("Segoe UI", 12 * _escala, FontStyle.Bold);

            // Antes solo se escalaba la fuente de este texto, nunca
            // su contenedor - por eso se cortaba al maximizar.
            _lblEstadoCamara.Font = new Font("Segoe UI", 9.5f * _escala);
            _lblEstadoCamara.Height = (int)(36 * _escala);

            _panelDerecho.Location = new Point(_panelIzquierdo.Right + espacioEntreColumnas, _panelPrincipal.Padding.Top);
            _panelDerecho.Size = new Size(anchoDerecho, altoDisponible);

            int alturaEstado = (int)(200 * _escala);
            _tarjetaEstado.Location = new Point(0, 0);
            _tarjetaEstado.Size = new Size(anchoDerecho, alturaEstado);
            _tarjetaEstado.AlturaEncabezado = (int)(50 * _escala); // antes 42
            _tarjetaEstado.FuenteEncabezado = new Font("Segoe UI", 11 * _escala, FontStyle.Bold);

            int margenInternoEstado = (int)(22 * _escala);
            _lblEstadoGrande.Font = new Font("Segoe UI", 17 * _escala, FontStyle.Bold);
            _lblEstadoGrande.Location = new Point(margenInternoEstado, margenInternoEstado);
            _lblEstadoSubtitulo.Font = new Font("Segoe UI", 9.5f * _escala);
            _lblEstadoSubtitulo.MaximumSize = new Size((int)(320 * _escala), 0);
            _lblEstadoSubtitulo.Location = new Point(margenInternoEstado, _lblEstadoGrande.Bottom + (int)(8 * _escala));

            int margenSuperiorRespaldo = (int)(22 * _escala); // antes 16
            _tarjetaRespaldo.Location = new Point(0, _tarjetaEstado.Bottom + margenSuperiorRespaldo);
            _tarjetaRespaldo.Size = new Size(anchoDerecho, Math.Max(altoDisponible - _tarjetaRespaldo.Top, 100));
            _tarjetaRespaldo.AlturaEncabezado = (int)(50 * _escala); // antes 42
            _tarjetaRespaldo.FuenteEncabezado = new Font("Segoe UI", 11 * _escala, FontStyle.Bold);

            int margenInterno = (int)(22 * _escala);
            _lblInstruccionRespaldo.Font = new Font("Segoe UI", 9.5f * _escala, FontStyle.Bold);
            _lblInstruccionRespaldo.Location = new Point(margenInterno, margenInterno);

            int anchoCampo = Math.Max(_tarjetaRespaldo.ContenidoPanel.Width - margenInterno * 2, 100);
            _campoNieManual.Width = anchoCampo;
            _campoNieManual.Location = new Point(margenInterno, _lblInstruccionRespaldo.Bottom + (int)(10 * _escala));
            _campoNieManual.EscalarA(_escala);

            _btnRegistrarManual.Font = new Font("Segoe UI", 10.5f * _escala, FontStyle.Bold);
            _btnRegistrarManual.Width = anchoCampo;
            _btnRegistrarManual.Height = (int)(44 * _escala);
            _btnRegistrarManual.Location = new Point(margenInterno, _campoNieManual.Bottom + (int)(16 * _escala));

            CentrarMarca();
            PosicionarBotonRegresar();
            ReposicionarReloj();
        }
    }
}
