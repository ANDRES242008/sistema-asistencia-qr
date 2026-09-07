using System.Drawing;
using System.Drawing.Imaging;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;
using AsistenciaQR.Servicios;
using Microsoft.Data.SqlClient;

namespace AsistenciaQR
{
    /// <summary>
    /// Ejecutor automatizado de pruebas del núcleo de AsistenciaQR.
    /// Valida sistemáticamente cada punto de CHECKLIST_PRUEBAS_NUCLEO.md
    /// contra la base de datos SQL Server y los servicios de la aplicación.
    /// </summary>
    public static class PruebasNucleoRunner
    {
        private static int _pruebasPasadas = 0;
        private static int _pruebasFalladas = 0;
        private static readonly List<int> _estudiantesCreados = new();
        private static readonly List<string> _archivosCreados = new();

        public static int Ejecutar()
        {
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }

            Console.WriteLine("================================================================================");
            Console.WriteLine("       EJECUTOR DE PRUEBAS DEL NÚCLEO — SISTEMA DE ASISTENCIA QR (2026)         ");
            Console.WriteLine("================================================================================");
            Console.WriteLine();

            try
            {
                ProbarConexionBaseDatos();

                ProbarModulo1EstudiantesCRUD();
                ProbarModulo2GeneracionQryCarnet();
                ProbarModulo3KioscoYReglasAsistencia();
                ProbarModulo4Dashboard();
                ProbarModulo5Reportes();
                ProbarModulo6FlujoPuntaAPunta();
            }
            catch (Exception ex)
            {
                EscribirColor($"\n[ERROR CRÍTICO EN LA SUITE DE PRUEBAS]: {ex.Message}\n{ex.StackTrace}", ConsoleColor.Red);
                _pruebasFalladas++;
            }
            finally
            {
                LimpiarDatosDePrueba();
            }

            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("                             RESUMEN DE PRUEBAS                                 ");
            Console.WriteLine("================================================================================");
            EscribirColor($"  ✔ Pruebas Aprobadas: {_pruebasPasadas}", ConsoleColor.Green);
            if (_pruebasFalladas > 0)
            {
                EscribirColor($"  ✖ Pruebas Falladas:  {_pruebasFalladas}", ConsoleColor.Red);
            }
            else
            {
                EscribirColor("  ✖ Pruebas Falladas:  0 — ¡TODAS LAS REGLAS DEL NÚCLEO FUNCIONAN AL 100%!", ConsoleColor.Cyan);
            }
            Console.WriteLine("================================================================================");

            return _pruebasFalladas == 0 ? 0 : 1;
        }

        private static void ProbarConexionBaseDatos()
        {
            ImprimirEncabezadoModulo("0. Conectividad con SQL Server");

            try
            {
                using var conexion = ConexionSQL.ObtenerConexion();
                conexion.Open();
                Evaluar("Conexión abierta correctamente a SQL Server (AsistenciaQR)", true);
            }
            catch (Exception ex)
            {
                Evaluar($"Conexión a SQL Server falló: {ex.Message}", false);
                throw;
            }
        }

