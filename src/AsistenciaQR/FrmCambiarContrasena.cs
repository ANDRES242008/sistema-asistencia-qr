using System.Drawing;
using System.Windows.Forms;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Dialogo para cambiar la propia contrasena. En modo obligatorio
    /// (despues de que un Administrador la reseteo) no pide la actual,
    /// porque el usuario acaba de escribirla en el login, y no deja
    /// entrar al sistema hasta cambiarla.
    /// </summary>
    public class FrmCambiarContrasena : Form
    {
        private readonly AuthService _authService = new();
        private readonly int _usuarioId;
        private readonly bool _obligatorio;

        private CampoSubrayado _campoActual = null!;
        private CampoSubrayado _campoNueva = null!;
        private CampoSubrayado _campoConfirmar = null!;
        private Label _lblError = null!;

        public FrmCambiarContrasena(int usuarioId, bool obligatorio)
        {
            _usuarioId = usuarioId;
            _obligatorio = obligatorio;
            ConstruirInterfaz();
        }

        private void Guardar()
        {
            var resultado = _authService.CambiarContrasena(
                _usuarioId,
                _obligatorio ? null : _campoActual.Texto,
                _campoNueva.Texto,
                _campoConfirmar.Texto);

            if (!resultado.Exitoso)
            {
                _lblError.Text = resultado.Mensaje;
                _lblError.Visible = true;
                return;
            }

            MessageBox.Show(this, resultado.Mensaje, "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Cambiar contrasena";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Colores.BlancoPuro;
            ClientSize = new Size(440, _obligatorio ? 360 : 420);

            const int margen = 32;
            const int ancho = 440 - margen * 2;
            int y = 26;

            var lblTitulo = new Label
            {
                Text = _obligatorio ? "Crea tu nueva contrasena" : "Cambiar contrasena",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true,
                Location = new Point(margen, y)
            };
            Controls.Add(lblTitulo);
            y += 40;

            var lblSubtitulo = new Label
            {
                Text = _obligatorio
                    ? "Tu contrasena fue asignada por un Administrador. Cambiala para continuar."
                    : $"Minimo {AuthService.LongitudMinimaContrasena} caracteres.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Colores.GrisSecundario,
                MaximumSize = new Size(ancho, 0),
                AutoSize = true,
                Location = new Point(margen, y)
            };
            Controls.Add(lblSubtitulo);
            y = lblSubtitulo.Bottom + 18;

            _campoActual = new CampoSubrayado { EsContrasena = true };
            if (!_obligatorio)
            {
                y = AgregarCampo("Contrasena actual", _campoActual, margen, y, ancho);
            }

            _campoNueva = new CampoSubrayado { EsContrasena = true };
            y = AgregarCampo("Nueva contrasena", _campoNueva, margen, y, ancho);

            _campoConfirmar = new CampoSubrayado { EsContrasena = true };
            y = AgregarCampo("Confirmar nueva contrasena", _campoConfirmar, margen, y, ancho);

            _lblError = new Label
            {
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 9),
                MaximumSize = new Size(ancho, 0),
                AutoSize = true,
                Visible = false,
                Location = new Point(margen, y)
            };
            Controls.Add(_lblError);
            y += 30;

            int anchoBoton = (ancho - 12) / 2;
            var btnGuardar = new BotonPrimario
            {
                Text = "Guardar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(anchoBoton, 40),
                Location = new Point(margen, y)
            };
            btnGuardar.Click += (_, _) => Guardar();
            AcceptButton = btnGuardar;

            var btnCancelar = new Button
            {
                Text = _obligatorio ? "Salir" : "Cancelar",
                FlatStyle = FlatStyle.Flat,
                BackColor = Colores.BlancoPuro,
                ForeColor = Colores.GrisOscuro,
                Font = new Font("Segoe UI", 10),
                Size = new Size(anchoBoton, 40),
                Location = new Point(margen + anchoBoton + 12, y),
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(219, 224, 230);
            CancelButton = btnCancelar;

            Controls.Add(btnGuardar);
            Controls.Add(btnCancelar);
        }

        private int AgregarCampo(string etiqueta, CampoSubrayado campo, int x, int y, int ancho)
        {
            var lbl = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Colores.GrisOscuro,
                AutoSize = true,
                Location = new Point(x, y)
            };
            Controls.Add(lbl);

            campo.Width = ancho;
            campo.Location = new Point(x, lbl.Bottom + 4);
            Controls.Add(campo);

            return campo.Bottom + 14;
        }
    }
}
