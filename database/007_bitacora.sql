
-- Bitacora de auditoria: quien hizo que y cuando (excepciones, bajas,
-- cambios de horario, cuentas, respaldos). Se puede ejecutar varias
-- veces; la app tambien lo aplica sola al arrancar.

use AsistenciaQR;
go

if object_id('Bitacora', 'U') is null
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
end
go
