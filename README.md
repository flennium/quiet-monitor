# Quiet Monitor

Quiet Monitor is a small, open-source Windows hardware overlay designed to stay visible over games without injecting code into them.

It has two display modes:

- **Xbox Game Bar widget (preferred by default):** the best option for fullscreen games. Open Quiet Monitor, select its widget in Game Bar, and pin it.
- **Desktop overlay fallback:** a transparent, click-through, always-on-top WPF window for PCs without Game Bar or users who prefer not to use it. It works reliably with borderless/windowed games; Windows cannot guarantee ordinary topmost windows over exclusive fullscreen.

The launcher checks whether the `ms-gamebar:` handler exists and whether Windows Game DVR is enabled. If either check fails, it automatically starts the desktop overlay. The default can be changed with `QuietMonitor.exe --settings`.

## Principles

- Nothing starts with Windows.
- No service, tray process, driver, DLL injection, or game hook.
- Closing the overlay stops sensor polling and exits every Quiet Monitor process.
- Hardware readings stay on the PC; there is no telemetry or network client.
- A second launch of the desktop overlay closes the running overlay, giving a simple toggle shortcut.

## Readings

CPU load and temperature, GPU load, edge temperature, hotspot temperature, board power, fan speed, system memory, and video memory are collected through LibreHardwareMonitor. A missing or unsupported sensor is shown as unavailable rather than fabricated.

## Use

1. Install the Game Bar package from a release, or build/deploy it from Visual Studio.
2. Keep `QuietMonitor.exe` and the `DesktopOverlay` folder together.
3. Create a shortcut to `QuietMonitor.exe`.
4. With Game Bar available, press the shortcut, choose **Quiet Monitor** in Game Bar's widget menu, and pin it. Pinning is remembered by Game Bar.
5. To disable Game Bar mode, run `QuietMonitor.exe --settings` and clear **Use Xbox Game Bar when available**.

If Game Bar is not installed or is disabled, step 4 is skipped and the desktop overlay opens automatically.

## Architecture

| Component | Technology | Lifetime |
| --- | --- | --- |
| `Launcher` | .NET 10 WPF | Exits after selecting a mode |
| Game Bar widget | C# / UWP XAML / Xbox Game Bar SDK | Hosted only while the widget is active |
| `SensorBroker` | .NET Framework 4.8 / LibreHardwareMonitor | Exits when the widget heartbeat stops |
| `DesktopOverlay` | .NET 10 WPF / LibreHardwareMonitor | Exits when the overlay closes |

The Game Bar widget uses a short-lived full-trust broker because UWP widgets cannot directly access all hardware sensors. The broker watches a heartbeat file and shuts itself down within seconds if the widget disappears unexpectedly.

## Build

Requirements:

- Windows 10 version 1903 or newer; Windows 11 recommended
- Visual Studio 2022/2026 with **Universal Windows Platform development** and **.NET desktop development**
- Windows 11 SDK 10.0.26100
- .NET 10 SDK
- .NET Framework 4.8 developer pack
- Xbox Game Bar for live widget testing

Build the desktop components without launching them:

```powershell
.\build.ps1 -DesktopOnly
```

Build everything from a Developer PowerShell:

```powershell
.\build.ps1
```

The UWP project currently needs the UWP/.NET Native reference assemblies installed by Visual Studio. A plain .NET SDK installation is not enough.

## Safety and compatibility

Quiet Monitor does not inject or hook game processes. This avoids the riskiest anti-cheat integration pattern, but no third-party project can promise compatibility with every game's policy. The fallback overlay is intentionally limited to normal Windows topmost behavior. For true fullscreen presentation, use and pin the Game Bar widget.

## License

Quiet Monitor is released under the MIT License. Third-party components keep their own licenses; see [NOTICE.md](NOTICE.md).
