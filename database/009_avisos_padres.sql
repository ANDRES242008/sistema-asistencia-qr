
-- Avisos a padres por correo: datos del encargado de cada estudiante,
-- configuracion del correo del colegio y registro de avisos enviados.
-- Se puede ejecutar varias veces; la app tambien lo aplica sola al
-- arrancar.

use AsistenciaQR;
go

if col_length('Estudiantes', 'EncargadoNombre') is null
    alter table Estudiantes add EncargadoNombre nvarchar(150) null;
go

if col_length('Estudiantes', 'EncargadoCorreo') is null
    alter table Estudiantes add EncargadoCorreo varchar(150) null;
go

-- Una sola fila. La contrasena se guarda cifrada con DPAPI de Windows:
-- solo la computadora que la guardo puede leerla, por eso esa misma
-- computadora (EquipoQueEnvia) es la que manda los avisos.
if object_id('ConfiguracionCorreo', 'U') is null
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
    );
go

-- Un aviso por estudiante por dia: nunca se manda dos veces el mismo.
if object_id('NotificacionesEnviadas', 'U') is null
begin
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
    );
end
go
