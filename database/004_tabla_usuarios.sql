
-- Usuarios login 


use AsistenciaQR;
go

create table Usuarios (
    UsuarioId int identity(1,1) not null,
    NombreUsuario varchar(50) not null,
    NombreCompleto varchar(150) not null,
    ContrasenaHash varchar(100) not null,
    ContrasenaSal varchar(50) not null,
    Rol varchar(30) not null,
    Activo bit not null,
    FechaRegistro datetime not null
);
go

alter table Usuarios add constraint pk_Usuarios primary key (UsuarioId);
go

alter table Usuarios add constraint uq_Usuarios_NombreUsuario unique (NombreUsuario);
go

alter table Usuarios add constraint df_Usuarios_Activo default (1) for Activo;
alter table Usuarios add constraint df_Usuarios_FechaRegistro default (getdate()) for FechaRegistro;
go
