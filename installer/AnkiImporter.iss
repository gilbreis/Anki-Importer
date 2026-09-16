#define MyAppName "Anki Importer"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Anki Importer"
#define MyAppExeName "AnkiImporter.Companion.exe"

#ifndef CompanionSource
  #define CompanionSource "..\artifacts\companion\AnkiImporter.Companion.exe"
#endif

#ifndef ServerUrl
  #define ServerUrl "https://anki-importer.example.invalid"
#endif

[Setup]
AppId={{8CBE6016-617C-4FBA-BB5E-B7AFEF4BAA8D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\AnkiImporter
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=AnkiImporterSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no

[Files]
Source: "{#CompanionSource}"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Anki Importer"; Filename: "{app}\{#MyAppExeName}"; Parameters: "setup {#ServerUrl}"
Name: "{userstartup}\Anki Importer"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"

[Registry]
Root: HKCU; Subkey: "Software\Classes\anki-importer"; ValueType: string; ValueName: ""; ValueData: "URL:Anki Importer Protocol"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\anki-importer"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\anki-importer\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCU; Subkey: "Software\Classes\anki-importer\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "setup {#ServerUrl}"; Description: "Configurar Anki Importer"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\AnkiImporter"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
