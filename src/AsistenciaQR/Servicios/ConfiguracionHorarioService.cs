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
            var anterior = ObtenerActual();
            _repository.Guardar(horaInicio, minutosTolerancia);

            bool cambio = anterior.HoraInicio != horaInicio || anterior.MinutosTolerancia != minutosTolerancia;
            if (cambio) BitacoraService.Registrar(BitacoraService.HorarioCambiado, BitacoraService.DescribirCambios(
                ("Inicio", anterior.HoraInicio.ToString(@"hh\:mm"), horaInicio.ToString(@"hh\:mm")),
                ("Tolerancia", $"{anterior.MinutosTolerancia} min", $"{minutosTolerancia} min")));
        }

        public bool EsTarde(TimeSpan horaRegistro)
        {
            var configuracion = ObtenerActual();
            return EsTarde(horaRegistro, configuracion.HoraInicio, configuracion.MinutosTolerancia);
        }

        /// <summary>
        /// Regla pura de puntualidad, sin base de datos: llegar justo
        /// en el limite (inicio + tolerancia) todavia cuenta a tiempo.
        /// </summary>
        public static bool EsTarde(TimeSpan horaRegistro, TimeSpan horaInicio, int minutosTolerancia)
        {
            var limite = horaInicio + TimeSpan.FromMinutes(minutosTolerancia);
            return horaRegistro > limite;
        }
    }
}
