#ifndef AppVersion
  #define AppVersion "0.1.0-beta.3"
#endif
#ifndef CertThumbprint
  #define CertThumbprint "876A918726367882CA91C931386E5D6FB4EE10B0"
#endif

[Setup]
AppId={{E39AAAF2-9B8C-4C44-9D41-3C81C6CB6321}
AppName=Quiet Monitor
AppVersion={#AppVersion}
AppPublisher=flennium
AppPublisherURL=https://github.com/flennium/quiet-monitor
AppSupportURL=https://github.com/flennium/quiet-monitor/issues
AppUpdatesURL=https://github.com/flennium/quiet-monitor/releases
DefaultDirName={autopf}\Quiet Monitor
DefaultGroupName=Quiet Monitor
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=QuietMonitor-v{#AppVersion}-setup-win-x64
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupIconFile=QuietMonitor.ico
UninstallDisplayIcon={app}\QuietMonitor.exe
VersionInfoVersion=0.1.0.3
VersionInfoCompany=flennium
VersionInfoDescription=Quiet Monitor installer
VersionInfoProductName=Quiet Monitor

[Files]
Source: "..\artifacts\QuietMonitor\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\staging\GameBarPackage\*"; DestDir: "{app}\GameBarPackage"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "QuietMonitor.cer"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Quiet Monitor"; Filename: "{app}\QuietMonitor.exe"; HotKey: "ctrl+alt+q"
Name: "{group}\Quiet Monitor Settings"; Filename: "{app}\QuietMonitor.exe"; Parameters: "--settings"
Name: "{autodesktop}\Quiet Monitor"; Filename: "{app}\QuietMonitor.exe"; HotKey: "ctrl+alt+q"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: checkedonce

[Run]
Filename: "{sys}\certutil.exe"; Parameters: "-addstore -f TrustedPeople ""{app}\QuietMonitor.cer"""; StatusMsg: "Trusting the Quiet Monitor package certificate..."; Flags: runhidden waituntilterminated
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\GameBarPackage\Install.ps1"" -SkipLoggingTelemetry"; StatusMsg: "Installing the Xbox Game Bar extension..."; Flags: waituntilterminated
Filename: "{app}\QuietMonitor.exe"; Parameters: "--settings"; Description: "Open the Quiet Monitor control center"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Get-AppxPackage flennium.QuietMonitor | Remove-AppxPackage"""; Flags: runhidden waituntilterminated
Filename: "{sys}\certutil.exe"; Parameters: "-delstore TrustedPeople {#CertThumbprint}"; Flags: runhidden waituntilterminated
