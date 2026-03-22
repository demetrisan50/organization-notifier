; Inno Setup Script for Organization Notifier
; Download Inno Setup from https://jrsoftware.org/isdl.php

#define MyAppName "Organization Notifier"
#define MyAppVersion "1.0"
#define MyAppPublisher "IT Support Team"
#define MyAppExeName "organization-notifier.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{D37E69-3AC7-4E69-A4A8-9340CFBC6B90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=.\Output
OutputBaseFilename=OrganizationNotifierSetup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; The source path should point to your 'publish' folder after running 'dotnet publish'
Source: ".\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\publish\Alert.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\publish\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\publish\icons\*"; DestDir: "{app}\icons"; Flags: ignoreversion recursesubdirs createallsubdirs
; Add any other DLLs or files found in the publish folder here

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
