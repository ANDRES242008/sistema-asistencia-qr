# Sistema de Asistencia con Código QR

Sistema de control de asistencia escolar que usa carnets de estudiante con código QR para registrar entradas de forma rápida .

Proyecto desarrollado para la Feria de Programación 2026.

## ¿Qué problema resuelve?

Pasar lista a mano toma tiempo y es fácil equivocarse. Con este sistema, cada estudiante tiene un carnet con un código QR único. Al escanearlo con una cámara web, el sistema:

- Reconoce al estudiante automáticamente
- Registra la hora de asistencia en la base de datos
- Evita que se marque doble asistencia el mismo día
- Permite generar reportes de asistencia por estudiante, sección o fecha

## Tecnologías utilizadas

- **C# (Windows Forms / WPF)** — interfaz de escritorio de la aplicación
- **SQL Server** — base de datos para estudiantes y asistencias
- **QRCoder (NuGet)** — generación de los códigos QR
- **AForge.NET / ZXing** — lectura de QR en tiempo real desde la webcam
- **ClosedXML** — exportación de reportes a Excel

## Estructura del proyecto

```
/src            Código fuente de la aplicación C#
/database       Scripts SQL (creación de tablas, datos de prueba)
/docs           Documentación técnica, diagramas, manual de usuario
/assets         Imágenes de carnets y códigos QR generados
```

## Estado actual

🚧 Proyecto en construcción - consta de 5 fases 

## Autores

- Andrés Alexander Marroquín Rodríguez
- Antony Leonel Rosa
- Mathew Pineda Morales
- Alexa Ciefuegos Calderon
- Aarón Levi Vásquez Moran
