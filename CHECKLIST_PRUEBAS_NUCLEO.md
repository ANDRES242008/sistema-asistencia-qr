# Checklist de Pruebas del Núcleo — AsistenciaQR

Objetivo: probar el flujo completo desde que se registra un estudiante hasta que aparece en un reporte, y confirmar que las reglas de negocio clave funcionan.

## 1. Gestión de Estudiantes (CRUD)

- [x] Registrar un estudiante nuevo con foto — aparece en la lista
- [x] Registrar un estudiante con un NIE que ya existe — muestra el mensaje de "Ya existe un estudiante registrado con ese NIE" y NO lo duplica
- [x] Editar un estudiante existente (cambiar grado o sección) — el cambio se refleja en la lista
- [x] Dar de baja a un estudiante — desaparece de la lista normal
- [x] Activar "Mostrar inactivos" — el estudiante dado de baja reaparece
- [x] Reactivar ese estudiante — vuelve a aparecer en la lista normal
- [x] Buscar por nombre parcial (ej. escribir solo 3 letras) — filtra correctamente
- [x] Buscar por NIE — filtra correctamente
- [x] Filtrar por sección — solo muestra esa sección

## 2. Generación de QR y Carnet

- [x] Generar QR para un estudiante nuevo — se crea la imagen en `%LocalAppData%\AsistenciaQR\QR\`
- [x] Generar un segundo QR para el mismo estudiante (simular carnet perdido) — el QR viejo queda inactivo, el nuevo funciona
- [x] Escanear el QR viejo (inactivo) en el Kiosco — debe salir como "Código no reconocido"
- [x] Generar el carnet — la imagen y el PDF se crean en `%LocalAppData%\AsistenciaQR\Carnets\`
- [x] Generar carnet de un estudiante SIN foto — debe mostrar el círculo con iniciales, no un error

## 3. Kiosco de Escaneo

- [ ] Abrir el Kiosco — la cámara enciende sola, sin botones (Validación con cámara física)
- [x] Escanear un QR válido — sale la alerta verde con foto/iniciales, nombre, NIE, grado y hora
- [x] Escanear el mismo QR otra vez de inmediato — no debe reaccionar (cooldown de 4 segundos en CamaraQRService)
- [x] Esperar el cooldown y volver a escanear el mismo QR — debe salir "Ya registraste tu asistencia hoy" (alerta amarilla)
- [x] Escanear un QR de un estudiante dado de baja — debe rechazarlo
- [x] Probar el respaldo manual con un NIE válido que no ha marcado — debe registrar y mostrar los datos
- [x] Probar el respaldo manual con un NIE que no existe — debe decir "NIE no encontrado o inactivo"
- [ ] Cerrar el Kiosco y volver a abrirlo — la cámara debe encender de nuevo sin errores (Validación con cámara física)

## 4. Dashboard

- [x] Con estudiantes ya escaneados hoy, el conteo de "Presentes" es correcto
- [x] "Faltantes" = total de activos menos presentes
- [x] El porcentaje calculado es correcto
- [x] Cambiar la fecha a un día sin registros — todo en cero
- [x] Filtrar por sección — los números y la lista se ajustan
- [x] La lista de detalle muestra hora en formato limpio (hh:mm:ss, sin decimales)

## 5. Reportes

- [x] Buscar por NIE de un estudiante — muestra solo su historial completo
- [x] Cambiar el rango de fechas — los resultados se ajustan
- [x] Filtrar por grado y sección combinados
- [x] Exportar a Excel — abre bien, encabezados en negrita, columnas legibles
- [x] Exportar a PDF — abre bien, sin errores de fuente, tabla ordenada
- [x] Intentar exportar sin resultados (filtro que no matchea nada) — debe avisar "No hay datos para exportar" en vez de crear un archivo vacío

## 6. Flujo completo de punta a punta (el más importante)

- [x] Registrar un estudiante nuevo desde cero
- [x] Generarle su QR
- [x] Generarle su carnet
- [x] Escanear ese QR en el Kiosco
- [x] Confirmar que aparece en el Dashboard de hoy
- [x] Confirmar que aparece en Reportes al buscar su NIE

## Antes de poner el tag v0.5

- [ ] Revisar si quieres limpiar los datos de prueba (`Estudiante de Prueba`, NIE 99999999) antes de seguir, o dejarlos y limpiarlos más adelante
- [x] Confirmar que `git status` no muestra ninguna carpeta `bin/`, `obj/` ni `.vs/` sin querer
