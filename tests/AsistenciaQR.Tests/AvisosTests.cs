using ClosedXML.Excel;
using AsistenciaQR.Modelos;
using AsistenciaQR.Repositorios;
using AsistenciaQR.Servicios;

namespace AsistenciaQR.Tests
{
    public class AvisosTests
    {
        // Viernes 30 de octubre de 2026
        private static readonly DateTime Viernes = new(2026, 10, 30, 9, 15, 0);

        private static ConfiguracionCorreo Config(bool activo = true, string equipo = "KIOSCO", DateTime? ultimo = null) => new()
        {
            Activo = activo,
            HoraEnvio = new TimeSpan(9, 0, 0),
            EquipoQueEnvia = equipo,
            UltimoEnvioAutomatico = ultimo,
            ServidorSmtp = "smtp.gmail.com",
            Puerto = 587,
            Usuario = "asistencia@colegio.edu.sv",
            Contrasena = "abcdefghijklmnop"
        };

        private static AvisoAsistencia Aviso(TipoAviso tipo, string? encargado = "Rosa Lopez", string nombre = "Valeria Martinez") => new()
        {
            Tipo = tipo,
            Fecha = Viernes.Date,
            HoraLlegada = tipo == TipoAviso.Tardanza ? new TimeSpan(7, 25, 0) : null,
            Estudiante = new Estudiante
            {
                NombreCompleto = nombre, Grado = "B2DA", Seccion = "A",
                EncargadoNombre = encargado, EncargadoCorreo = "rosa@ejemplo.com"
            }
        };

        // --- Cuando se envia ---

        [Fact]
        public void PasadaLaHora_EnLaComputadoraElegida_SeEnvia()
        {
            Assert.True(AvisosService.DebeEnviarAutomatico(Config(), Viernes, "kiosco"));
        }

        [Fact]
        public void AntesDeLaHora_NoSeEnvia()
        {
            Assert.False(AvisosService.DebeEnviarAutomatico(Config(), Viernes.Date.AddHours(8.99), "KIOSCO"));
        }

        [Fact]
        public void SiYaSeEnvioHoy_NoSeRepite()
        {
            Assert.False(AvisosService.DebeEnviarAutomatico(Config(ultimo: Viernes.Date), Viernes, "KIOSCO"));
            Assert.True(AvisosService.DebeEnviarAutomatico(Config(ultimo: Viernes.Date.AddDays(-1)), Viernes, "KIOSCO"));
        }

        [Fact]
        public void OtraComputadora_Desactivado_OFinDeSemana_NoSeEnvia()
        {
            Assert.False(AvisosService.DebeEnviarAutomatico(Config(), Viernes, "DIRECCION"));
            Assert.False(AvisosService.DebeEnviarAutomatico(Config(activo: false), Viernes, "KIOSCO"));
            Assert.False(AvisosService.DebeEnviarAutomatico(Config(), Viernes.AddDays(1), "KIOSCO"));
            Assert.False(AvisosService.DebeEnviarAutomatico(Config(), Viernes.AddDays(2), "KIOSCO"));
        }

        // --- El mensaje ---

        [Fact]
        public void Tardanza_DiceLaHoraDeLlegada()
        {
            var (asunto, cuerpo) = AvisosService.ConstruirMensaje(Aviso(TipoAviso.Tardanza), "Colegio Ejemplo", new TimeSpan(9, 0, 0));

            Assert.Equal("[Colegio Ejemplo] Llegada tarde: Valeria Martinez", asunto);
            Assert.Contains("Estimado(a) Rosa Lopez:", cuerpo);
            Assert.Contains("07:25", cuerpo);
            Assert.Contains("30/10/2026", cuerpo);
        }

        [Fact]
        public void Ausencia_DiceHastaQueHoraSeReviso()
        {
            var (asunto, cuerpo) = AvisosService.ConstruirMensaje(Aviso(TipoAviso.Ausencia), "Colegio Ejemplo", new TimeSpan(9, 0, 0));

            Assert.Equal("[Colegio Ejemplo] Ausencia: Valeria Martinez", asunto);
            Assert.Contains("no ha registrado su asistencia", cuerpo);
            Assert.Contains("09:00", cuerpo);
        }

