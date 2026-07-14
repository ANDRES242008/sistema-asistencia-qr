# Historias de usuario


## Administrador / Encargado de disciplina

1. Como administrador, quiero registrar nuevos estudiantes en el sistema, para tener su información lista antes de generar su carnet.
2. Como administrador, quiero editar o eliminar estudiantes, para mantener los datos actualizados durante el año escolar.
3. Como administrador, quiero generar un código QR único para cada estudiante, para poder imprimir su carnet de identificación.
4. Como administrador, quiero ver un panel con la lista de estudiantes presentes hoy, para saber quién ha llegado en tiempo real.
5. Como administrador, quiero filtrar la asistencia por fecha o por sección, para revisar el historial de un grupo específico.
6. Como administrador, quiero exportar reportes de asistencia a Excel, para entregarlos a dirección o usarlos en reuniones.

## Estudiante

7. Como estudiante, quiero que se me entregue un carnet con mi código QR, para poder marcar mi asistencia todos los días.
8. Como estudiante, quiero que el sistema reconozca mi carnet al instante, para no perder tiempo en la entrada.
9. Como estudiante, quiero recibir una confirmación visual (ej. mensaje en pantalla) de que mi asistencia quedó registrada, para tener la seguridad de que se marcó correctamente.

## Sistema (reglas de negocio)

10. Como sistema, debo evitar que un mismo estudiante marque asistencia dos veces el mismo día, para que los reportes sean confiables.
11. Como sistema, debo mostrar un mensaje distinto si el QR ya fue escaneado hoy, para que el encargado entienda por qué no se registró de nuevo.
12. Como sistema, debo guardar cada asistencia con fecha y hora exacta, para poder generar históricos precisos por estudiante.
