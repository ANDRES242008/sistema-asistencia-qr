
--  31 julio 2026
-- Agrega columna para guardar la ruta de la imagen qr generada por cada codigo

use AsistenciaQR;
go

alter table QR add RutaImagen varchar(255) null;
go
