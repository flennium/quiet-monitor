param(
    [switch]$DesktopOnly,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

dotnet build (Join-Path $root 'Launcher\QuietMonitor.Launcher.csproj') -c $Configuration
dotnet build (Join-Path $root 'DesktopOverlay\QuietMonitor.DesktopOverlay.csproj') -c $Configuration
dotnet build (Join-Path $root 'SensorBroker\QuietMonitor.SensorBroker.csproj') -c $Configuration

if (-not $DesktopOnly) {
    $msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    if (-not $msbuild) { throw 'MSBuild was not found. Install Visual Studio with the UWP workload.' }
    & $msbuild (Join-Path $root 'QuietMonitor.csproj') /restore /t:Build /p:Configuration=$Configuration /p:Platform=x64 /p:AppxPackageSigningEnabled=false
    if ($LASTEXITCODE -ne 0) { throw "Game Bar build failed with exit code $LASTEXITCODE." }
}
