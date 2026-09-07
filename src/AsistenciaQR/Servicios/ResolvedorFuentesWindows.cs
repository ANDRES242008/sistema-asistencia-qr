using PdfSharp.Fonts;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// PdfSharp moderno ya no toma las fuentes de Windows automaticamente,
    /// hay que decirle explicitamente donde esta el archivo .ttf de cada
    /// fuente. Esta clase apunta directo a la carpeta de fuentes de Windows.
    /// </summary>
    public class ResolvedorFuentesWindows : IFontResolver
    {
        public byte[] GetFont(string faceName)
        {
            string ruta = faceName == "SegoeUI#Bold"
                ? @"C:\Windows\Fonts\segoeuib.ttf"
                : @"C:\Windows\Fonts\segoeui.ttf";

            return File.ReadAllBytes(ruta);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            string faceName = isBold ? "SegoeUI#Bold" : "SegoeUI";
            return new FontResolverInfo(faceName);
        }
    }
}