        private static void ProbarModulo1EstudiantesCRUD()
        {
            ImprimirEncabezadoModulo("1. Gestión de Estudiantes (CRUD)");

            var service = new EstudianteService();
            var repo = new EstudianteRepository();

            // 1.1 Registrar estudiante con foto
            string niePrueba = "88880001";
            LimpiarNiePrevio(niePrueba);

            string rutaFotoTemp = CrearImagenTemporal("foto_juan.jpg");
            var nuevo = new Estudiante
            {
                NIE = niePrueba,
                NombreCompleto = "Juan Carlos Martinez Prueba",
                Grado = "Primer Año",
                Seccion = "A",
                Correo = "juan.prueba@escuela.edu.sv"
            };

            var resAlta = service.Registrar(nuevo, rutaFotoTemp);
            Evaluar("1.1 Registrar estudiante nuevo con foto — guardado exitoso",
                resAlta.Exitoso && nuevo.EstudianteId > 0);

            if (nuevo.EstudianteId > 0) _estudiantesCreados.Add(nuevo.EstudianteId);

            var buscado = repo.ObtenerPorNIE(niePrueba);
            Evaluar("1.1 Estudiante guardado en base de datos y foto copiada",
                buscado != null && !string.IsNullOrEmpty(buscado.FotoRuta) && File.Exists(buscado.FotoRuta));

            if (buscado?.FotoRuta != null) _archivosCreados.Add(buscado.FotoRuta);

            // 1.2 Registrar con NIE duplicado
            var duplicado = new Estudiante
            {
                NIE = niePrueba,
                NombreCompleto = "Otro Estudiante con Mismo NIE",
                Grado = "Tercer Año",
                Seccion = "B"
            };
            var resDup = service.Registrar(duplicado, null);
            Evaluar("1.2 Rechaza registrar NIE repetido con mensaje adecuado",
                !resDup.Exitoso && resDup.Mensaje.Contains("Ya existe un estudiante registrado con ese NIE"));

            // 1.3 Editar estudiante existente
            buscado!.Grado = "Segundo Año";
            buscado.Seccion = "C";
            var resEdit = service.Editar(buscado, null);
            var modificado = repo.ObtenerPorId(buscado.EstudianteId);
            Evaluar("1.3 Editar estudiante (cambio a 'Segundo Año' - 'C') se refleja en BD",
                resEdit.Exitoso && modificado?.Grado == "Segundo Año" && modificado?.Seccion == "C");

            // 1.4 Dar de baja
            var resBaja = service.DarDeBaja(buscado.EstudianteId);
            var listaActivos = service.Buscar(null, null, incluirInactivos: false);
            Evaluar("1.4 Dar de baja — desaparece de la lista normal de activos",
                resBaja.Exitoso && !listaActivos.Any(e => e.EstudianteId == buscado.EstudianteId));

            // 1.5 Activar 'Mostrar inactivos'
            var listaConInactivos = service.Buscar(null, null, incluirInactivos: true);
            var dadoDeBaja = listaConInactivos.FirstOrDefault(e => e.EstudianteId == buscado.EstudianteId);
            Evaluar("1.5 Al incluir inactivos reaparece con Activo = false",
                dadoDeBaja != null && !dadoDeBaja.Activo);

            // 1.6 Reactivar
            var resReact = service.Reactivar(buscado.EstudianteId);
            var listaReactivados = service.Buscar(null, null, incluirInactivos: false);
            Evaluar("1.6 Reactivar — vuelve a aparecer en la lista normal con Activo = true",
                resReact.Exitoso && listaReactivados.Any(e => e.EstudianteId == buscado.EstudianteId && e.Activo));

            // 1.7 Buscar por nombre parcial
            var porNombre = service.Buscar("Carlos", null, false);
            Evaluar("1.7 Búsqueda por nombre parcial ('Carlos') encuentra al estudiante",
                porNombre.Any(e => e.EstudianteId == buscado.EstudianteId));

            // 1.8 Buscar por NIE
            var porNie = service.Buscar("88880001", null, false);
            Evaluar("1.8 Búsqueda por NIE encuentra al estudiante",
                porNie.Any(e => e.EstudianteId == buscado.EstudianteId));

            // 1.9 Filtrar por sección
            var porSeccion = service.Buscar(null, "C", false);
            Evaluar("1.9 Filtrar por sección 'C' devuelve solo estudiantes de sección C",
                porSeccion.Count > 0 && porSeccion.All(e => e.Seccion == "C"));
        }

