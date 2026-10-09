param(
    [ValidateSet('win-x64')]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$artifacts = Join-Path $root 'artifacts\QuietMonitor'
$desktop = Join-Path $artifacts 'DesktopOverlay'

if (Test-Path -LiteralPath $artifacts) { Remove-Item -LiteralPath $artifacts -Recurse -Force }
New-Item -ItemType Directory -Path $desktop -Force | Out-Null

dotnet publish (Join-Path $root 'Launcher\QuietMonitor.Launcher.csproj') -c Release -r $Runtime --self-contained false -p:PublishSingleFile=true -o $artifacts
dotnet publish (Join-Path $root 'DesktopOverlay\QuietMonitor.DesktopOverlay.csproj') -c Release -r $Runtime --self-contained false -p:PublishSingleFile=true -o $desktop

$shell = New-Object -ComObject WScript.Shell
$monitorShortcut = $shell.CreateShortcut((Join-Path $artifacts 'Quiet Monitor.lnk'))
$monitorShortcut.TargetPath = Join-Path $artifacts 'QuietMonitor.exe'
$monitorShortcut.WorkingDirectory = $artifacts
$monitorShortcut.Description = 'Toggle the Quiet Monitor hardware overlay'
$monitorShortcut.Hotkey = 'CTRL+ALT+Q'
$monitorShortcut.Save()

$settingsShortcut = $shell.CreateShortcut((Join-Path $artifacts 'Quiet Monitor Settings.lnk'))
$settingsShortcut.TargetPath = Join-Path $artifacts 'QuietMonitor.exe'
$settingsShortcut.Arguments = '--settings'
$settingsShortcut.WorkingDirectory = $artifacts
$settingsShortcut.Description = 'Configure Quiet Monitor launch mode'
$settingsShortcut.Save()

Write-Host "Portable desktop package created at $artifacts"
