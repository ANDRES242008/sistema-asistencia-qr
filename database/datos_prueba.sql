-- ============================================
-- Estudiante de prueba, solo para probar la
-- generacion de QR mientras no existe todavia
-- el formulario de registro (Fase 3).
-- ============================================

use AsistenciaQR;
go

insert into Estudiantes (NIE, NombreCompleto, Grado, Seccion, Correo, FotoRuta, Activo, FechaRegistro)
values ('99999999', 'Estudiante de Prueba', 'Segundo año', 'B', null, null, 1, getdate());
go
