#ifndef RepoRoot
  #define RepoRoot ".."
#endif
#define AppVersion "1.0.0"

[Setup]
AppId={{BEB71C57-9EA2-4C04-B03B-619C5A1B2F3F}
AppName=Auto-Hotkeys
AppVersion={#AppVersion}
AppPublisher=George Fejer
AppPublisherURL=https://github.com/GeorgeFejer91/Auto-Hotkeys
AppSupportURL=https://github.com/GeorgeFejer91/Auto-Hotkeys/issues
AppUpdatesURL=https://github.com/GeorgeFejer91/Auto-Hotkeys/releases
DefaultDirName={localappdata}\Programs\Auto-Hotkeys
DefaultGroupName=Auto-Hotkeys
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0
OutputDir={#RepoRoot}\dist
OutputBaseFilename=Auto-Hotkeys-Setup-{#AppVersion}
SetupIconFile={#RepoRoot}\assets\App.ico
UninstallDisplayIcon={app}\Auto-Hotkeys.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
LicenseFile={#RepoRoot}\LICENSE

[Tasks]
Name: autostart; Description: "Start Auto-Hotkeys with Windows (recommended)"

[Files]
Source: "{#RepoRoot}\build\Auto-Hotkeys.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Auto-Hotkeys"; Filename: "{app}\Auto-Hotkeys.exe"

[Run]
Filename: "{app}\Auto-Hotkeys.exe"; Parameters: "--configure-autostart=on"; Tasks: autostart; Flags: runhidden waituntilterminated
Filename: "{app}\Auto-Hotkeys.exe"; Parameters: "--configure-autostart=off"; Tasks: not autostart; Flags: runhidden waituntilterminated
Filename: "{app}\Auto-Hotkeys.exe"; Parameters: "--background"; Flags: nowait runhidden
Filename: "{app}\Auto-Hotkeys.exe"; Description: "Open Auto-Hotkeys"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\Auto-Hotkeys.exe"; Parameters: "--prepare-uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveAutoHotkeysStartup"

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var ExitCode: Integer; Existing: String;
begin
  Result := '';
  Existing := ExpandConstant('{app}\Auto-Hotkeys.exe');
  if FileExists(Existing) then
    if not Exec(Existing, '--prepare-uninstall', '', SW_HIDE, ewWaitUntilTerminated, ExitCode) or (ExitCode <> 0) then
      Result := 'Could not stop the previous Auto-Hotkeys installation. Close it and try again.';
end;

function InitializeSetup(): Boolean;
var Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    SuppressibleMsgBox('Auto-Hotkeys needs .NET Framework 4.8, included with current Windows 10 and Windows 11. Please update Windows before installing.', mbError, MB_OK, IDOK);
end;
