using Microsoft.Data.SqlClient;

namespace AsistenciaQR.Repositorios
{
  
    public static class ConexionSQL
    {
        private const string CadenaConexion =
            @"Server=ANDRES\SQLEXPRESS;Database=AsistenciaQR;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection ObtenerConexion()
        {
            return new SqlConnection(CadenaConexion);
        }
    }
}
