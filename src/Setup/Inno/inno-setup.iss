
#define MyAppName "Multi Reproductor Multimedia"
#define MyAppExeName "MultiReproductorMultimedia.exe"
#define MyAppSourceDir "..\..\MpvNet.Windows\bin\Debug"
#define MyAppVersion GetFileVersion("..\..\MpvNet.Windows\bin\Debug\MultiReproductorMultimedia.exe")

[Setup]
AppId={{D992A8EF-5E04-41F2-9786-1D651476EBDE}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppName}
ArchitecturesInstallIn64BitMode=x64
Compression=lzma2
DefaultDirName={autopf}\{#MyAppName}
OutputBaseFilename=MultiReproductorMultimedia-v{#MyAppVersion}-setup-x64
OutputDir=..\..\dist
DefaultGroupName={#MyAppName}
SetupIconFile=..\..\MpvNet.Windows\app-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Files]
Source: "{#MyAppSourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Excludes: "win-x64,win-arm64"; Flags: ignoreversion recursesubdirs createallsubdirs;
