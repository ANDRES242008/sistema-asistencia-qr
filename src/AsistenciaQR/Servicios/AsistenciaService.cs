using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Logica de negocio para registrar asistencia. Se usa tanto desde
    /// el escaneo automatico de QR como desde el respaldo manual (NIE).
    /// Ambos caminos terminan en el mismo metodo privado, para no
    /// duplicar las reglas de negocio en dos lugares.
    /// </summary>
    public class AsistenciaService
    {
        private readonly QRRepository _qrRepository = new();
        private readonly EstudianteRepository _estudianteRepository = new();
        private readonly AsistenciaRepository _asistenciaRepository = new();

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

        public ResultadoAsistencia RegistrarPorNie(string nie)
        {
            var estudiante = _estudianteRepository.ObtenerPorNIE(nie);
            if (estudiante is null || !estudiante.Activo)
            {
                return new ResultadoAsistencia { Estado = ResultadoEscaneo.EstudianteNoEncontrado };
            }

            return RegistrarAsistenciaEstudiante(estudiante);
        }

        private ResultadoAsistencia RegistrarAsistenciaEstudiante(Estudiante estudiante)
        {
            if (_asistenciaRepository.ExisteAsistenciaHoy(estudiante.EstudianteId))
            {
                return new ResultadoAsistencia
                {
                    Estado = ResultadoEscaneo.YaRegistradoHoy,
                    Estudiante = estudiante
                };
            }

            try
            {
                _asistenciaRepository.RegistrarAsistencia(estudiante.EstudianteId);
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
                HoraRegistro = DateTime.Now
            };
        }
    }
}
