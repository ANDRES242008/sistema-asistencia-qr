using System.Globalization;
using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Respaldos de la base de datos. Se guardan en
    /// C:\ProgramData\AsistenciaQR\Respaldos, una carpeta donde el
    /// servicio de SQL Server puede escribir y la app puede leer (para
    /// copiarlos a una USB). Ademas del boton manual, se hace un
    /// respaldo automatico al dia y se conservan los ultimos 7.
    /// </summary>
    public class RespaldoService
    {
        public const int AutomaticosAConservar = 7;
        public const int PreviosARestaurarAConservar = 3;

        private const string FormatoFecha = "yyyy-MM-dd_HHmmss";

        public static string Carpeta { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AsistenciaQR", "Respaldos");

        public RespaldoService()
        {
            Directory.CreateDirectory(Carpeta);
        }

        public List<RespaldoInfo> Listar()
        {
            return Directory.GetFiles(Carpeta, "*.bak")
                .Select(ruta => (ruta, info: InterpretarNombre(Path.GetFileName(ruta))))
                .Where(x => x.info is not null)
                .Select(x => new RespaldoInfo
                {
                    Ruta = x.ruta,
                    Fecha = x.info!.Value.Fecha,
                    Tipo = x.info.Value.Tipo,
                    TamanoBytes = new FileInfo(x.ruta).Length
                })
                .OrderByDescending(r => r.Fecha)
                .ToList();
        }

        public RespaldoInfo Crear(TipoRespaldo tipo)
        {
            var ahora = DateTime.Now;
            string ruta = Path.Combine(Carpeta, NombreArchivo(ahora, tipo));
            RespaldoRepository.Respaldar(ruta);

            // El automatico lo hace el sistema; el "antes de restaurar"
            // queda descrito en el registro de la restauracion.
            if (tipo == TipoRespaldo.Manual)
            {
                BitacoraService.Registrar(BitacoraService.RespaldoCreado, Path.GetFileName(ruta));
            }

            return new RespaldoInfo { Ruta = ruta, Fecha = ahora, Tipo = tipo, TamanoBytes = new FileInfo(ruta).Length };
        }

        /// <summary>
        /// Se llama al abrir el programa. Si hoy todavia no hay
        /// respaldo automatico, lo crea, y borra los mas viejos.
        /// Nunca lanza excepciones: un respaldo fallido no debe
        /// impedir que el colegio tome asistencia.
        /// </summary>
        public static void RespaldoAutomaticoSiCorresponde(Action<Exception> registrarError)
        {
            try
            {
                var servicio = new RespaldoService();
                var respaldos = servicio.Listar();

                if (NecesitaRespaldoAutomatico(respaldos, DateTime.Today))
                {
                    respaldos.Insert(0, servicio.Crear(TipoRespaldo.Automatico));
                }

                foreach (var viejo in SeleccionarParaBorrar(respaldos))
                {
                    servicio.Eliminar(viejo);
                }
            }
            catch (Exception ex)
            {
                registrarError(ex);
            }
        }

        public void Eliminar(RespaldoInfo respaldo)
        {
            RespaldoRepository.Eliminar(respaldo.Ruta);

            // Si el archivo lo puso la app (por ejemplo, uno traido de
            // una USB), SQL Server no lo considera suyo; se borra aqui.
            if (File.Exists(respaldo.Ruta)) File.Delete(respaldo.Ruta);
        }

        public void CopiarA(RespaldoInfo respaldo, string rutaDestino)
        {
            File.Copy(respaldo.Ruta, rutaDestino, overwrite: true);
        }

        /// <summary>
        /// Restaura un respaldo, ya sea de la lista o un .bak traido de
        /// otra parte (que primero se copia a la carpeta de respaldos,
        /// donde SQL Server puede leerlo). Antes de reemplazar nada se
        /// verifica el archivo y se respaldan los datos actuales, para
        /// poder deshacer.
        /// </summary>
        public ResultadoOperacion Restaurar(string rutaArchivo)
        {
            string rutaLocal = rutaArchivo;
            bool esCopiaTemporal = false;
            if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(rutaArchivo)), Path.GetFullPath(Carpeta),
                    StringComparison.OrdinalIgnoreCase))
            {
                // Se guarda como respaldo manual: si la restauracion
                // sale bien, queda en la lista como cualquier otro.
                rutaLocal = Path.Combine(Carpeta, NombreArchivo(DateTime.Now, TipoRespaldo.Manual));
                File.Copy(rutaArchivo, rutaLocal, overwrite: false);
                esCopiaTemporal = true;
            }

            string? motivoRechazo = null;
            try
            {
                string baseEnRespaldo = RespaldoRepository.Verificar(rutaLocal);
                if (!string.Equals(baseEnRespaldo, RespaldoRepository.NombreBaseDatos, StringComparison.OrdinalIgnoreCase))
                {
                    motivoRechazo = $"Ese respaldo es de la base de datos \"{baseEnRespaldo}\", no de AsistenciaQR. No se cambio nada.";
                }
            }
            catch (SqlException)
            {
                motivoRechazo = "El archivo no es un respaldo valido o esta danado. No se cambio nada.";
            }

            if (motivoRechazo is not null)
            {
                // La copia la hizo la app, asi que la app puede borrarla.
                if (esCopiaTemporal) File.Delete(rutaLocal);
                return ResultadoOperacion.Error(motivoRechazo);
            }

            var previo = Crear(TipoRespaldo.AntesDeRestaurar);
            RespaldoRepository.Restaurar(rutaLocal);

            // Un respaldo de una version anterior puede no tener las
            // columnas nuevas (ni la bitacora): se agregan aqui mismo.
            EsquemaRepository.AplicarActualizaciones();

            // Se registra DESPUES de restaurar: la bitacora vieja se
            // reemplazo con la del respaldo, y este registro debe quedar.
            BitacoraService.Registrar(BitacoraService.RespaldoRestaurado,
                $"Se restauro {Path.GetFileName(rutaLocal)}. Los datos anteriores quedaron en {previo.NombreArchivo}");

            return ResultadoOperacion.Ok(
                "Respaldo restaurado. Por si acaso, los datos que habia antes quedaron guardados " +
                "como un respaldo \"Antes de restaurar\".");
        }

        internal static string NombreArchivo(DateTime fecha, TipoRespaldo tipo)
        {
            string sufijo = tipo switch
            {
                TipoRespaldo.Automatico => "auto",
                TipoRespaldo.Manual => "manual",
                _ => "antes-de-restaurar"
            };
            return $"AsistenciaQR_{fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture)}_{sufijo}.bak";
        }

        internal static (DateTime Fecha, TipoRespaldo Tipo)? InterpretarNombre(string nombreArchivo)
        {
            string nombre = Path.GetFileNameWithoutExtension(nombreArchivo);
            const string prefijo = "AsistenciaQR_";
            if (!nombre.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)) return null;

            string resto = nombre[prefijo.Length..];
            if (resto.Length <= FormatoFecha.Length + 1) return null;

            if (!DateTime.TryParseExact(resto[..FormatoFecha.Length], FormatoFecha, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fecha))
            {
                return null;
            }

            TipoRespaldo? tipo = resto[(FormatoFecha.Length + 1)..] switch
            {
                "auto" => TipoRespaldo.Automatico,
                "manual" => TipoRespaldo.Manual,
                "antes-de-restaurar" => TipoRespaldo.AntesDeRestaurar,
                _ => null
            };

            return tipo is null ? null : (fecha, tipo.Value);
        }

        internal static bool NecesitaRespaldoAutomatico(IEnumerable<RespaldoInfo> respaldos, DateTime hoy) =>
            !respaldos.Any(r => r.Tipo == TipoRespaldo.Automatico && r.Fecha.Date == hoy.Date);

        /// <summary>
        /// Los manuales nunca se borran solos. De los automaticos se
        /// conservan los 7 mas recientes y de los "antes de restaurar"
        /// los 3 mas recientes.
        /// </summary>
        internal static List<RespaldoInfo> SeleccionarParaBorrar(IEnumerable<RespaldoInfo> respaldos)
        {
            var lista = respaldos.ToList();

            IEnumerable<RespaldoInfo> Sobrantes(TipoRespaldo tipo, int conservar) =>
                lista.Where(r => r.Tipo == tipo).OrderByDescending(r => r.Fecha).Skip(conservar);

            return Sobrantes(TipoRespaldo.Automatico, AutomaticosAConservar)
                .Concat(Sobrantes(TipoRespaldo.AntesDeRestaurar, PreviosARestaurarAConservar))
                .ToList();
        }
    }
}
