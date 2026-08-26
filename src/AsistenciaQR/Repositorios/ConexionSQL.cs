using Microsoft.Data.SqlClient;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// Punto unico donde vive la cadena de conexion a SQL Server.
    /// Todos los repositorios usan esta clase en vez de repetir
    /// la cadena de conexion en cada archivo.
    /// </summary>
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
