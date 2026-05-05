; Gestore PDF — Inno Setup script
; Build:  ISCC.exe build\installer.iss  (from repo root)
; Output: artifacts\installer\GestorePDF-Setup-1.0.0.exe

#define AppVersion "1.1.1"
#define AppName "Gestore PDF"
#define AppExe "GestorePDF.exe"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Davide Silvestri
AppPublisherURL=
DefaultDirName={autopf}\GestorePDF
DefaultGroupName={#AppName}
OutputBaseFilename=GestorePDF-Setup-{#AppVersion}
OutputDir=..\artifacts\installer
Compression=lzma2/ultra
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
WizardStyle=modern
SetupIconFile=..\assets\icons\app.ico
UninstallDisplayIcon={app}\GestorePDF.exe
ShowLanguageDialog=no
UninstallDisplayName={#AppName}

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Files]
Source: "..\artifacts\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";       Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Crea un'icona sul desktop"; GroupDescription: "Icone aggiuntive:"

[Run]
Filename: "{app}\{#AppExe}"; Description: "Avvia {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Registry]
; Associate .pdf with Gestore PDF (per-user, no UAC)
Root: HKCU; Subkey: "Software\Classes\.pdf\OpenWithProgids"; ValueType: string; ValueName: "GestorePDF.pdf"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\GestorePDF.pdf"; ValueType: string; ValueName: ""; ValueData: "Documento PDF"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\GestorePDF.pdf\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExe},0"
Root: HKCU; Subkey: "Software\Classes\GestorePDF.pdf\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
