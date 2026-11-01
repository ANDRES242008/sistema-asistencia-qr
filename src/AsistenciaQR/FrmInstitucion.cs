using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Nombre, logo y color de la institucion (solo Administrador),
    /// con una vista previa del carnet que se actualiza al momento.
    /// </summary>
    public class FrmInstitucion : Form
    {
        private const string CodigoVistaPrevia = "VISTA-PREVIA";

        private readonly InstitucionService _institucionService = new();
        private readonly Estudiante _estudianteEjemplo = new()
        {
            NIE = "12345678",
            NombreCompleto = "Nombre Apellido Ejemplo",
            Grado = "1er Ano",
            Seccion = "A"
        };
        private readonly Qr _qrEjemplo;

        private byte[]? _logo;
        private string? _color;

        private TextBox _txtNombre = null!;
        private PictureBox _picLogo = null!;
        private Panel _pnlColor = null!;
        private Label _lblError = null!;
        private PictureBox _picVistaPrevia = null!;

        public FrmInstitucion()
        {
            var actual = _institucionService.Obtener();
            _logo = actual.Logo;
            _color = actual.ColorPrincipal;
            _qrEjemplo = new Qr { CodigoQR = CodigoVistaPrevia, RutaImagen = new QRService().DibujarImagen(CodigoVistaPrevia) };

            ConstruirInterfaz();
            _txtNombre.Text = actual.Nombre;
            MostrarLogo();
            MostrarColor();
            ActualizarVistaPrevia();
        }

        private void ActualizarVistaPrevia()
        {
            var institucion = new ConfiguracionInstitucion
            {
                Nombre = string.IsNullOrWhiteSpace(_txtNombre.Text) ? InstitucionService.NombrePorDefecto : _txtNombre.Text.Trim(),
                Logo = _logo,
                ColorPrincipal = _color
            };

            var anterior = _picVistaPrevia.Image;
            _picVistaPrevia.Image = new CarnetService(institucion).DibujarCarnet(_estudianteEjemplo, _qrEjemplo);
            anterior?.Dispose();
        }

        private void BtnElegirLogo_Click(object? sender, EventArgs e)
        {
            using var dialogo = new OpenFileDialog
            {
                Filter = "Imagenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "Elegir logo o escudo"
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            var logo = InstitucionService.PrepararLogo(dialogo.FileName, out string? motivo);
            if (logo is null)
            {
                MessageBox.Show(this, motivo, "Imagen no valida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _logo = logo;
            MostrarLogo();
            ActualizarVistaPrevia();
        }

        private void BtnElegirColor_Click(object? sender, EventArgs e)
        {
            using var dialogo = new ColorDialog
            {
                FullOpen = true,
                Color = InstitucionService.LeerColor(_color) ?? Color.FromArgb(13, 120, 92)
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            _color = InstitucionService.ConvertirColor(dialogo.Color);
            MostrarColor();
            ActualizarVistaPrevia();
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            var resultado = _institucionService.Guardar(_txtNombre.Text, _logo, _color);
            if (!resultado.Exitoso)
            {
                _lblError.Text = resultado.Mensaje;
                _lblError.Visible = true;
                return;
            }

            MessageBox.Show(this,
                resultado.Mensaje + "\nLos carnets ya impresos no cambian; vuelve a generarlos si quieres el diseno nuevo.",
                "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void MostrarLogo()
        {
            _picLogo.Image?.Dispose();
            _picLogo.Image = null;
            if (_logo is null) return;

            using var flujo = new MemoryStream(_logo);
            _picLogo.Image = new Bitmap(flujo);
        }

        private void MostrarColor()
        {
            _pnlColor.BackColor = InstitucionService.LeerColor(_color) ?? Color.FromArgb(13, 120, 92);
        }

        private void ConstruirInterfaz()
        {
            // Medidas de diseno a 100%; E() las lleva a la escala real de Windows.
            int E(int pixeles) => Escala.Px(this, pixeles);

            Text = "AsistenciaQR - Datos de la institucion";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Colores.BlancoPuro;
            ClientSize = new Size(E(860), E(600));

            int x = E(32);
            int ancho = E(440);
            int y = E(26);

            var lblTitulo = new Label
            {
                Text = "Datos de la institucion",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true,
                Location = new Point(x, y)
            };
            Controls.Add(lblTitulo);
            y = lblTitulo.Bottom + E(6);

            var lblAyuda = new Label
            {
                Text = "Aparecen en los carnets, los reportes y el menu. Se guardan en la base de datos, " +
                       "asi se ven igual en todas las computadoras y viajan con los respaldos.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Colores.GrisSecundario,
                MaximumSize = new Size(ancho, 0),
                AutoSize = true,
                Location = new Point(x, y)
            };
            Controls.Add(lblAyuda);
            y = lblAyuda.Bottom + E(20);

            var lblNombre = CrearEtiqueta($"Nombre en el carnet (hasta {InstitucionService.LargoMaximoNombre} letras)", x, y);
            Controls.Add(lblNombre);
            _txtNombre = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Width = ancho,
                MaxLength = InstitucionService.LargoMaximoNombre,
                Location = new Point(x, lblNombre.Bottom + E(6))
            };
            _txtNombre.TextChanged += (_, _) => ActualizarVistaPrevia();
            Controls.Add(_txtNombre);
            y = _txtNombre.Bottom + E(22);

            var lblLogo = CrearEtiqueta("Logo o escudo", x, y);
            Controls.Add(lblLogo);
            y = lblLogo.Bottom + E(8);
            _picLogo = new PictureBox
            {
                Size = new Size(E(96), E(96)),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Colores.GrisClaro,
                Location = new Point(x, y)
            };
            Controls.Add(_picLogo);

            var btnElegirLogo = CrearBoton("Elegir imagen...", new Point(x + E(112), y + E(4)), E(170), E(38));
            btnElegirLogo.Click += BtnElegirLogo_Click;
            var btnQuitarLogo = CrearBoton("Quitar logo", new Point(x + E(112), y + E(50)), E(170), E(38));
            btnQuitarLogo.Click += (_, _) => { _logo = null; MostrarLogo(); ActualizarVistaPrevia(); };
            Controls.Add(btnElegirLogo);
            Controls.Add(btnQuitarLogo);
            y = _picLogo.Bottom + E(22);

            var lblColor = CrearEtiqueta("Color de la banda del carnet", x, y);
            Controls.Add(lblColor);
            y = lblColor.Bottom + E(8);
            _pnlColor = new Panel { Size = new Size(E(96), E(38)), BorderStyle = BorderStyle.FixedSingle, Location = new Point(x, y) };
            Controls.Add(_pnlColor);

            var btnElegirColor = CrearBoton("Elegir color...", new Point(x + E(112), y), E(150), E(38));
            btnElegirColor.Click += BtnElegirColor_Click;
            var btnColorOriginal = CrearBoton("Color original", new Point(x + E(272), y), E(150), E(38));
            btnColorOriginal.Click += (_, _) => { _color = null; MostrarColor(); ActualizarVistaPrevia(); };
            Controls.Add(btnElegirColor);
            Controls.Add(btnColorOriginal);
            y = _pnlColor.Bottom + E(18);

            _lblError = new Label
            {
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 9),
                MaximumSize = new Size(ancho, 0),
                AutoSize = true,
                Visible = false,
                Location = new Point(x, y)
            };
            Controls.Add(_lblError);

            // La ventana crece si el contenido no cabe (escala de Windows alta).
            int yBotones = Math.Max(ClientSize.Height - E(70), y + E(48));
            ClientSize = new Size(ClientSize.Width, yBotones + E(70));

            var btnGuardar = new BotonPrimario
            {
                Text = "Guardar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(E(214), E(42)),
                Location = new Point(x, yBotones)
            };
            btnGuardar.Click += BtnGuardar_Click;
            AcceptButton = btnGuardar;

            var btnCancelar = CrearBoton("Cancelar", new Point(x + E(226), yBotones), E(214), E(42));
            btnCancelar.DialogResult = DialogResult.Cancel;
            CancelButton = btnCancelar;
            Controls.Add(btnGuardar);
            Controls.Add(btnCancelar);

            // Vista previa a la derecha, del alto de la ventana.
            Controls.Add(new Label
            {
                Text = "Vista previa",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Colores.GrisSecundario,
                AutoSize = true,
                Location = new Point(E(560), E(26))
            });
            _picVistaPrevia = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(E(530), E(56)),
                Size = new Size(E(300), ClientSize.Height - E(86))
            };
            Controls.Add(_picVistaPrevia);
        }

        private static Label CrearEtiqueta(string texto, int x, int y) => new()
        {
            Text = texto,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Colores.GrisOscuro,
            AutoSize = true,
            Location = new Point(x, y)
        };

        private static Button CrearBoton(string texto, Point ubicacion, int ancho, int alto)
        {
            var boton = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                BackColor = Colores.BlancoPuro,
                ForeColor = Colores.GrisOscuro,
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(ancho, alto),
                Location = ubicacion
            };
            boton.FlatAppearance.BorderColor = Color.FromArgb(219, 224, 230);
            return boton;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _picVistaPrevia?.Image?.Dispose();
                _picLogo?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
