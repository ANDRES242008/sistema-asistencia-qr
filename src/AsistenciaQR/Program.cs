using System.IO;
using System.Threading;

namespace AsistenciaQR
{
    internal static class Program
    {
        // Nombre unico del candado. Si ya existe, el programa ya esta abierto.
        private const string NombreMutex = "AsistenciaQR_InstanciaUnica";
        private const string ArgumentoReinicio = "--reinicio";

        /// <summary>Cierra el programa y lo vuelve a abrir desde el login.</summary>
        public static void Reiniciar()
        {
            System.Diagnostics.Process.Start(Application.ExecutablePath, ArgumentoReinicio);
            Application.Exit();
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--probar-nucleo")
            {
                int codigoSalida = PruebasNucleoRunner.Ejecutar();
                Environment.ExitCode = codigoSalida;
                return;
            }

            // BLINDAJE 2: instancia unica. Si alguien abre el programa dos
            // veces (doble clic por nervios), la segunda copia no arranca,
            // asi no pelea por la camara con la primera.
            using var mutex = new Mutex(true, NombreMutex, out bool esPrimeraInstancia);

            // Al reiniciar (por ejemplo, despues de restaurar un
            // respaldo) la copia anterior tarda un momento en cerrarse.
            if (!esPrimeraInstancia && args.Contains(ArgumentoReinicio))
            {
                try { esPrimeraInstancia = mutex.WaitOne(TimeSpan.FromSeconds(10)); }
                catch (AbandonedMutexException) { esPrimeraInstancia = true; }
            }

            if (!esPrimeraInstancia)
            {
                MessageBox.Show("AsistenciaQR ya esta abierto.", "AsistenciaQR",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // BLINDAJE 1: malla de seguridad global. Cualquier error no
            // previsto se registra en errores.log y se muestra un aviso
            // amable, en vez de la ventana de "dejo de funcionar".
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => ManejarError(e.Exception, mostrarAviso: true);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                ManejarError(e.ExceptionObject as Exception, mostrarAviso: false);
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                ManejarError(e.Exception, mostrarAviso: false);
                e.SetObserved();
            };

            ApplicationConfiguration.Initialize();
            Servicios.BitacoraService.RegistrarError = RegistrarEnLog;

            if (!PrepararPrimerUso()) return;

            // Respaldo del dia en segundo plano, mientras se inicia sesion.
            Task.Run(() => Servicios.RespaldoService.RespaldoAutomaticoSiCorresponde(RegistrarEnLog));

            using var login = new FrmLogin();
            if (login.ShowDialog() == DialogResult.OK)
            {
                // Avisos a padres: cada minuto se revisa si ya es la hora
                // del envio del dia. Solo una revision a la vez.
                int revisando = 0;
                using var temporizadorAvisos = new System.Threading.Timer(_ =>
                {
                    if (Interlocked.Exchange(ref revisando, 1) == 1) return;
                    try { Servicios.AvisosService.EnviarAutomaticoSiCorresponde(RegistrarEnLog); }
                    finally { Interlocked.Exchange(ref revisando, 0); }
                }, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));

                Application.Run(new FrmDashboard());
            }
        }

        /// <summary>
        /// Revisa la base de datos antes del login. Si no hay conexion,
        /// explica donde se configura. Si no hay ninguna cuenta, abre el
        /// asistente para crear el primer Administrador. Devuelve false
        /// si el programa no debe seguir.
        /// </summary>
        private static bool PrepararPrimerUso()
        {
            bool necesitaPrimerUsuario;
            try
            {
                Repositorios.EsquemaRepository.AplicarActualizaciones();
                necesitaPrimerUsuario = new Servicios.AuthService().NecesitaPrimerUsuario();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                RegistrarEnLog(ex);
                MessageBox.Show(
                    "No se pudo preparar la base de datos.\n\n" +
                    "Revisa que SQL Server este encendido y que la cadena de conexion sea correcta en:\n" +
                    $"{Repositorios.ConexionSQL.RutaArchivo}\n\n" +
                    $"(o en la variable de entorno {Repositorios.ConexionSQL.VariableEntorno}).\n\n" +
                    $"Detalle: {ex.Message}",
                    "AsistenciaQR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!necesitaPrimerUsuario) return true;

            using var primerUso = new FrmPrimerUso();
            return primerUso.ShowDialog() == DialogResult.OK;
        }

        private static void ManejarError(Exception? ex, bool mostrarAviso)
        {
            RegistrarEnLog(ex);

            if (!mostrarAviso) return;

            try
            {
                MessageBox.Show(
                    "Ocurrio un problema inesperado, pero el sistema sigue funcionando.\n" +
                    "Si se repite, cierra esta ventana y vuelve a intentarlo.",
                    "AsistenciaQR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch
            {
                // Si ni siquiera se puede mostrar el aviso, no hacer nada mas.
            }
        }

        private static void RegistrarEnLog(Exception? ex)
        {
            try
            {
                string carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AsistenciaQR");
                Directory.CreateDirectory(carpeta);

                string linea =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex?.GetType().Name}: {ex?.Message}{Environment.NewLine}" +
                    $"{ex?.StackTrace}{Environment.NewLine}{new string('-', 60)}{Environment.NewLine}";

                File.AppendAllText(Path.Combine(carpeta, "errores.log"), linea);
            }
            catch
            {
                // El registro nunca debe provocar otro error.
            }
        }
    }
}
