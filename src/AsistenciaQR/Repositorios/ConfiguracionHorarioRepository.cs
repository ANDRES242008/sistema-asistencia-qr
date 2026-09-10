using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// En vez de guardar una sola fila que se sobreescribe, cada
    /// cambio de horario inserta una fila nueva. Esto da un pequeno
    /// historial de cambios "gratis", y ObtenerActual siempre toma
    /// la mas reciente.
    /// </summary>
    public class ConfiguracionHorarioRepository
    {
        public ConfiguracionHorario ObtenerActual()
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                select top 1 ConfiguracionHorarioId, HoraInicio, MinutosTolerancia, FechaActualizacion
                from ConfiguracionHorario
                order by ConfiguracionHorarioId desc;";

            using var comando = new SqlCommand(sql, conexion);
            conexion.Open();
            using var lector = comando.ExecuteReader();

            if (lector.Read())
            {
                return new ConfiguracionHorario
                {
                    ConfiguracionHorarioId = lector.GetInt32(0),
                    HoraInicio = lector.GetTimeSpan(1),
                    MinutosTolerancia = lector.GetInt32(2),
                    FechaActualizacion = lector.GetDateTime(3)
                };
            }

            // Todavia no se ha configurado nada: valor por defecto razonable.
            return new ConfiguracionHorario
            {
                HoraInicio = new TimeSpan(7, 0, 0),
                MinutosTolerancia = 10
            };
        }

        public void Guardar(TimeSpan horaInicio, int minutosTolerancia)
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            const string sql = @"
                insert into ConfiguracionHorario (HoraInicio, MinutosTolerancia)
                values (@HoraInicio, @MinutosTolerancia);";

            using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@HoraInicio", horaInicio);
            comando.Parameters.AddWithValue("@MinutosTolerancia", minutosTolerancia);

            conexion.Open();
            comando.ExecuteNonQuery();
        }
    }
}
