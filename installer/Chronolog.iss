; Chronolog multi-SKU installer
; Defines passed from build-installer.ps1:
;   MyAppName, MyAppVersion, MyAppId, MyAppExeName, PublishDir, OutputDir,
;   OutputBaseFilename, MinVersion, ArchitecturesAllowed, ArchitecturesInstallIn64BitMode,
;   RequireNet48 (0 or 1)

#define MyAppPublisher "Skywere Industries"
#define MyAppURL "https://skywere.local"

#ifndef MyAppName
  #define MyAppName "Chronolog Modern"
#endif
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef MyAppExeName
  #define MyAppExeName "Chronolog.exe"
#endif
#ifndef MyAppId
  #define MyAppId "{{A8E2C1D4-9F3B-4E6A-8C1D-2B7F0A5E9D31}"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\payload\modern"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif
#ifndef OutputBaseFilename
  #define OutputBaseFilename "Chronolog-Modern-Setup-1.0.0"
#endif
#ifndef MinVersion
  #define MinVersion "10.0"
#endif
#ifndef ArchitecturesAllowed
  #define ArchitecturesAllowed "x64compatible"
#endif
#ifndef ArchitecturesInstallIn64BitMode
  #define ArchitecturesInstallIn64BitMode "x64compatible"
#endif
#ifndef RequireNet48
  #define RequireNet48 "0"
#endif

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppPublisher}\{#MyAppName}
DefaultGroupName=Skywere Industries
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile=..\assets\installer.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed={#ArchitecturesAllowed}
ArchitecturesInstallIn64BitMode={#ArchitecturesInstallIn64BitMode}
MinVersion={#MinVersion}
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCopyright=© {#MyAppPublisher}
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startmenuicon"; Description: "Create &Start Menu shortcuts (Skywere Industries)"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Skywere Industries\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{autoprograms}\Skywere Industries\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"; Tasks: startmenuicon
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet48OrNewer(): Boolean;
var
  release: Cardinal;
begin
  Result := False;
  { HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\Release >= 528040 is 4.8 }
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release) then
    Result := release >= 528040
  else if IsWin64 and RegQueryDWordValue(HKLM32, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release) then
    Result := release >= 528040;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if '{#RequireNet48}' = '1' then
  begin
    if not IsDotNet48OrNewer() then
    begin
      MsgBox(
        '{#MyAppName} requires Microsoft .NET Framework 4.8.'#13#10#13#10 +
        'Install it from Microsoft, then run this setup again.'#13#10#13#10 +
        'Download: https://dotnet.microsoft.com/download/dotnet-framework/net48',
        mbCriticalError, MB_OK);
      Result := False;
    end;
  end;
end;
