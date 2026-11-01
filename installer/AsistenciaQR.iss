; Instalador de AsistenciaQR (Inno Setup 6).
;
; Compilar (despues de "dotnet publish ... -o publicar"):
;   iscc /DVersion=2.0.0 /DOrigen=..\publicar installer\AsistenciaQR.iss
;
; GitHub Actions lo compila solo al crear un tag v*.

#ifndef Version
  #define Version "0.0.0"
#endif
#ifndef Origen
  #define Origen "..\publicar"
#endif

#define Nombre "AsistenciaQR"
#define Ejecutable "AsistenciaQR.exe"
#define UrlSqlExpress "https://www.microsoft.com/es-es/sql-server/sql-server-downloads"

[Setup]
AppId={{6E3B1F2A-8C4D-4E7B-9A51-3D2C7F0B9E14}
AppName={#Nombre}
AppVersion={#Version}
AppVerName={#Nombre} {#Version}
AppPublisher=Andres Alexander Marroquin Rodriguez
AppPublisherURL=https://github.com/ANDRES242008/sistema-asistencia-qr
DefaultDirName={autopf}\{#Nombre}
DefaultGroupName={#Nombre}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename={#Nombre}-Setup-{#Version}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#Ejecutable}
VersionInfoVersion={#Version}

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "escritorio"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
Source: "{#Origen}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\database\*.sql"; DestDir: "{app}\database"; Flags: ignoreversion
Source: "..\database\instalar_todo.cmd"; DestDir: "{app}\database"; Flags: ignoreversion
Source: "..\docs\Manual_de_Usuario_AsistenciaQR.pdf"; DestDir: "{app}\docs"; Flags: ignoreversion skipifsourcedoesntexist

[Dirs]
; Configuracion de la conexion y respaldos de esta computadora. Todas las
; cuentas de Windows deben poder usarla, no solo la que instalo.
Name: "{commonappdata}\{#Nombre}"; Permissions: users-modify

[Icons]
Name: "{group}\{#Nombre}"; Filename: "{app}\{#Ejecutable}"
Name: "{group}\Manual de usuario"; Filename: "{app}\docs\Manual_de_Usuario_AsistenciaQR.pdf"; Check: ExisteManual
Name: "{group}\Desinstalar {#Nombre}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#Nombre}"; Filename: "{app}\{#Ejecutable}"; Tasks: escritorio

[Run]
Filename: "{app}\{#Ejecutable}"; Description: "Abrir {#Nombre}"; Flags: nowait postinstall skipifsilent

; Al desinstalar NO se borran la base de datos, los respaldos ni la
; configuracion: son los datos del colegio.

[Code]
function HaySqlServer(): Boolean;
var
  Instancias: TArrayOfString;
begin
  Result := RegGetValueNames(HKLM64, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', Instancias)
            and (GetArrayLength(Instancias) > 0);
end;

function ExisteManual(): Boolean;
begin
  Result := FileExists(ExpandConstant('{app}\docs\Manual_de_Usuario_AsistenciaQR.pdf'));
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Codigo: Integer;
begin
  if (CurStep = ssPostInstall) and (not HaySqlServer()) and (not WizardSilent()) then
  begin
    if MsgBox('AsistenciaQR necesita SQL Server para guardar sus datos, y no se encontro en esta computadora.' + #13#10#13#10 +
              'Puedes instalar SQL Server Express (gratis). Al abrir AsistenciaQR por primera vez, ' +
              'el programa creara la base de datos solo.' + #13#10#13#10 +
              'Abrir ahora la pagina de descarga de SQL Server Express?',
              mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', '{#UrlSqlExpress}', '', '', SW_SHOWNORMAL, ewNoWait, Codigo);
  end;
end;
