using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Logica de negocio para registrar asistencia. Se usa desde el
    /// escaneo automatico de QR, el respaldo manual del Kiosco, y la
    /// pantalla dedicada de Registro Manual de Excepciones. Los tres
    /// caminos terminan en el mismo metodo privado, para no duplicar
    /// las reglas de negocio (incluida la clasificacion de puntualidad)
    /// en varios lugares.
    /// </summary>
    public class AsistenciaService
    {
        private readonly QRRepository _qrRepository = new();
        private readonly EstudianteRepository _estudianteRepository = new();
        private readonly AsistenciaRepository _asistenciaRepository = new();
        private readonly ConfiguracionHorarioService _horarioService = new();

        public ResultadoAsistencia RegistrarPorCodigoQr(string codigoQr)
        {
            var qr = _qrRepository.ObtenerPorCodigo(codigoQr);
            if (qr is null)
            {
                return new ResultadoAsistencia { Estado = ResultadoEscaneo.CodigoNoValido };
            }

            var estudiante = _estudianteRepository.ObtenerPorId(qr.EstudianteId);
            if (estudiante is null || !estudiante.Activo)
            {
                return new ResultadoAsistencia { Estado = ResultadoEscaneo.CodigoNoValido };
            }

            return RegistrarAsistenciaEstudiante(estudiante);
        }

        public ResultadoAsistencia RegistrarPorNie(string nie, string? observacion = null)
        {
            var estudiante = _estudianteRepository.ObtenerPorNIE(nie);
            if (estudiante is null || !estudiante.Activo)
            {
                return new ResultadoAsistencia { Estado = ResultadoEscaneo.CodigoNoValido };
            }

            return RegistrarAsistenciaEstudiante(estudiante, observacion);
        }

        private ResultadoAsistencia RegistrarAsistenciaEstudiante(Estudiante estudiante, string? observacion = null)
        {
            if (_asistenciaRepository.ExisteAsistenciaHoy(estudiante.EstudianteId))
            {
                return new ResultadoAsistencia
                {
                    Estado = ResultadoEscaneo.YaRegistradoHoy,
                    Estudiante = estudiante
                };
            }

            var horaActual = DateTime.Now.TimeOfDay;
            bool esTarde = _horarioService.EsTarde(horaActual);
            string estadoPuntualidad = esTarde ? "Tarde" : "A tiempo";

            try
            {
                _asistenciaRepository.RegistrarAsistencia(estudiante.EstudianteId, observacion, estadoPuntualidad);
            }
            catch (SqlException)
            {
                return new ResultadoAsistencia
                {
                    Estado = ResultadoEscaneo.YaRegistradoHoy,
                    Estudiante = estudiante
                };
            }

            return new ResultadoAsistencia
            {
                Estado = ResultadoEscaneo.Exitoso,
                Estudiante = estudiante,
                HoraRegistro = DateTime.Now,
                EsTarde = esTarde
            };
        }
    }
}