        private static void ProbarModulo2GeneracionQryCarnet()
        {
            ImprimirEncabezadoModulo("2. Generación de QR y Carnet");

            var estudianteService = new EstudianteService();
            var qrService = new QRService();
            var qrRepo = new QRRepository();
            var carnetService = new CarnetService();

            string niePrueba = "88880002";
            LimpiarNiePrevio(niePrueba);

            var estudiante = new Estudiante
            {
                NIE = niePrueba,
                NombreCompleto = "Ana Sofia Morales Lopez",
                Grado = "Segundo Año",
                Seccion = "B",
                Correo = "ana.morales@colegio.edu.sv"
            };
            estudianteService.Registrar(estudiante, null);
            _estudiantesCreados.Add(estudiante.EstudianteId);

            // 2.1 Generar QR
            var qr1 = qrService.GenerarQrParaEstudiante(estudiante);
            Evaluar("2.1 Generar QR — archivo .png creado físicamente en carpeta de datos",
                qr1 != null && !string.IsNullOrEmpty(qr1.RutaImagen) && File.Exists(qr1.RutaImagen));

            if (qr1?.RutaImagen != null) _archivosCreados.Add(qr1.RutaImagen);

            // 2.2 Generar segundo QR (simular carnet perdido)
            var qr2 = qrService.GenerarQrParaEstudiante(estudiante);
            Evaluar("2.2 Generar segundo QR — nuevo QR activo generado",
                qr2 != null && qr2.QRId != qr1!.QRId && qr2.Activo);

            if (qr2?.RutaImagen != null) _archivosCreados.Add(qr2.RutaImagen);

            // Verificar que qr1 quedó inactivo
            var qrActivoActual = qrRepo.ObtenerQrActivo(estudiante.EstudianteId);
            Evaluar("2.2 El primer QR quedó inactivo y el segundo es el activo",
                qrActivoActual?.QRId == qr2.QRId);

            // 2.3 Escanear el QR viejo inactivo en el kiosco
            var asistenciaService = new AsistenciaService();
            var resViejo = asistenciaService.RegistrarPorCodigoQr(qr1!.CodigoQR);
            Evaluar("2.3 Escaneo de QR viejo (inactivo) es rechazado con 'CodigoNoValido'",
                resViejo.Estado == ResultadoEscaneo.CodigoNoValido);

            // 2.4 Generar imagen del carnet y PDF
            string rutaCarnetImg = carnetService.GenerarImagenCarnet(estudiante, qr2);
            string rutaCarnetPdf = carnetService.ExportarCarnetComoPdf(rutaCarnetImg);
            Evaluar("2.4 Generar carnet — imagen PNG creada y PDF exportado correctamente",
                File.Exists(rutaCarnetImg) && File.Exists(rutaCarnetPdf));

            _archivosCreados.Add(rutaCarnetImg);
            _archivosCreados.Add(rutaCarnetPdf);

            // 2.5 Generar carnet de estudiante SIN foto (con iniciales en círculo)
            string nieSinFoto = "88880003";
            LimpiarNiePrevio(nieSinFoto);
            var estSinFoto = new Estudiante
            {
                NIE = nieSinFoto,
                NombreCompleto = "David Ernesto Gomez",
                Grado = "Primer Año",
                Seccion = "A",
                FotoRuta = null
            };
            estudianteService.Registrar(estSinFoto, null);
            _estudiantesCreados.Add(estSinFoto.EstudianteId);

            var qrSinFoto = qrService.GenerarQrParaEstudiante(estSinFoto);
            if (qrSinFoto.RutaImagen != null) _archivosCreados.Add(qrSinFoto.RutaImagen);

            string rutaImgSinFoto = carnetService.GenerarImagenCarnet(estSinFoto, qrSinFoto);
            string rutaPdfSinFoto = carnetService.ExportarCarnetComoPdf(rutaImgSinFoto);
            Evaluar("2.5 Carnet sin foto genera círculo con iniciales sin error",
                File.Exists(rutaImgSinFoto) && File.Exists(rutaPdfSinFoto));

            _archivosCreados.Add(rutaImgSinFoto);
            _archivosCreados.Add(rutaPdfSinFoto);
        }

