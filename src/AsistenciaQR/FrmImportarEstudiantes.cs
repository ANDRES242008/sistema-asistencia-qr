using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AsistenciaQR.Modelos;
using AsistenciaQR.Servicios;
using AsistenciaQR.UI;

namespace AsistenciaQR
{
    /// <summary>
    /// Vista previa de la importacion desde Excel. Muestra cada fila
    /// en verde (lista) o rojo (con el motivo del error) y solo
    /// guarda las validas. Devuelve OK si se importo algo.
    /// </summary>
    public class FrmImportarEstudiantes : Form
    {
        private readonly ImportacionEstudiantesService _importacionService = new();
        private List<FilaImportacion> _filas = new();

        private Label _lblArchivo = null!;
        private Label _lblResumen = null!;
        private DataGridView _dgvFilas = null!;
        private BotonPrimario _btnImportar = null!;

        public FrmImportarEstudiantes()
        {
            ConstruirInterfaz();
        }

        private void BtnElegirArchivo_Click(object? sender, EventArgs e)
        {
            using var dialogo = new OpenFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                Title = "Elegir archivo de estudiantes"
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            Cursor = Cursors.WaitCursor;
            _filas = _importacionService.LeerArchivo(dialogo.FileName, out string? mensajeError);
            Cursor = Cursors.Default;

            _lblArchivo.Text = Path.GetFileName(dialogo.FileName);
            MostrarFilas();

            if (mensajeError is not null)
            {
                MessageBox.Show(this, mensajeError, "No se pudo leer el archivo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnPlantilla_Click(object? sender, EventArgs e)
        {
            using var dialogo = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = "plantilla_estudiantes.xlsx"
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                ImportacionEstudiantesService.GenerarPlantilla(dialogo.FileName);
            }
            catch (IOException)
            {
                MessageBox.Show(this, "El archivo esta abierto en otro programa. Cierralo o elige otro nombre.",
                    "Archivo en uso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });
        }

        private void MostrarFilas()
        {
            _dgvFilas.Rows.Clear();
            foreach (var fila in _filas)
            {
                var e = fila.Estudiante;
                int indice = _dgvFilas.Rows.Add(fila.NumeroFila, e.NIE, e.NombreCompleto, e.Grado, e.Seccion,
                    e.Correo ?? string.Empty, e.EncargadoCorreo ?? string.Empty, fila.EsValida ? "✔ Lista" : fila.Error);

                var estilo = _dgvFilas.Rows[indice].DefaultCellStyle;
                estilo.BackColor = fila.EsValida ? Color.FromArgb(236, 253, 245) : Color.FromArgb(254, 242, 242);
                estilo.ForeColor = fila.EsValida ? Colores.GrisOscuro : Color.Firebrick;
            }

            int validas = _filas.Count(f => f.EsValida);
            int conError = _filas.Count - validas;

            _lblResumen.Text = _filas.Count == 0
                ? "Elige un archivo para ver la vista previa."
                : $"{validas} listas para importar  ·  {conError} con errores (no se importaran)";
            _lblResumen.ForeColor = conError > 0 ? Color.FromArgb(180, 83, 9) : Colores.VerdeEsmeralda;

            _btnImportar.Enabled = validas > 0;
            _btnImportar.Text = validas > 0 ? $"Importar {validas} estudiantes" : "Importar";
            _btnImportar.Invalidate();
        }

        private void BtnImportar_Click(object? sender, EventArgs e)
        {
            int conError = _filas.Count(f => !f.EsValida);
            if (conError > 0)
            {
                var confirmar = MessageBox.Show(this,
                    $"{conError} filas tienen errores y se omitiran. Puedes corregirlas en Excel e importarlas despues.\n\nContinuar?",
                    "Filas con errores", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirmar != DialogResult.Yes) return;
            }

            Cursor = Cursors.WaitCursor;
            var resultado = _importacionService.Importar(_filas);
            Cursor = Cursors.Default;

            if (!resultado.Exitoso)
            {
                MessageBox.Show(this, resultado.Mensaje, "No se importo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(this,
                resultado.Mensaje + "\n\nPara imprimir sus carnets ve a Carnets y QR y usa \"Carnets de la lista (PDF)\".",
                "Importacion completa", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ConstruirInterfaz()
        {
            Text = "AsistenciaQR - Importar estudiantes desde Excel";
            Size = Escala.Ventana(this, 1000, 640);
            MinimumSize = Escala.Px(this, 800, 460);
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Colores.GrisClaro;
            Padding = new Padding(Escala.Px(this, 20));

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
                Text = "Importar estudiantes",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Colores.AzulMarino,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, Escala.Px(this, 8))
            });

            var filaBotones = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };

            var btnElegir = Escala.Boton("📂  Elegir archivo Excel...");
            btnElegir.Click += BtnElegirArchivo_Click;

            var btnPlantilla = Escala.Boton("⬇  Descargar plantilla");
            btnPlantilla.Margin = new Padding(Escala.Px(this, 10), 0, 0, 0);
            btnPlantilla.Click += BtnPlantilla_Click;

            _lblArchivo = new Label
            {
                ForeColor = Colores.GrisSecundario,
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(Escala.Px(this, 14), 0, 0, 0)
            };

            filaBotones.Controls.AddRange(new Control[] { btnElegir, btnPlantilla, _lblArchivo });
            panelSuperior.Controls.Add(filaBotones);

            _dgvFilas = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Colores.BlancoPuro,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI Emoji", 9.5f),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            _dgvFilas.RowTemplate.Height = Escala.Px(this, 28);
            _dgvFilas.Columns.Add("Fila", "Fila");
            _dgvFilas.Columns.Add("NIE", "NIE");
            _dgvFilas.Columns.Add("Nombre", "Nombre completo");
            _dgvFilas.Columns.Add("Grado", "Grado");
            _dgvFilas.Columns.Add("Seccion", "Seccion");
            _dgvFilas.Columns.Add("Correo", "Correo");
            _dgvFilas.Columns.Add("CorreoEncargado", "Correo del encargado");
            _dgvFilas.Columns.Add("Estado", "Estado");
            _dgvFilas.Columns["Fila"]!.FillWeight = 35;
            _dgvFilas.Columns["Grado"]!.FillWeight = 50;
            _dgvFilas.Columns["Seccion"]!.FillWeight = 62;
            _dgvFilas.Columns["Nombre"]!.FillWeight = 160;
            _dgvFilas.Columns["Estado"]!.FillWeight = 190;

            // Resumen a la izquierda; Cancelar e Importar a la derecha.
            var panelInferior = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                ColumnCount = 3,
                Padding = new Padding(0, Escala.Px(this, 12), 0, 0)
            };
            panelInferior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panelInferior.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelInferior.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _lblResumen = new Label
            {
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            _btnImportar = new BotonPrimario
            {
                Text = "Importar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Padding = new Padding(Escala.Px(this, 24), 0, Escala.Px(this, 24), 0),
                MinimumSize = Escala.Px(this, 200, 42),
                Enabled = false
            };
            _btnImportar.Click += BtnImportar_Click;

            var btnCancelar = Escala.Boton("Cancelar", altoDiseno: 42);
            btnCancelar.Margin = new Padding(0, 0, Escala.Px(this, 10), 0);
            btnCancelar.DialogResult = DialogResult.Cancel;
            CancelButton = btnCancelar;

            panelInferior.Controls.Add(_lblResumen, 0, 0);
            panelInferior.Controls.Add(btnCancelar, 1, 0);
            panelInferior.Controls.Add(_btnImportar, 2, 0);

            Controls.Add(_dgvFilas);
            Controls.Add(panelInferior);
            Controls.Add(panelSuperior);

            MostrarFilas();
        }
    }
}
