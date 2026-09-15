using System;

namespace AsistenciaQR.Servicios
{
    /// <summary>
    /// Guarda quien inicio sesion en este proceso (rol, usuario,
    /// nombre). Se llena UNA VEZ en FrmLogin, justo al validar el
    /// login correcto. BarraLateral la lee para decidir que botones
    /// mostrar segun el rol.
    /// </summary>
    public static class SesionActual
    {
        public static string? NombreUsuario { get; set; }
        public static string? NombreCompleto { get; set; }
        public static string? Rol { get; set; }

        public static bool EsAdministrador =>
            string.Equals(Rol, "Administrador", StringComparison.OrdinalIgnoreCase);

        public static void Limpiar()
        {
            NombreUsuario = null;
            NombreCompleto = null;
            Rol = null;
        }
    }
}