        [Fact]
        public void SinNombreDeEncargado_UsaUnSaludoGenerico()
        {
            var (_, cuerpo) = AvisosService.ConstruirMensaje(Aviso(TipoAviso.Ausencia, encargado: null), "Colegio", TimeSpan.FromHours(9));

            Assert.Contains("Estimado(a) encargado(a):", cuerpo);
        }

        [Fact]
        public void LosNombres_NoPuedenInyectarHtml()
        {
            var (_, cuerpo) = AvisosService.ConstruirMensaje(
                Aviso(TipoAviso.Ausencia, encargado: "<b>Rosa</b>", nombre: "<script>alert(1)</script>"), "Colegio", TimeSpan.FromHours(9));

            Assert.DoesNotContain("<script>", cuerpo);
            Assert.DoesNotContain("<b>Rosa</b>", cuerpo);
            Assert.Contains("&lt;script&gt;", cuerpo);
        }

        // --- Configuracion ---

        [Fact]
        public void ConfiguracionCompleta_EsValida()
        {
            Assert.True(AvisosService.Validar(Config()).Exitoso);
        }

        [Fact]
        public void ConfiguracionIncompleta_SeRechaza()
        {
            var sinServidor = Config(); sinServidor.ServidorSmtp = " ";
            var usuarioMalo = Config(); usuarioMalo.Usuario = "asistencia";
            var sinContrasena = Config(); sinContrasena.Contrasena = "";
            var sinTipos = Config(); sinTipos.AvisarTardanzas = false; sinTipos.AvisarAusencias = false;

            Assert.False(AvisosService.Validar(sinServidor).Exitoso);
            Assert.False(AvisosService.Validar(usuarioMalo).Exitoso);
            Assert.False(AvisosService.Validar(sinContrasena).Exitoso);
            Assert.False(AvisosService.Validar(sinTipos).Exitoso);
        }

        [Fact]
        public void Contrasena_SeGuardaCifradaYSeRecupera()
        {
            byte[] cifrada = AvisosRepository.Cifrar("abcd efgh ijkl mnop");

            Assert.DoesNotContain("abcd", System.Text.Encoding.UTF8.GetString(cifrada));
            Assert.Equal("abcd efgh ijkl mnop", AvisosRepository.Descifrar(cifrada));
            Assert.Null(AvisosRepository.Descifrar(new byte[] { 1, 2, 3 }));
        }

        // --- Correo del encargado ---

        [Theory]
        [InlineData("rosa@ejemplo.com", true)]
        [InlineData("rosa.lopez@colegio.edu.sv", true)]
        [InlineData("rosa@ejemplo", false)]
        [InlineData("rosa ejemplo.com", false)]
        [InlineData("Rosa <rosa@ejemplo.com>", false)]
        public void CorreoDelEncargado_SeValida(string correo, bool valido)
        {
            var estudiante = new Estudiante { NIE = "12345678", NombreCompleto = "Valeria Martinez", Grado = "B2DA", Seccion = "A", EncargadoCorreo = correo };

            Assert.Equal(valido, EstudianteService.Validar(estudiante).Exitoso);
        }

        [Fact]
        public void Importacion_LeeLasColumnasDelEncargado()
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Estudiantes");
            string[] encabezados = { "NIE", "Nombre completo", "Grado", "Seccion", "Correo", "Encargado", "Correo del encargado" };
            for (int i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
            object[] fila = { "12345678", "Valeria Martinez", "B2DA", "A", "", "Rosa Lopez", "rosa@ejemplo.com" };
            for (int i = 0; i < fila.Length; i++) hoja.Cell(2, i + 1).Value = XLCellValue.FromObject(fila[i]);
            hoja.Cell(3, 1).Value = "87654321"; hoja.Cell(3, 2).Value = "Jose Franco"; hoja.Cell(3, 3).Value = "B2DA";
            hoja.Cell(3, 4).Value = "A"; hoja.Cell(3, 7).Value = "correo-malo";

            var filas = ImportacionEstudiantesService.LeerFilas(libro, out _);
            ImportacionEstudiantesService.Revisar(filas, new HashSet<string>());

            Assert.Equal("Rosa Lopez", filas[0].Estudiante.EncargadoNombre);
            Assert.Equal("rosa@ejemplo.com", filas[0].Estudiante.EncargadoCorreo);
            Assert.True(filas[0].EsValida);
            Assert.Contains("encargado", filas[1].Error);
        }
    }
}
