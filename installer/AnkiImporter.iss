#define MyAppName "Anki Importer"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "Anki Importer"
#define MyAppExeName "AnkiImporter.exe"

#ifndef ImporterSource
  #define ImporterSource "..\artifacts\local-importer\AnkiImporter.exe"
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
ChangesAssociations=yes

[Files]
Source: "{#ImporterSource}"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Anki Importer"; Filename: "{app}\{#MyAppExeName}"

[Registry]
Root: HKCU; Subkey: "Software\Classes\.ankiimport"; ValueType: string; ValueName: ""; ValueData: "AnkiImporter.Package"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\AnkiImporter.Package"; ValueType: string; ValueName: ""; ValueData: "Pacote Anki Importer"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\AnkiImporter.Package\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCU; Subkey: "Software\Classes\AnkiImporter.Package\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir Anki Importer"; Flags: nowait postinstall skipifsilent unchecked
