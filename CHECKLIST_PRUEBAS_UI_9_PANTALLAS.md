# Checklist de Pruebas de Usuario - Interfaz (9 Pantallas)

Este checklist cubre la interfaz rediseñada en Fase 4. No reemplaza
`CHECKLIST_PRUEBAS_NUCLEO.md` (flujo QR → asistencia → reporte); ese
sigue siendo la prueba crítica de negocio. Este es el repaso visual
y funcional de cada pantalla antes del tag v1.0.

## Antes de empezar

- [ ] Compila sin errores ni advertencias nuevas.
- [ ] SQL Server corriendo en `ANDRES\SQLEXPRESS`.
- [ ] Webcam Argom CAM45 conectada.
- [ ] Al menos 3 estudiantes de prueba, con carnet/QR generado.

## Pruebas generales (aplican a las 9 pantallas)

- [ ] Maximizar la ventana: todo escala, nada se corta ni se monta.
- [ ] Restaurar tamaño normal: todo vuelve a acomodarse bien.
- [ ] Redimensionar arrastrando el borde: sin parpadeo raro ni overlap.
- [ ] Reducir hasta el tamaño mínimo permitido: el texto sigue siendo legible.
- [ ] Botón Regresar (en las 8 que no son Dashboard): regresa al MISMO
      Dashboard que ya estaba abierto, no abre uno nuevo.
- [ ] El sidebar del Dashboard resalta la sección correcta al volver.

## 1. FrmLogin

- [ ] Usuario y contraseña correctos entran directo al Dashboard (no a Form1).
- [ ] Usuario o contraseña incorrectos muestran un error claro.
- [ ] El campo de contraseña oculta el texto.
- [ ] El logo se ve bien tanto en tamaño normal como maximizado.

## 2. FrmKioscoEscaneo

- [ ] QR válido de un estudiante activo: marca asistencia y muestra
      verde "a tiempo" o naranja "tardanza" según la hora configurada.
- [ ] QR ya usado hoy: muestra azul "ya registrado".
- [ ] QR inválido o no reconocido: muestra rojo "código inválido".
- [ ] El reloj del panel superior avanza en tiempo real.
- [ ] Botón Regresar funciona y apaga la cámara (no queda encendida
      en segundo plano).
- [ ] El registro manual desde el botón correspondiente funciona
      igual que por QR.

## 3. FrmDashboard

- [ ] Al abrir, muestra la asistencia del día actual (no de otro día).
- [ ] Cada botón del sidebar abre su pantalla correcta.
- [ ] Las estadísticas (presentes, tardanzas, etc.) coinciden con lo
      recién marcado en el Kiosco.
- [ ] Cerrar sesión regresa al Login correctamente.

## 4. FrmEstudiantes

- [ ] Crear un estudiante nuevo lo guarda y aparece en la lista.
- [ ] Editar un estudiante existente guarda los cambios.
- [ ] Desactivar un estudiante no lo borra, solo lo marca inactivo.
- [ ] Buscar por nombre, NIE, grado o sección filtra bien.
- [ ] Subir foto de perfil funciona y se ve en la lista.

## 5. FrmCarnetsQR

- [ ] Generar un QR nuevo para un estudiante que no tenía.
- [ ] Regenerar QR de un estudiante (carnet perdido) NO borra su
      historial de asistencia anterior.
- [ ] Exportar el carnet como imagen funciona.
- [ ] Exportar el carnet como PDF funciona.
- [ ] El QR generado se puede escanear de verdad en el Kiosco.

## 6. FrmReportes

- [ ] Filtrar por rango de fechas trae los registros correctos.
- [ ] Filtrar por grado y sección junto con fechas funciona.
- [ ] Buscar por nombre/NIE dentro de los resultados filtrados funciona.
- [ ] Exportar a Excel abre un archivo correcto y legible.
- [ ] Exportar a PDF abre un archivo correcto, con la columna de
      Observación visible.
- [ ] La paginación (10 por página) avanza y retrocede bien.

## 7. FrmRegistroExcepciones

- [ ] Buscar un estudiante por NIE o nombre lo encuentra y muestra
      su mini-perfil.
- [ ] Registrar una excepción con un motivo predefinido funciona.
- [ ] Registrar con notas adicionales concatena bien el texto guardado.
- [ ] Intentar registrar dos veces el mismo día muestra el mensaje
      correcto (ya registrado).
- [ ] La hora mostrada en pantalla coincide con la hora real del sistema.

## 8. FrmConfiguracion

- [ ] Cambiar la cámara seleccionada y Probar Cámara muestra la
      imagen correcta.
- [ ] Detener cámara apaga la vista previa sin trabar la pantalla.
- [ ] Cambiar el cooldown, guardar, y confirmar que el Kiosco respeta
      el nuevo tiempo.
- [ ] Probar Conexión SQL muestra éxito o error según corresponda.
- [ ] Guardar configuración muestra el aviso de confirmación.

## 9. FrmHorarios

- [ ] Cambiar la hora de inicio actualiza el mensaje de límite al instante.
- [ ] Cambiar los minutos de tolerancia actualiza el mensaje y la
      línea de tiempo visual.
- [ ] Guardar NO reclasifica asistencias ya registradas (verificar en Reportes).
- [ ] Después de guardar, una asistencia nueva SÍ usa la nueva regla.

## Cierre

- [ ] Recorrer las 9 pantallas una vez más en una sola sesión, sin
      cerrar la app, para verificar que no se pone lenta (posible
      fuga de memoria).
- [ ] Anotar cualquier hallazgo abajo antes de fusionar a main / tag v1.0.

---

### Hallazgos

_(usar esta sección para anotar bugs encontrados durante las pruebas)_

-
-
-
