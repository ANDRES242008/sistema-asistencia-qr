using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// Acceso a datos de la tabla QR.
    /// </summary>
    public class QRRepository
    {
        public void Insertar(Qr qr)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                insert into QR (EstudianteId, CodigoQR, RutaImagen, FechaGeneracion, Activo)
                values (@EstudianteId, @CodigoQR, @RutaImagen, @FechaGeneracion, @Activo);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@EstudianteId", qr.EstudianteId);
            comando.Parameters.AddWithValue("@CodigoQR", qr.CodigoQR);
            comando.Parameters.AddWithValue("@RutaImagen", (object?)qr.RutaImagen ?? DBNull.Value);
            comando.Parameters.AddWithValue("@FechaGeneracion", qr.FechaGeneracion);
            comando.Parameters.AddWithValue("@Activo", qr.Activo);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        public Qr? ObtenerQrActivo(int estudianteId)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select QRId, EstudianteId, CodigoQR, RutaImagen, FechaGeneracion, Activo
                from QR
                where EstudianteId = @EstudianteId and Activo = 1;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@EstudianteId", estudianteId);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            return lector.Read() ? LeerQr(lector) : null;
        }

        public Qr? ObtenerPorCodigo(string codigoQR)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select QRId, EstudianteId, CodigoQR, RutaImagen, FechaGeneracion, Activo
                from QR
                where CodigoQR = @CodigoQR and Activo = 1;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@CodigoQR", codigoQR);

            conexion.Open();
            using var lector = comando.ExecuteReader();

            return lector.Read() ? LeerQr(lector) : null;
        }

        public void InactivarQr(int qrId)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = "update QR set Activo = 0 where QRId = @QRId;";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@QRId", qrId);

            conexion.Open();
            comando.ExecuteNonQuery();
        }

        private static Qr LeerQr(SqlDataReader lector)
        {
            return new Qr
            {
                QRId = lector.GetInt32(0),
                EstudianteId = lector.GetInt32(1),
                CodigoQR = lector.GetString(2),
                RutaImagen = lector.IsDBNull(3) ? null : lector.GetString(3),
                FechaGeneracion = lector.GetDateTime(4),
                Activo = lector.GetBoolean(5)
            };
        }
    }
}
