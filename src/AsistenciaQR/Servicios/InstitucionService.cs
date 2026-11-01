using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Nombre, logo y color de la institucion. Mientras no se configure,
    /// el sistema usa un nombre generico y el emblema por defecto, asi
    /// el mismo programa sirve para cualquier colegio.
    /// </summary>
    public class InstitucionService
    {
        public const string NombrePorDefecto = "Mi Institucion";
        public const int LargoMaximoNombre = 40;
        public const int LadoMaximoLogo = 512;
        public const long PesoMaximoArchivoLogo = 5 * 1024 * 1024;

        private readonly InstitucionRepository _repositorio = new();

        // Se lee muchas veces (cada carnet, cada pantalla con menu);
        // se guarda en memoria y se refresca al guardar cambios.
        private static ConfiguracionInstitucion? _cache;

        public ConfiguracionInstitucion Obtener()
        {
            if (_cache is not null) return _cache;

            try
            {
                _cache = _repositorio.Obtener() ?? PorDefecto();
            }
            catch (Microsoft.Data.SqlClient.SqlException)
            {
                // Sin base de datos (o sin la tabla todavia) no se rompe
                // nada: se muestran los valores por defecto.
                return PorDefecto();
            }

            return _cache;
        }

        public ResultadoOperacion Guardar(string nombre, byte[]? logo, string? colorPrincipal)
        {
            var validacion = Validar(nombre, colorPrincipal);
            if (!validacion.Exitoso) return validacion;

            var anterior = Obtener();
            var nueva = new ConfiguracionInstitucion
            {
                Nombre = nombre.Trim(),
                Logo = logo,
                ColorPrincipal = colorPrincipal?.ToUpperInvariant()
            };

            _repositorio.Guardar(nueva);
            _cache = nueva;

            BitacoraService.Registrar(BitacoraService.InstitucionActualizada, BitacoraService.DescribirCambios(
                ("Nombre", anterior.Nombre, nueva.Nombre),
                ("Logo", anterior.Logo is null ? null : "si", nueva.Logo is null ? null :
                    (anterior.Logo is not null && anterior.Logo.AsSpan().SequenceEqual(nueva.Logo) ? "si" : "nuevo")),
                ("Color", anterior.ColorPrincipal, nueva.ColorPrincipal)));

            return ResultadoOperacion.Ok("Datos de la institucion guardados. Los carnets nuevos ya los usan.");
        }

        public static ConfiguracionInstitucion PorDefecto() => new() { Nombre = NombrePorDefecto };

        internal static ResultadoOperacion Validar(string nombre, string? colorPrincipal)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return ResultadoOperacion.Error("Escribe el nombre de la institucion.");
            }

            if (nombre.Trim().Length > LargoMaximoNombre)
            {
                return ResultadoOperacion.Error(
                    $"El nombre puede tener hasta {LargoMaximoNombre} caracteres (debe caber en el carnet). Usa una forma corta.");
            }

            if (colorPrincipal is not null && LeerColor(colorPrincipal) is null)
            {
                return ResultadoOperacion.Error("El color no es valido.");
            }

            return ResultadoOperacion.Ok();
        }

        /// <summary>
        /// Prepara la imagen elegida como logo: la valida, la reduce a
        /// 512 px como maximo y la convierte a PNG. Devuelve null y un
        /// motivo si el archivo no sirve.
        /// </summary>
        public static byte[]? PrepararLogo(string rutaArchivo, out string? motivo)
        {
            motivo = null;
            if (new FileInfo(rutaArchivo).Length > PesoMaximoArchivoLogo)
            {
                motivo = "La imagen pesa mas de 5 MB. Usa una mas liviana.";
                return null;
            }

            try
            {
                return PrepararLogo(File.ReadAllBytes(rutaArchivo));
            }
            catch (ArgumentException)
            {
                motivo = "El archivo no es una imagen valida (usa PNG o JPG).";
                return null;
            }
        }

        internal static byte[] PrepararLogo(byte[] imagenOriginal)
        {
            using var entrada = new MemoryStream(imagenOriginal);
            using var original = Image.FromStream(entrada);

            float escala = Math.Min(1f, (float)LadoMaximoLogo / Math.Max(original.Width, original.Height));
            int ancho = Math.Max((int)(original.Width * escala), 1);
            int alto = Math.Max((int)(original.Height * escala), 1);

            using var reducida = new Bitmap(ancho, alto, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(reducida))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(original, 0, 0, ancho, alto);
            }

            using var salida = new MemoryStream();
            reducida.Save(salida, ImageFormat.Png);
            return salida.ToArray();
        }

        public static string ConvertirColor(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        public static Color? LeerColor(string? texto)
        {
            if (texto is null || texto.Length != 7 || texto[0] != '#') return null;
            return int.TryParse(texto.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb)
                ? Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF)
                : null;
        }

        /// <summary>Mezcla un color con negro: factor 0.2 = 20% mas oscuro.</summary>
        public static Color Oscurecer(Color color, float factor) => Color.FromArgb(
            (int)(color.R * (1 - factor)), (int)(color.G * (1 - factor)), (int)(color.B * (1 - factor)));
    }
}
