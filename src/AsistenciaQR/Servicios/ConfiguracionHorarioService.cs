using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Logica de puntualidad: compara la hora de un registro contra
    /// la hora de inicio de clases + tolerancia, para clasificarlo
    /// como "A tiempo" o "Tarde".
    /// </summary>
    public class ConfiguracionHorarioService
    {
        private readonly ConfiguracionHorarioRepository _repository = new();

        public ConfiguracionHorario ObtenerActual() => _repository.ObtenerActual();

        public void Guardar(TimeSpan horaInicio, int minutosTolerancia)
        {
            _repository.Guardar(horaInicio, minutosTolerancia);
        }

        public bool EsTarde(TimeSpan horaRegistro)
        {
            var configuracion = ObtenerActual();
            var limite = configuracion.HoraInicio + TimeSpan.FromMinutes(configuracion.MinutosTolerancia);
            return horaRegistro > limite;
        }
    }
}
