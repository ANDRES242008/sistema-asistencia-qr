using System.Drawing;

namespace AsistenciaQR.UI
{
    /// <summary>
    /// Paleta de colores institucional del sistema AsistenciaQR. Un
    /// solo lugar para todos los colores, para que el diseno quede
    /// consistente en todas las pantallas de la Fase 4.
    /// </summary>
    public static class Colores
    {
        public static readonly Color AzulMarino = ColorTranslator.FromHtml("#1B2A4A");
        public static readonly Color VerdeEsmeralda = ColorTranslator.FromHtml("#10B981");
        public static readonly Color AzulInformativo = ColorTranslator.FromHtml("#E0F2FE");
        public static readonly Color GrisClaro = ColorTranslator.FromHtml("#F8FAFC");
        public static readonly Color BlancoPuro = ColorTranslator.FromHtml("#FFFFFF");
        public static readonly Color GrisOscuro = ColorTranslator.FromHtml("#1E293B");
        public static readonly Color GrisSecundario = ColorTranslator.FromHtml("#64748B");

        // Complementa el degradado del boton principal (verde -> azul).
        public static readonly Color AzulDegradado = ColorTranslator.FromHtml("#3B82F6");
    }
}
