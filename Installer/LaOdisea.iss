; Instalador de Windows de La Odisea del Pan de Platano (Inno Setup 6.3 o más nuevo).
;
; Antes: hacer el build de Unity en Build\LaOdisea_v<versión>\ (Windows x64).
; Compilar desde la raíz del repo:
;   "C:\Program Files\Inno Setup 7\ISCC.exe" Installer\LaOdisea.iss
; Otra versión sin editar el archivo:
;   "C:\Program Files\Inno Setup 7\ISCC.exe" /DMyAppVersion=1.0.1 Installer\LaOdisea.iss
; Sale en Build\LaOdisea_Setup_<versión>.exe.

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppName "La Odisea del Pan de Platano"
#define MyAppPublisher "La Odisea del Pan de Platano"
#define MyAppExeName "LaOdiseaDelPanDePlatano.exe"
#define BuildDir "..\Build\LaOdisea_v" + MyAppVersion

[Setup]
; No cambiar el AppId: con él Windows reconoce las versiones nuevas como actualización del mismo juego.
AppId={{E96CC0DB-5A3A-4B7B-AB87-5DE25072B111}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Sin pedir permisos de administrador (se instala para el usuario); en la primera pantalla se puede elegir
; instalarlo para todos.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\Build
OutputBaseFilename=LaOdisea_Setup_{#MyAppVersion}
SetupIconFile=LaOdisea.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Todo el build menos la carpeta de depuración que Unity pide no distribuir.
Source: "{#BuildDir}\*"; DestDir: "{app}"; Excludes: "*_BackUpThisFolder_ButDontShipItWithYourGame"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; \
  Flags: nowait postinstall skipifsilent
