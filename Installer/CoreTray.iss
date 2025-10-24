#define MyAppName "CoreTray"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Kalmix"
#define MyAppURL "https://github.com/kalmix/coretray"
#define MyAppExeName "CoreTray.exe"

[Setup]
; Basic App Info
AppId={{A5B3C4D2-E1F0-4A9B-8C7D-6E5F4A3B2C1D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation Directories
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output Configuration
OutputDir=..\Installer\Output
OutputBaseFilename=CoreTray-Setup-{#MyAppVersion}
SetupIconFile=SetupIcon.ico
WizardImageFile=WizardBanner.bmp

; Compression
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
LZMADictionarySize=1048576
LZMANumFastBytes=273

; Privileges and Compatibility
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
MinVersion=10.0.17763
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; UI Configuration
WizardStyle=modern
DisableWelcomePage=no
LicenseFile=

; Uninstall Configuration
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallFilesDir={app}\Uninstall

; Misc
AllowNoIcons=yes
Uninstallable=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Launch {#MyAppName} at Windows startup"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\CoreTray\bin\x64\Release\net7.0-windows10.0.19041.0\win10-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\CoreTray\bin\x64\Release\net7.0-windows10.0.19041.0\win10-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
; Launch app with elevated privileges 
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent shellexec

[Code]
function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  
  // Check for Windows 10 version 1809 (Build 17763) or later
  if (Version.Major < 10) or ((Version.Major = 10) and (Version.Build < 17763)) then
  begin
    MsgBox('This application requires Windows 10 version 1809 (Build 17763) or later.' + #13#10 + 
           'Please update Windows before installing.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;

// Check if .NET 7 Runtime is installed
function IsDotNetInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  // Check if dotnet command exists and can query for runtime
  Result := Exec('cmd.exe', '/c dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 7"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  if not IsDotNetInstalled() then
  begin
    MsgBox('Microsoft .NET 7 Desktop Runtime is required but not found.' + #13#10 + #13#10 +
           'The installer will now open the download page. Please install .NET 7 and run this installer again.', 
           mbInformation, MB_OK);
    ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/7.0', '', '', SW_SHOW, ewNoWait, ResultCode);
    Result := 'Please install .NET 7 Desktop Runtime and try again.';
  end
  else
    Result := '';
end;

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
