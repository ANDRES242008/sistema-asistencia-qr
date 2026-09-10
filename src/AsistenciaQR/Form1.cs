using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;
using AsistenciaQR.Servicios;
using Microsoft.Data.SqlClient;

namespace AsistenciaQR
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            string cadenaConexion = @"Server=ANDRES\SQLEXPRESS;Database=AsistenciaQR;Trusted_Connection=True;TrustServerCertificate=True;";

            using (SqlConnection conexion = new SqlConnection(cadenaConexion))
            {
                try
                {
                    conexion.Open();
                    MessageBox.Show("Conexión exitosa a la base de datos");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error de conexión: " + ex.Message);
                }
            }
        }

        private void btnGenerarQR_Click_Click(object sender, EventArgs e)
        {
            var estudianteRepo = new EstudianteRepository();
            var estudiante = estudianteRepo.ObtenerPorNIE("99999999");

            if (estudiante is null)
            {
                MessageBox.Show("No se encontró el estudiante de prueba.");
                return;
            }

            var qrService = new QRService();
            var qrGenerado = qrService.GenerarQrParaEstudiante(estudiante);

            MessageBox.Show($"QR generado correctamente.\nGuardado en: {qrGenerado.RutaImagen}");
        }

        private void btnGenerarCarnet_Click_Click(object sender, EventArgs e)
        {
            var estudianteRepo = new EstudianteRepository();
            var estudiante = estudianteRepo.ObtenerPorNIE("99999999");

            if (estudiante is null)
            {
                MessageBox.Show("No se encontro el estudiante de prueba.");
                return;
            }

            var qrRepository = new QRRepository();
            var qrActivo = qrRepository.ObtenerQrActivo(estudiante.EstudianteId);

            if (qrActivo is null)
            {
                MessageBox.Show("Este estudiante todavia no tiene QR. Genera uno primero con el otro boton.");
                return;
            }

            var carnetService = new CarnetService();
            string rutaImagen = carnetService.GenerarImagenCarnet(estudiante, qrActivo);
            string rutaPdf = carnetService.ExportarCarnetComoPdf(rutaImagen);

            MessageBox.Show($"Carnet generado.\n\nImagen: {rutaImagen}\nPDF: {rutaPdf}");
        }

        private void btnAbrirKiosco_Click_Click(object sender, EventArgs e)
        {
            var kiosco = new FrmKioscoEscaneo();
            kiosco.Show();
        }

        private void btnAbrirEstudiantes_Click_Click(object sender, EventArgs e)
        {
            var frm = new FrmEstudiantes();
            frm.Show();
        }

        private void btnAbrirDashboard_Click_Click(object sender, EventArgs e)
        {
            var frm = new FrmDashboard();
            frm.Show();
        }

        private void btnAbrirReportes_Click_Click(object sender, EventArgs e)
        {
            var frm = new FrmReportes();
            frm.Show();
        }

        private void btnAbrirCarnetsQR_Click_Click(object sender, EventArgs e)
        {
            var frm = new FrmCarnetsQR();
            frm.Show();
        }

        private void btnCrearAdmin_Click_Click(object sender, EventArgs e)
        {
            var authService = new AuthService();
            var resultado = authService.Registrar("admin", "Administrador", "admin123", "Administrador");
            MessageBox.Show(resultado.Mensaje);
        }

        private void btnAbrirExcepciones_Click(object sender, EventArgs e)
        {
            var frm = new FrmExcepciones();
            frm.Show();
        }

        private void btnAbrirConfiguracion_Click(object sender, EventArgs e)
        {
            var frm = new FrmConfiguracion();
            frm.Show();
        }

        private void btnAbrirHorarios_Click(object sender, EventArgs e)
        {
            var frm = new FrmHorarios();
            frm.Show();
        }
    }

}