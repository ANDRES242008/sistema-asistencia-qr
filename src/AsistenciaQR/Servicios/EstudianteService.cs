using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Logica de negocio del CRUD de estudiantes: valida los datos,
    /// copia la foto elegida a la carpeta administrada por el sistema,
    /// y traduce errores de base de datos (como NIE duplicado) en
    /// mensajes claros para el usuario.
    /// </summary>
    public class EstudianteService
    {
        private readonly EstudianteRepository _estudianteRepository = new();
        private readonly string _carpetaFotos;

        public EstudianteService()
        {
            // Igual que QR y Carnets: las fotos reales de estudiantes
            // nunca se guardan dentro del repositorio de Git.
            _carpetaFotos = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AsistenciaQR", "Fotos");

            Directory.CreateDirectory(_carpetaFotos);
        }

        public List<Estudiante> Buscar(string? texto, string? seccion, bool incluirInactivos)
        {
            return _estudianteRepository.Listar(texto, seccion, incluirInactivos);
        }

        public List<string> ObtenerSecciones() => _estudianteRepository.ObtenerSeccionesDisponibles();

        public ResultadoOperacion Registrar(Estudiante estudiante, string? rutaFotoOrigen)
        {
            var validacion = Validar(estudiante);
            if (!validacion.Exitoso) return validacion;

            if (!string.IsNullOrEmpty(rutaFotoOrigen))
            {
                estudiante.FotoRuta = GuardarFoto(rutaFotoOrigen, estudiante.NIE);
            }

            try
            {
                estudiante.EstudianteId = _estudianteRepository.Insertar(estudiante);
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                return ResultadoOperacion.Error("Ya existe un estudiante registrado con ese NIE.");
            }

            return ResultadoOperacion.Ok("Estudiante registrado correctamente.");
        }

        public ResultadoOperacion Editar(Estudiante estudiante, string? rutaFotoOrigen)
        {
            var validacion = Validar(estudiante);
            if (!validacion.Exitoso) return validacion;

            if (!string.IsNullOrEmpty(rutaFotoOrigen))
            {
                estudiante.FotoRuta = GuardarFoto(rutaFotoOrigen, estudiante.NIE);
            }

            _estudianteRepository.Actualizar(estudiante);

            return ResultadoOperacion.Ok("Cambios guardados correctamente.");
        }

        public ResultadoOperacion DarDeBaja(int estudianteId)
        {
            _estudianteRepository.DarDeBaja(estudianteId);
            return ResultadoOperacion.Ok("Estudiante dado de baja.");
        }

        public ResultadoOperacion Reactivar(int estudianteId)
        {
            _estudianteRepository.Reactivar(estudianteId);
            return ResultadoOperacion.Ok("Estudiante reactivado.");
        }

        private string GuardarFoto(string rutaOrigen, string nie)
        {
            string extension = Path.GetExtension(rutaOrigen);
            string rutaDestino = Path.Combine(_carpetaFotos, $"{nie}{extension}");

            File.Copy(rutaOrigen, rutaDestino, overwrite: true);

            return rutaDestino;
        }

        private static ResultadoOperacion Validar(Estudiante estudiante)
        {
            if (string.IsNullOrWhiteSpace(estudiante.NIE) ||
                !estudiante.NIE.All(char.IsDigit) ||
                estudiante.NIE.Length < 6 || estudiante.NIE.Length > 10)
            {
                return ResultadoOperacion.Error("El NIE debe tener entre 6 y 10 digitos numericos.");
            }

            if (string.IsNullOrWhiteSpace(estudiante.NombreCompleto) || estudiante.NombreCompleto.Trim().Length < 3)
            {
                return ResultadoOperacion.Error("Escribe el nombre completo del estudiante.");
            }

            if (string.IsNullOrWhiteSpace(estudiante.Grado))
            {
                return ResultadoOperacion.Error("El grado es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(estudiante.Seccion))
            {
                return ResultadoOperacion.Error("La seccion es obligatoria.");
            }

            if (!string.IsNullOrWhiteSpace(estudiante.Correo) &&
                (!estudiante.Correo.Contains('@') || !estudiante.Correo.Contains('.')))
            {
                return ResultadoOperacion.Error("El correo no tiene un formato valido.");
            }

            return ResultadoOperacion.Ok();
        }
    }
}
