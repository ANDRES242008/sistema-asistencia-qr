
-- Seguridad de cuentas: bloqueo por intentos fallidos y cambio
-- obligatorio de contrasena despues de que un Administrador la resetea.
-- Se puede ejecutar varias veces; la app tambien lo aplica sola al
-- arrancar (Repositorios/EsquemaRepository.cs).

use AsistenciaQR;
go

if col_length('Usuarios', 'IntentosFallidos') is null
    alter table Usuarios add IntentosFallidos int not null
        constraint df_Usuarios_IntentosFallidos default (0);
go

if col_length('Usuarios', 'BloqueadoHasta') is null
    alter table Usuarios add BloqueadoHasta datetime null;
go

if col_length('Usuarios', 'DebeCambiarContrasena') is null
    alter table Usuarios add DebeCambiarContrasena bit not null
        constraint df_Usuarios_DebeCambiarContrasena default (0);
go