        private static void ProbarModulo3KioscoYReglasAsistencia()
        {
            ImprimirEncabezadoModulo("3. Kiosco de Escaneo y Reglas de Asistencia");

            var estudianteService = new EstudianteService();
            var qrService = new QRService();
            var asistenciaService = new AsistenciaService();

            string niePrueba = "88880004";
            LimpiarNiePrevio(niePrueba);

            var estudiante = new Estudiante
            {
                NIE = niePrueba,
                NombreCompleto = "Mariana Elizabeth Reyes",
                Grado = "Segundo Año",
                Seccion = "A"
            };
            estudianteService.Registrar(estudiante, null);
            _estudiantesCreados.Add(estudiante.EstudianteId);

            var qr = qrService.GenerarQrParaEstudiante(estudiante);
            if (qr.RutaImagen != null) _archivosCreados.Add(qr.RutaImagen);

            // 3.1 Primer escaneo: debe ser Exitoso
            var res1 = asistenciaService.RegistrarPorCodigoQr(qr.CodigoQR);
            Evaluar("3.1 Escanear QR válido — Asistencia registrada con estado 'Exitoso'",
                res1.Estado == ResultadoEscaneo.Exitoso && res1.Estudiante?.EstudianteId == estudiante.EstudianteId);

            // 3.2 Segundo escaneo el mismo día: debe rechazar con 'YaRegistradoHoy'
            var res2 = asistenciaService.RegistrarPorCodigoQr(qr.CodigoQR);
            Evaluar("3.2 Segundo escaneo el mismo día — Rechazado con 'YaRegistradoHoy'",
                res2.Estado == ResultadoEscaneo.YaRegistradoHoy);

            // 3.3 Escanear QR de estudiante dado de baja
            string nieBaja = "88880005";
            LimpiarNiePrevio(nieBaja);
            var estBaja = new Estudiante
            {
                NIE = nieBaja,
                NombreCompleto = "Estudiante Dado De Baja",
                Grado = "Primer Año",
                Seccion = "B"
            };
            estudianteService.Registrar(estBaja, null);
            _estudiantesCreados.Add(estBaja.EstudianteId);
            var qrBaja = qrService.GenerarQrParaEstudiante(estBaja);
            if (qrBaja.RutaImagen != null) _archivosCreados.Add(qrBaja.RutaImagen);

            estudianteService.DarDeBaja(estBaja.EstudianteId);
            var resBaja = asistenciaService.RegistrarPorCodigoQr(qrBaja.CodigoQR);
            Evaluar("3.3 Escaneo de QR de alumno inactivo — Rechazado con 'CodigoNoValido'",
                resBaja.Estado == ResultadoEscaneo.CodigoNoValido);

            // 3.4 Respaldo manual con NIE válido que ya marcó hoy
            var resNieYaMarco = asistenciaService.RegistrarPorNie(niePrueba);
            Evaluar("3.4 Respaldo manual NIE que ya marcó — Devuelve 'YaRegistradoHoy'",
                resNieYaMarco.Estado == ResultadoEscaneo.YaRegistradoHoy);

            // 3.5 Respaldo manual con NIE válido que no ha marcado
            string nieManual = "88880006";
            LimpiarNiePrevio(nieManual);
            var estManual = new Estudiante
            {
                NIE = nieManual,
                NombreCompleto = "Alumno Respaldo Manual",
                Grado = "Tercer Año",
                Seccion = "A"
            };
            estudianteService.Registrar(estManual, null);
            _estudiantesCreados.Add(estManual.EstudianteId);

            var resNieNuevo = asistenciaService.RegistrarPorNie(nieManual);
            Evaluar("3.5 Respaldo manual con NIE válido que no ha marcado — 'Exitoso'",
                resNieNuevo.Estado == ResultadoEscaneo.Exitoso);

            // 3.6 Respaldo manual con NIE inexistente
            var resNieInvalido = asistenciaService.RegistrarPorNie("00000000");
            Evaluar("3.6 Respaldo manual con NIE inexistente — 'EstudianteNoEncontrado'",
                resNieInvalido.Estado == ResultadoEscaneo.EstudianteNoEncontrado);
        }

