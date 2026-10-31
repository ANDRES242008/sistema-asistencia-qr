using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Carga masiva de estudiantes desde Excel. El flujo es:
    /// 1. GenerarPlantilla: archivo con las columnas esperadas.
    /// 2. LeerArchivo: convierte cada fila en estudiante y la revisa
    ///    con las mismas reglas que el formulario (EstudianteService),
    ///    mas NIE repetido en el archivo o ya registrado.
    /// 3. Importar: guarda solo las filas validas, todas juntas en
    ///    una transaccion.
    /// La foto y el QR se agregan despues, desde sus pantallas.
    /// </summary>
    public class ImportacionEstudiantesService
    {
        public const string NombreHoja = "Estudiantes";

        private static readonly string[] Encabezados = { "NIE", "Nombre completo", "Grado", "Seccion", "Correo" };

        private readonly EstudianteRepository _estudianteRepository = new();

        public static void GenerarPlantilla(string rutaDestino)
        {
            using var libro = new XLWorkbook();

            var hoja = libro.Worksheets.Add(NombreHoja);
            for (int i = 0; i < Encabezados.Length; i++)
            {
                var celda = hoja.Cell(1, i + 1);
                celda.Value = Encabezados[i];
                celda.Style.Font.Bold = true;
                celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2A4A");
                celda.Style.Font.FontColor = XLColor.White;
            }

            // NIE como texto: si Excel lo trata como numero, borra los
            // ceros a la izquierda.
            hoja.Column(1).Style.NumberFormat.Format = "@";
            hoja.Column(1).Width = 14;
            hoja.Column(2).Width = 38;
            hoja.Column(3).Width = 12;
            hoja.Column(4).Width = 10;
            hoja.Column(5).Width = 32;
            hoja.SheetView.FreezeRows(1);

            var instrucciones = libro.Worksheets.Add("Instrucciones");
            string[] lineas =
            {
                "Como llenar la hoja Estudiantes",
                "",
                "- Una fila por estudiante, a partir de la fila 2. No cambies los encabezados.",
                "- NIE: entre 6 y 10 digitos, sin guiones. No puede repetirse.",
                "- Nombre completo: nombres y apellidos.",
                "- Grado y Seccion: obligatorios (ejemplo: B2DA y A).",
                "- Correo: opcional.",
                "",
                "Ejemplo:  12345678 | Valeria Martinez Lopez | B2DA | A | valeria@ejemplo.com",
                "",
                "Las filas con errores se muestran en rojo antes de importar y no se guardan."
            };
            for (int i = 0; i < lineas.Length; i++) instrucciones.Cell(i + 1, 1).Value = lineas[i];
            instrucciones.Cell(1, 1).Style.Font.Bold = true;
            instrucciones.Column(1).Width = 100;

            hoja.SetTabActive();
            libro.SaveAs(rutaDestino);
        }

        /// <summary>
        /// Lee y revisa el archivo. Si no tiene el formato esperado
        /// devuelve el motivo en mensajeError y una lista vacia.
        /// </summary>
        public List<FilaImportacion> LeerArchivo(string ruta, out string? mensajeError)
        {
            List<FilaImportacion> filas;
            try
            {
                using var libro = new XLWorkbook(ruta);
                filas = LeerFilas(libro, out mensajeError);
            }
            catch (IOException)
            {
                mensajeError = "El archivo esta abierto en otro programa. Cierralo e intenta de nuevo.";
                return new List<FilaImportacion>();
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentException or FormatException)
            {
                mensajeError = "El archivo no es un Excel valido (.xlsx).";
                return new List<FilaImportacion>();
            }

            if (mensajeError is null)
            {
                Revisar(filas, _estudianteRepository.ObtenerTodosLosNie());
            }

            return filas;
        }

        public ResultadoOperacion Importar(IEnumerable<FilaImportacion> filas)
        {
            var estudiantes = filas.Where(f => f.EsValida).Select(f => f.Estudiante).ToList();
            if (estudiantes.Count == 0)
            {
                return ResultadoOperacion.Error("No hay filas validas para importar.");
            }

            try
            {
                _estudianteRepository.InsertarVarios(estudiantes);
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                return ResultadoOperacion.Error(
                    "Alguno de los NIE ya fue registrado mientras revisabas el archivo. " +
                    "No se guardo ningun estudiante; vuelve a cargar el archivo.");
            }

            return ResultadoOperacion.Ok($"Se importaron {estudiantes.Count} estudiantes.");
        }

        internal static List<FilaImportacion> LeerFilas(XLWorkbook libro, out string? mensajeError)
        {
            mensajeError = null;
            var filas = new List<FilaImportacion>();

            var hoja = libro.Worksheets.FirstOrDefault(h => Normalizar(h.Name) == Normalizar(NombreHoja))
                       ?? libro.Worksheets.First();

            var columnas = UbicarColumnas(hoja.Row(1));
            var faltantes = new[] { "nie", "nombre", "grado", "seccion" }.Where(c => !columnas.ContainsKey(c)).ToList();
            if (faltantes.Count > 0)
            {
                mensajeError = "La primera fila debe tener los encabezados NIE, Nombre completo, Grado y Seccion. " +
                               "Descarga la plantilla para ver el formato.";
                return filas;
            }

            int ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;
            for (int numero = 2; numero <= ultimaFila; numero++)
            {
                var fila = hoja.Row(numero);
                string Leer(string clave) => columnas.TryGetValue(clave, out int col) ? LeerCelda(fila.Cell(col)) : string.Empty;

                var estudiante = new Estudiante
                {
                    NIE = Leer("nie"),
                    NombreCompleto = Regex.Replace(Leer("nombre"), @"\s+", " "),
                    Grado = Leer("grado"),
                    Seccion = Leer("seccion"),
                    Correo = string.IsNullOrWhiteSpace(Leer("correo")) ? null : Leer("correo"),
                    Activo = true
                };

                bool vacia = string.IsNullOrEmpty(estudiante.NIE) && string.IsNullOrEmpty(estudiante.NombreCompleto) &&
                             string.IsNullOrEmpty(estudiante.Grado) && string.IsNullOrEmpty(estudiante.Seccion);
                if (vacia) continue;

                filas.Add(new FilaImportacion { NumeroFila = numero, Estudiante = estudiante });
            }

            if (filas.Count == 0)
            {
                mensajeError = "El archivo no tiene estudiantes debajo de los encabezados.";
            }

            return filas;
        }

        /// <summary>
        /// Marca el primer error de cada fila: datos invalidos, NIE
        /// repetido dentro del archivo o NIE ya registrado.
        /// </summary>
        internal static void Revisar(List<FilaImportacion> filas, ISet<string> niesRegistrados)
        {
            var primeraFilaPorNie = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var fila in filas)
            {
                var validacion = EstudianteService.Validar(fila.Estudiante);
                if (!validacion.Exitoso)
                {
                    fila.Error = validacion.Mensaje;
                }
                else if (niesRegistrados.Contains(fila.Estudiante.NIE))
                {
                    fila.Error = "Ya existe un estudiante registrado con ese NIE.";
                }
                else if (primeraFilaPorNie.TryGetValue(fila.Estudiante.NIE, out int filaAnterior))
                {
                    fila.Error = $"NIE repetido: ya aparece en la fila {filaAnterior}.";
                }

                if (!string.IsNullOrEmpty(fila.Estudiante.NIE))
                {
                    primeraFilaPorNie.TryAdd(fila.Estudiante.NIE, fila.NumeroFila);
                }
            }
        }

        private static Dictionary<string, int> UbicarColumnas(IXLRow encabezado)
        {
            var columnas = new Dictionary<string, int>();
            foreach (var celda in encabezado.CellsUsed())
            {
                string? clave = Normalizar(celda.GetString()) switch
                {
                    "nie" => "nie",
                    "nombre" or "nombre completo" or "nombres" => "nombre",
                    "grado" => "grado",
                    "seccion" => "seccion",
                    "correo" or "correo electronico" or "email" => "correo",
                    _ => null
                };

                if (clave is not null) columnas.TryAdd(clave, celda.Address.ColumnNumber);
            }
            return columnas;
        }

        private static string LeerCelda(IXLCell celda)
        {
            var valor = celda.Value;
            if (valor.IsBlank) return string.Empty;

            // Un NIE escrito como numero llega como 12345678.0
            if (valor.IsNumber)
            {
                return valor.GetNumber().ToString("0.##########", CultureInfo.InvariantCulture);
            }

            return celda.GetFormattedString().Trim();
        }

        /// <summary>Minusculas y sin tildes, para aceptar "Sección" o "SECCION".</summary>
        private static string Normalizar(string texto)
        {
            var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sinTildes = descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return new string(sinTildes.ToArray()).Normalize(NormalizationForm.FormC);
        }
    }
}
