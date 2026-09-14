using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaQR.UI
{
   
    public class CampoSubrayado : Panel
    {
        private readonly TextBox _txt;
        private readonly Panel _linea;

        public CampoSubrayado()
        {
            Height = 30;
            BackColor = Colores.BlancoPuro;

            _txt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                ForeColor = Colores.GrisOscuro,
                BackColor = Colores.BlancoPuro,
                Location = new Point(0, 2)
            };

            _linea = new Panel
            {
                Height = 1,
                BackColor = Color.FromArgb(219, 224, 230),
                Dock = DockStyle.Bottom
            };

            Controls.Add(_txt);
            Controls.Add(_linea);
            Resize += (_, _) => _txt.Width = Width;
        }

        public string Texto
        {
            get => _txt.Text;
            set => _txt.Text = value;
        }

        public bool Habilitado
        {
            get => _txt.Enabled;
            set
            {
                _txt.Enabled = value;
                _txt.ForeColor = value ? Colores.GrisOscuro : Colores.GrisSecundario;
            }
        }

        public void EscalarA(float escala)
        {
            Height = (int)(30 * escala);
            _txt.Font = new Font("Segoe UI", 10 * escala);
            _txt.Location = new Point(0, (int)(2 * escala));
            _txt.Width = Width;
        }

        internal void Limpiar()
        {

            _txt.Clear();
        }
    }
}
