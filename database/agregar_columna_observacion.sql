
-- Modificacion de esquema: agrega Observacion a
-- Asistencia, para registros manuales de excepcion
-- (sin carnet, QR dañado, etc.)

use AsistenciaQR;
go

alter table Asistencia add Observacion varchar(255) null;
go