        private static void ProbarModulo4Dashboard()
        {
            ImprimirEncabezadoModulo("4. Dashboard");

            var dashboardService = new DashboardService();
            var resumenHoy = dashboardService.ObtenerResumen(DateTime.Today, null);

            // 4.1 Presentes y Totales coherentes
            Evaluar("4.1 Presentes hoy es mayor o igual a 1", resumenHoy.Presentes >= 1);
            Evaluar("4.1 Faltantes = TotalActivos - Presentes",
                resumenHoy.Faltantes == resumenHoy.TotalActivos - resumenHoy.Presentes);

            // 4.2 Porcentaje calculado
            double esperado = resumenHoy.TotalActivos == 0 ? 0 : (double)resumenHoy.Presentes / resumenHoy.TotalActivos * 100;
            Evaluar("4.2 Porcentaje de asistencia calculado correctamente",
                Math.Abs(resumenHoy.Porcentaje - esperado) < 0.01);

            // 4.3 Fecha sin asistencias
            var resumenVacio = dashboardService.ObtenerResumen(new DateTime(2015, 1, 1), null);
            Evaluar("4.3 Consulta de fecha sin registros devuelve 0 presentes",
                resumenVacio.Presentes == 0);

            // 4.4 Filtrar por sección en Dashboard
            var resumenSeccionA = dashboardService.ObtenerResumen(DateTime.Today, "A");
            Evaluar("4.4 Filtro por sección A en Dashboard no lanza error y es consistente",
                resumenSeccionA.Presentes <= resumenHoy.Presentes);
        }

        private static void ProbarModulo5Reportes()
        {
            ImprimirEncabezadoModulo("5. Reportes y Exportación");

            var reporteService = new ReporteService();

            // 5.1 Obtener histórico por rango
            var hoy = DateTime.Today;
            var datos = reporteService.ObtenerHistorico(hoy.AddDays(-1), hoy.AddDays(1));
            Evaluar("5.1 Consulta de histórico de asistencias devuelve registros",
                datos.Count > 0);

            // 5.2 Filtro por NIE
            var datosNie = reporteService.ObtenerHistorico(hoy.AddDays(-1), hoy.AddDays(1), busqueda: "88880004");
            Evaluar("5.2 Filtro de histórico por NIE devuelve solo el estudiante correspondiente",
                datosNie.All(d => d.NIE == "88880004"));

            // 5.3 Exportar a Excel
            string rutaExcel = Path.Combine(Path.GetTempPath(), $"Reporte_Prueba_{Guid.NewGuid():N}.xlsx");
            reporteService.ExportarExcel(datos, rutaExcel);
            _archivosCreados.Add(rutaExcel);

            var infoExcel = new FileInfo(rutaExcel);
            Evaluar("5.3 Exportación a Excel (.xlsx) genera archivo con contenido",
                infoExcel.Exists && infoExcel.Length > 1000);

            // 5.4 Exportar a PDF
            string rutaPdf = Path.Combine(Path.GetTempPath(), $"Reporte_Prueba_{Guid.NewGuid():N}.pdf");
            reporteService.ExportarPdf(datos, rutaPdf);
            _archivosCreados.Add(rutaPdf);

            var infoPdf = new FileInfo(rutaPdf);
            Evaluar("5.4 Exportación a PDF (.pdf) genera archivo con contenido y sin error de fuentes",
                infoPdf.Exists && infoPdf.Length > 1000);
        }

