
-- Datos de la institucion (nombre, logo y color del carnet). Una sola
-- fila. El logo se guarda dentro de la base para que viaje con los
-- respaldos y se vea igual en todas las computadoras. Se puede
-- ejecutar varias veces; la app tambien lo aplica sola al arrancar.

use AsistenciaQR;
go

if object_id('ConfiguracionInstitucion', 'U') is null
    create table ConfiguracionInstitucion (
        ConfiguracionInstitucionId int not null
            constraint pk_ConfiguracionInstitucion primary key
            constraint ck_ConfiguracionInstitucion_UnaFila check (ConfiguracionInstitucionId = 1),
        Nombre nvarchar(80) not null,
        Logo varbinary(max) null,
        ColorPrincipal char(7) null,
        FechaActualizacion datetime not null
            constraint df_ConfiguracionInstitucion_Fecha default (getdate())
    );
go
