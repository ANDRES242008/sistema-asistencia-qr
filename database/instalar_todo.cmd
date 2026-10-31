@echo off
rem Crea la base de datos AsistenciaQR ejecutando los scripts en orden.
rem Uso: instalar_todo.cmd [instancia]   (por defecto .\SQLEXPRESS)
setlocal
set INSTANCIA=%~1
if "%INSTANCIA%"=="" set INSTANCIA=.\SQLEXPRESS

for %%f in ("%~dp00*.sql") do (
    echo Ejecutando %%~nxf ...
    sqlcmd -S "%INSTANCIA%" -E -b -i "%%f" || goto :error
)

echo.
echo Base de datos lista en %INSTANCIA%.
exit /b 0

:error
echo.
echo Fallo un script. Revisa el mensaje de arriba.
exit /b 1
