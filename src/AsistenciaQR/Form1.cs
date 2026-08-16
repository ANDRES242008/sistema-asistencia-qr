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
    }

}