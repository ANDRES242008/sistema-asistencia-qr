
-- Control de Puntualidad
--nueva tabla para la hora de inicio de clases + tolerancia
--  y una columna en Asistencia para guardar la clasificacion.


use AsistenciaQR;
go

create table ConfiguracionHorario (
    ConfiguracionHorarioId int identity(1,1) not null,
    HoraInicio time not null,
    MinutosTolerancia int not null,
    FechaActualizacion datetime not null
);
go

alter table ConfiguracionHorario add constraint pk_ConfiguracionHorario primary key (ConfiguracionHorarioId);
go

alter table ConfiguracionHorario add constraint df_ConfiguracionHorario_FechaActualizacion default (getdate()) for FechaActualizacion;
go

alter table Asistencia add EstadoPuntualidad varchar(20) null;
go
