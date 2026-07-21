
create database AsistenciaQR;

use AsistenciaQR;

create table Estudiantes (
    EstudianteId int identity(1,1) not null,
    NIE varchar(20) not null,
    NombreCompleto varchar(150) not null,
    Grado varchar(50) not null,
    Seccion varchar(10) not null,
    Correo varchar(150) null,
    FotoRuta varchar(255) null,
    Activo bit not null default 1, 
    FechaRegistro datetime not null default getdate()
);

create table QR (
    QRId int identity(1,1) not null,
    EstudianteId int not null,
    CodigoQR varchar(100) not null,
    FechaGeneracion datetime not null default getdate(),
    Activo bit not null default 1
);


create table Asistencia (
AsistenciaId int identity(1,1) not null,
    EstudianteId int not null,
    Fecha date not null,
    Hora time not null
);


-- Llaves primarias y restriciones 
alter table Estudiantes add constraint pk_Estudiantes primary key (EstudianteId);
alter table QR add constraint pk_QR primary key (QRId);
alter table Asistencia add constraint pk_Asistencia primary key (AsistenciaId);


alter table Estudiantes add constraint uq_Estudiantes_NIE unique (NIE);
alter table QR add constraint uq_QR_CodigoQR unique (CodigoQR);
alter table Asistencia add constraint uq_Asistencia_Estudiante_Fecha unique (EstudianteId, Fecha);


-- ============================================
-- Llaves foráneas
-- ============================================
alter table QR add constraint fk_QR_Estudiantes foreign key (EstudianteId) references Estudiantes(EstudianteId) on update cascade on delete cascade;
alter table Asistencia add constraint fk_Asistencia_Estudiantes foreign key (EstudianteId) references Estudiantes(EstudianteId) on update cascade on delete cascade;

