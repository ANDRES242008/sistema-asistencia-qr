    using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{
    
    public class CampoTexto : Panel
    {
        private const int RadioBase = 10;

        private readonly Label _lblIcono;
        private readonly TextBox _txt;
        private readonly Label? _lblOjo;
        private readonly string _placeholder;
        private bool _mostrandoPlaceholder = true;
        private float _escala = 1f;

        private bool _contrasenaVisible = false;

        public event Action? EnterPresionado;

        public event Action<string>? TextoCambiado;

        public CampoTexto(string icono, string placeholder, bool esContrasena = false)
        {
            _placeholder = placeholder;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Height = 44;
            BackColor = Colores.BlancoPuro;

            _lblIcono = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI Emoji", 11),
                ForeColor = Colores.GrisSecundario,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                BackColor = Color.Transparent
            };

            _txt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                ForeColor = Colores.GrisSecundario,
                Text = placeholder,
                BackColor = Colores.BlancoPuro
            };

            _txt.GotFocus += (_, _) => LimpiarPlaceholder();
            _txt.LostFocus += (_, _) => RestaurarPlaceholder();
            _txt.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter) EnterPresionado?.Invoke();
            };
            _txt.TextChanged += (_, _) =>
            {
                if (!_mostrandoPlaceholder) TextoCambiado?.Invoke(_txt.Text);
            };

            Controls.Add(_lblIcono);
            Controls.Add(_txt);

            if (esContrasena)
            {
                _lblOjo = new Label
                {
                    Text = "\U0001F441", // Ojo normal
                    Font = new Font("Segoe UI Emoji", 10),
                    ForeColor = Colores.GrisSecundario,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = false,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                _lblOjo.Click += (_, _) => AlternarVisibilidad();
                Controls.Add(_lblOjo);
            }

            Resize += (_, _) => ReubicarControles();
            ReubicarControles();
        }

        public void EscalarA(float escala)
        {
            _escala = escala;

            Height = (int)(44 * escala);
            _lblIcono.Font = new Font("Segoe UI Emoji", 11 * escala);
            _txt.Font = new Font("Segoe UI", 10 * escala);
            if (_lblOjo is not null) _lblOjo.Font = new Font("Segoe UI Emoji", 10 * escala);

            ReubicarControles();
            Invalidate();
        }

        public void Limpiar()
        {
            _mostrandoPlaceholder = true;
            _txt.PasswordChar = '\0';
            _txt.Text = _placeholder;
            _txt.ForeColor = Colores.GrisSecundario;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var area = new Rectangle(0, 0, Width - 1, Height - 1);
            int radio = Math.Max((int)(RadioBase * _escala), 6);
            using var ruta = Formas.Redondeada(area, radio);

            using var fondo = new SolidBrush(Colores.BlancoPuro);
            g.FillPath(fondo, ruta);

            using var borde = new Pen(Color.FromArgb(219, 224, 230), 1);
            g.DrawPath(borde, ruta);
        }

        private void ReubicarControles()
        {
            int iconoAncho = (int)(Height * 0.8);
            _lblIcono.Size = new Size(iconoAncho, Height);
            _lblIcono.Location = new Point(2, 0);

            int inicioTexto = _lblIcono.Right + (int)(4 * _escala);

            if (_lblOjo is not null)
            {
                int ojoAncho = (int)(Height * 0.7);
                _lblOjo.Size = new Size(ojoAncho, Height);
                _lblOjo.Location = new Point(Width - ojoAncho - 2, 0);

                _txt.Location = new Point(inicioTexto, Math.Max((Height - _txt.Height) / 2, 0));
                _txt.Width = Math.Max(_lblOjo.Left - inicioTexto - 4, 40);
            }
            else
            {
                _txt.Location = new Point(inicioTexto, Math.Max((Height - _txt.Height) / 2, 0));
                _txt.Width = Math.Max(Width - inicioTexto - 12, 40);
            }
        }

        private void LimpiarPlaceholder()
        {
            if (IsDisposed || _txt.IsDisposed) return;
            if (!_mostrandoPlaceholder) return;

            _mostrandoPlaceholder = false;
            _txt.Text = string.Empty;
            _txt.ForeColor = Colores.GrisOscuro;

            if (_lblOjo is not null && !_contrasenaVisible)
            {
                _txt.PasswordChar = '●';
            }
        }

        private void RestaurarPlaceholder()
        {
            if (IsDisposed || _txt.IsDisposed) return;
            if (!string.IsNullOrEmpty(_txt.Text)) return;

            _mostrandoPlaceholder = true;
            if (_lblOjo is not null)
            {
                _txt.PasswordChar = '\0';
            }
            _txt.Text = _placeholder;
            _txt.ForeColor = Colores.GrisSecundario;
        }

        private void AlternarVisibilidad()
        {
            if (IsDisposed || _txt.IsDisposed) return;
            if (_mostrandoPlaceholder) return;

            _contrasenaVisible = !_contrasenaVisible;
            _txt.PasswordChar = _contrasenaVisible ? '\0' : '●';

            if (_lblOjo is not null)
            {
                _lblOjo.Text = _contrasenaVisible ? "\U0001F576" : "\U0001F441";
            }
        }

        public string ObtenerValorReal() => _mostrandoPlaceholder ? string.Empty : _txt.Text;

        // ¡Esta es la propiedad CLAVE que la otra IA te había borrado y necesitamos para el Kiosco/Login!
        public override string Text
        {
            get => ObtenerValorReal();
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    RestaurarPlaceholder();
                }
                else
                {
                    _mostrandoPlaceholder = false;
                    _txt.Text = value;
                    _txt.ForeColor = Colores.GrisOscuro;

                    if (_lblOjo is not null && !_contrasenaVisible)
                    {
                        _txt.PasswordChar = '●';
                    }
                }
            }
        }
    }
}