using Microsoft.Data.SqlClient;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// Aplica al arrancar los cambios de base de datos de versiones
    /// nuevas, para que un colegio que actualiza el programa no tenga
    /// que correr scripts a mano. Cada cambio revisa primero si ya
    /// existe, asi que ejecutarlo varias veces no hace dano. Debe
    /// coincidir con los scripts numerados de la carpeta database/.
    /// </summary>
    public static class EsquemaRepository
    {
        private static readonly string[] Actualizaciones =
        {
            // 001-005: esquema base. Asi una base de datos vacia (la que
            // crea el asistente de instalacion) queda lista sin scripts.
            @"if object_id('Estudiantes', 'U') is null
                create table Estudiantes (
                    EstudianteId int identity(1,1) not null constraint pk_Estudiantes primary key,
                    NIE varchar(20) not null constraint uq_Estudiantes_NIE unique,
                    NombreCompleto varchar(150) not null,
                    Grado varchar(50) not null,
                    Seccion varchar(10) not null,
                    Correo varchar(150) null,
                    FotoRuta varchar(255) null,
                    Activo bit not null default 1,
                    FechaRegistro datetime not null default getdate()
                );",
            @"if object_id('QR', 'U') is null
                create table QR (
                    QRId int identity(1,1) not null constraint pk_QR primary key,
                    EstudianteId int not null constraint fk_QR_Estudiantes
                        references Estudiantes(EstudianteId) on update cascade on delete cascade,
                    CodigoQR varchar(100) not null constraint uq_QR_CodigoQR unique,
                    FechaGeneracion datetime not null default getdate(),
                    Activo bit not null default 1
                );",
            @"if object_id('Asistencia', 'U') is null
                create table Asistencia (
                    AsistenciaId int identity(1,1) not null constraint pk_Asistencia primary key,
                    EstudianteId int not null constraint fk_Asistencia_Estudiantes
                        references Estudiantes(EstudianteId) on update cascade on delete cascade,
                    Fecha date not null,
                    Hora time not null,
                    constraint uq_Asistencia_Estudiante_Fecha unique (EstudianteId, Fecha)
                );",
            @"if col_length('QR', 'RutaImagen') is null
                alter table QR add RutaImagen varchar(255) null;",
            @"if col_length('Asistencia', 'Observacion') is null
                alter table Asistencia add Observacion varchar(255) null;",
            @"if object_id('Usuarios', 'U') is null
                create table Usuarios (
                    UsuarioId int identity(1,1) not null constraint pk_Usuarios primary key,
                    NombreUsuario varchar(50) not null constraint uq_Usuarios_NombreUsuario unique,
                    NombreCompleto varchar(150) not null,
                    ContrasenaHash varchar(100) not null,
                    ContrasenaSal varchar(50) not null,
                    Rol varchar(30) not null,
                    Activo bit not null constraint df_Usuarios_Activo default (1),
                    FechaRegistro datetime not null constraint df_Usuarios_FechaRegistro default (getdate())
                );",
            @"if object_id('ConfiguracionHorario', 'U') is null
                create table ConfiguracionHorario (
                    ConfiguracionHorarioId int identity(1,1) not null constraint pk_ConfiguracionHorario primary key,
                    HoraInicio time not null,
                    MinutosTolerancia int not null,
                    FechaActualizacion datetime not null
                        constraint df_ConfiguracionHorario_FechaActualizacion default (getdate())
                );",
            @"if col_length('Asistencia', 'EstadoPuntualidad') is null
                alter table Asistencia add EstadoPuntualidad varchar(20) null;",

            // 006_seguridad_usuarios.sql
            @"if col_length('Usuarios', 'IntentosFallidos') is null
                alter table Usuarios add IntentosFallidos int not null
                    constraint df_Usuarios_IntentosFallidos default (0);",
            @"if col_length('Usuarios', 'BloqueadoHasta') is null
                alter table Usuarios add BloqueadoHasta datetime null;",
            @"if col_length('Usuarios', 'DebeCambiarContrasena') is null
                alter table Usuarios add DebeCambiarContrasena bit not null
                    constraint df_Usuarios_DebeCambiarContrasena default (0);",

            // 007_bitacora.sql
            @"if object_id('Bitacora', 'U') is null
              begin
                  create table Bitacora (
                      BitacoraId int identity(1,1) not null,
                      Fecha datetime not null constraint df_Bitacora_Fecha default (getdate()),
                      Usuario varchar(50) not null,
                      Accion varchar(60) not null,
                      Detalle nvarchar(500) not null,
                      constraint pk_Bitacora primary key (BitacoraId)
                  );
                  create index ix_Bitacora_Fecha on Bitacora (Fecha desc);
              end",

            // 008_institucion.sql
            @"if object_id('ConfiguracionInstitucion', 'U') is null
                create table ConfiguracionInstitucion (
                    ConfiguracionInstitucionId int not null
                        constraint pk_ConfiguracionInstitucion primary key
                        constraint ck_ConfiguracionInstitucion_UnaFila check (ConfiguracionInstitucionId = 1),
                    Nombre nvarchar(80) not null,
                    Logo varbinary(max) null,
                    ColorPrincipal char(7) null,
                    FechaActualizacion datetime not null
                        constraint df_ConfiguracionInstitucion_Fecha default (getdate())
                );",

            // 009_avisos_padres.sql
            @"if col_length('Estudiantes', 'EncargadoNombre') is null
                alter table Estudiantes add EncargadoNombre nvarchar(150) null;",
            @"if col_length('Estudiantes', 'EncargadoCorreo') is null
                alter table Estudiantes add EncargadoCorreo varchar(150) null;",
            @"if object_id('ConfiguracionCorreo', 'U') is null
                create table ConfiguracionCorreo (
                    ConfiguracionCorreoId int not null
                        constraint pk_ConfiguracionCorreo primary key
                        constraint ck_ConfiguracionCorreo_UnaFila check (ConfiguracionCorreoId = 1),
                    Activo bit not null,
                    HoraEnvio time not null,
                    AvisarTardanzas bit not null,
                    AvisarAusencias bit not null,
                    ServidorSmtp varchar(150) not null,
                    Puerto int not null,
                    UsarSsl bit not null,
                    Usuario varchar(150) not null,
                    ContrasenaCifrada varbinary(max) null,
                    EquipoQueEnvia nvarchar(100) not null,
                    UltimoEnvioAutomatico date null
                );",
            @"if object_id('NotificacionesEnviadas', 'U') is null
                create table NotificacionesEnviadas (
                    NotificacionId int identity(1,1) not null
                        constraint pk_NotificacionesEnviadas primary key,
                    Fecha date not null,
                    EstudianteId int not null
                        constraint fk_Notificaciones_Estudiantes references Estudiantes(EstudianteId) on delete cascade,
                    Tipo varchar(20) not null,
                    Correo varchar(150) not null,
                    Enviado bit not null,
                    Error nvarchar(400) null,
                    FechaIntento datetime not null
                        constraint df_Notificaciones_FechaIntento default (getdate()),
                    constraint uq_Notificaciones_Estudiante_Fecha unique (EstudianteId, Fecha)
                );"
        };

        public static void AplicarActualizaciones()
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            conexion.Open();

            foreach (string sql in Actualizaciones)
            {
                using var comando = new SqlCommand(sql, conexion);
                comando.ExecuteNonQuery();
            }
        }
    }
}
