# Documentación de la base de datos — AsistenciaQR

## Tabla: Estudiantes

Guarda la información básica de cada estudiante inscrito en el sistema.

| Columna | Tipo | Descripción |
|---|---|---|
| EstudianteId | INT (PK) | Identificador interno, autogenerado |
| NIE | VARCHAR(20), único | Código único nacional del estudiante (El Salvador) |
| NombreCompleto | VARCHAR(150) | Nombre y apellidos completos |
| Grado | VARCHAR(50) | Ej. "Segundo Año" |
| Seccion | VARCHAR(10) | Ej. "B" |
| Correo | VARCHAR(150) | Correo institucional o personal (opcional) |
| FotoRuta | VARCHAR(255) | Ruta o nombre del archivo de la foto del estudiante |
| Activo | BIT | 1 = estudiante activo, 0 = retirado (no se borra el registro) |
| FechaRegistro | DATETIME | Fecha en que se registró en el sistema |

## Tabla: QR

Guarda el código QR asignado a cada estudiante. Se usa tabla separada (en vez de una columna en Estudiantes) para poder generar un QR nuevo si se pierde el carnet, sin perder el historial de códigos anteriores.

| Columna | Tipo | Descripción |
|---|---|---|
| QRId | INT (PK) | Identificador interno, autogenerado |
| EstudianteId | INT (FK) | Referencia al estudiante dueño del QR |
| CodigoQR | VARCHAR(100), único | Texto único que se codifica dentro de la imagen QR |
| FechaGeneracion | DATETIME | Fecha en que se generó ese código |
| Activo | BIT | 1 = es el QR vigente del estudiante, 0 = código viejo/reemplazado |

## Tabla: Asistencia

Guarda cada registro de entrada de un estudiante al escanear su QR.

| Columna | Tipo | Descripción |
|---|---|---|
| AsistenciaId | INT (PK) | Identificador interno, autogenerado |
| EstudianteId | INT (FK) | Referencia al estudiante que marcó asistencia |
| Fecha | DATE | Fecha de la asistencia |
| Hora | TIME | Hora exacta en que se escaneó el QR |

**Regla importante:** existe una restricción `UNIQUE` sobre `(EstudianteId, Fecha)`, que impide que un mismo estudiante tenga dos registros de asistencia el mismo día. Si intenta escanear de nuevo, la base de datos rechaza el segundo intento automáticamente — esto resuelve la historia de usuario #10 y #11.

## Relaciones entre tablas

- Un estudiante puede tener **varios códigos QR** a lo largo del tiempo (pero solo uno activo).
- Un estudiante puede tener **varios registros de asistencia**, uno por cada día que asista.