        private static void ProbarModulo6FlujoPuntaAPunta()
        {
            ImprimirEncabezadoModulo("6. Flujo Completo de Punta a Punta");

            var estService = new EstudianteService();
            var qrService = new QRService();
            var carnetService = new CarnetService();
            var asistenciaService = new AsistenciaService();
            var dashboardService = new DashboardService();
            var reporteService = new ReporteService();

            string nieE2E = "88880009";
            LimpiarNiePrevio(nieE2E);

            // 1. Crear estudiante
            var alumno = new Estudiante
            {
                NIE = nieE2E,
                NombreCompleto = "Rodrigo Alejandro Castaneda",
                Grado = "Segundo Año",
                Seccion = "A",
                Correo = "rodrigo.castaneda@escuela.edu.sv"
            };
            var resReg = estService.Registrar(alumno, null);
            _estudiantesCreados.Add(alumno.EstudianteId);

            // 2. Generar QR
            var qr = qrService.GenerarQrParaEstudiante(alumno);
            if (qr.RutaImagen != null) _archivosCreados.Add(qr.RutaImagen);

            // 3. Generar Carnet
            string rutaImg = carnetService.GenerarImagenCarnet(alumno, qr);
            string rutaPdf = carnetService.ExportarCarnetComoPdf(rutaImg);
            _archivosCreados.Add(rutaImg);
            _archivosCreados.Add(rutaPdf);

            // 4. Escanear en Kiosco
            var resScan = asistenciaService.RegistrarPorCodigoQr(qr.CodigoQR);

            // 5. Validar en Dashboard
            var dash = dashboardService.ObtenerResumen(DateTime.Today, "A");
            bool enDashboard = dash.Detalle.Any(d => d.NIE == nieE2E);

            // 6. Validar en Reporte
            var rep = reporteService.ObtenerHistorico(DateTime.Today, DateTime.Today, busqueda: nieE2E);
            bool enReporte = rep.Any(d => d.NIE == nieE2E);

            Evaluar("6.1 Flujo E2E: Registro -> QR -> Carnet -> Escaneo -> Dashboard -> Reporte completado al 100%",
                resReg.Exitoso && qr != null && File.Exists(rutaPdf) && resScan.Estado == ResultadoEscaneo.Exitoso && enDashboard && enReporte);
        }

        private static void LimpiarNiePrevio(string nie)
        {
            try
            {
                using var conexion = ConexionSQL.ObtenerConexion();
                const string sql = "delete from Estudiantes where NIE = @NIE;";
                using var cmd = new SqlCommand(sql, conexion);
                cmd.Parameters.AddWithValue("@NIE", nie);
                conexion.Open();
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        private static void LimpiarDatosDePrueba()
        {
            Console.WriteLine();
            Console.WriteLine("🧹 Limpiando registros temporales de prueba en SQL Server y disco...");

            try
            {
                using var conexion = ConexionSQL.ObtenerConexion();
                conexion.Open();

                foreach (int id in _estudiantesCreados)
                {
                    const string sql = "delete from Estudiantes where EstudianteId = @Id;";
                    using var cmd = new SqlCommand(sql, conexion);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Nota de limpieza BD: {ex.Message}");
            }

            foreach (var ruta in _archivosCreados)
            {
                try
                {
                    if (File.Exists(ruta)) File.Delete(ruta);
                }
                catch { }
            }

            Console.WriteLine("   Registros y archivos temporales de prueba depurados exitosamente.");
        }

        private static string CrearImagenTemporal(string nombre)
        {
            string ruta = Path.Combine(Path.GetTempPath(), nombre);
            using var bmp = new Bitmap(100, 100);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.CornflowerBlue);
            bmp.Save(ruta, ImageFormat.Jpeg);
            _archivosCreados.Add(ruta);
            return ruta;
        }

        private static void ImprimirEncabezadoModulo(string titulo)
        {
            Console.WriteLine();
            EscribirColor($"--- {titulo} ---", ConsoleColor.Yellow);
        }

        private static void Evaluar(string descripcion, bool condicion)
        {
            if (condicion)
            {
                EscribirColor("  [PASS] ", ConsoleColor.Green, nuevaLinea: false);
                Console.WriteLine(descripcion);
                _pruebasPasadas++;
            }
            else
            {
                EscribirColor("  [FAIL] ", ConsoleColor.Red, nuevaLinea: false);
                Console.WriteLine(descripcion);
                _pruebasFalladas++;
            }
        }

        private static void EscribirColor(string texto, ConsoleColor color, bool nuevaLinea = true)
        {
            try
            {
                Console.ForegroundColor = color;
                if (nuevaLinea) Console.WriteLine(texto); else Console.Write(texto);
                Console.ResetColor();
            }
            catch
            {
                if (nuevaLinea) Console.WriteLine(texto); else Console.Write(texto);
            }
        }
    }
}
