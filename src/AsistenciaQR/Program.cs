namespace AsistenciaQR
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--probar-nucleo")
            {
                int codigoSalida = PruebasNucleoRunner.Ejecutar();
                Environment.ExitCode = codigoSalida;
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // FrmLogin ya NO es la ventana principal de la app: se
            // muestra como dialogo (ShowDialog). Solo si el login es
            // correcto arranca la app de verdad con FrmDashboard como
            // ventana principal. Antes, cerrar FrmLogin despues de
            // abrir el Dashboard mataba toda la aplicacion (cerrar la
            // ventana de Application.Run termina el proceso completo).
            using var login = new FrmLogin();
            if (login.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new FrmDashboard());
            }
        }
    }
}