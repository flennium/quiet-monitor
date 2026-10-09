<div align="center">

# Quiet Monitor

### Hardware stats where they matter — over your game, only when you ask.

[![Build](https://github.com/flennium/quiet-monitor/actions/workflows/build.yml/badge.svg)](https://github.com/flennium/quiet-monitor/actions/workflows/build.yml)
[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-4287F5)](https://github.com/flennium/quiet-monitor)
[![License: MIT](https://img.shields.io/badge/License-MIT-6FD4B0.svg)](../LICENSE)

Quiet Monitor is a compact, open-source C# hardware overlay for Windows. It prefers a native Xbox Game Bar widget for fullscreen games and automatically falls back to a transparent desktop overlay when Game Bar is unavailable or disabled.

**No startup entry · No service · No tray process · No game injection · No telemetry**

</div>

---

## Why Quiet Monitor?

Most monitoring tools are designed to stay running. Quiet Monitor is designed to disappear. Open it from a shortcut when you need a quick hardware check, then close it and every Quiet Monitor process stops.

It reads CPU, GPU, memory, temperature, power, fan, and VRAM sensors through LibreHardwareMonitor. Unsupported readings are shown as unavailable instead of being guessed.

## Two overlay modes, one shortcut

| | Xbox Game Bar | Desktop fallback |
| --- | --- | --- |
| Best for | Fullscreen games | Borderless and windowed games |
| Presentation | Pin-aware Game Bar widget | Click-through topmost window |
| Default | **Enabled** | Automatic fallback |
| Game integration | Microsoft-hosted widget | None |
| Lifetime | While the widget is active | Until closed or toggled off |

The launcher checks the registered `ms-gamebar:` handler and Windows Game DVR settings. If Game Bar is ready, it opens Game Bar so the Quiet Monitor widget can be selected and pinned. If not, it opens the desktop overlay. Run `QuietMonitor.exe --settings` to turn Game Bar preference off at any time.

> A normal Windows topmost window cannot reliably cover exclusive fullscreen. Pin the Game Bar widget for that scenario. Quiet Monitor deliberately avoids DLL injection and graphics hooks.

## Quick start

1. Download both build artifacts from the latest successful [GitHub Actions run](https://github.com/flennium/quiet-monitor/actions/workflows/build.yml).
2. Install the Game Bar test package from the `QuietMonitor-game-bar-x64` artifact if Xbox Game Bar is installed.
3. Extract `QuietMonitor-desktop-win-x64` and keep its `DesktopOverlay` folder beside `QuietMonitor.exe`.
4. Use **Quiet Monitor.lnk** to open or toggle the monitor.
5. The first time Game Bar opens, choose **Quiet Monitor** from its widget menu and pin it. Game Bar remembers the pin.

Use **Quiet Monitor Settings.lnk** to choose the desktop overlay permanently, or run:

```powershell
QuietMonitor.exe --settings
```

## What it shows

- CPU utilization and temperature
- GPU utilization, edge temperature, and hotspot temperature
- GPU board power and fan speed
- Used and total system memory
- Used and total video memory

The settings UI controls visible readings, refresh interval, opacity, scale, and placement. Both overlays use a compact score-bug layout with tabular values and high-contrast warning states.

## Architecture

```text
Shortcut
   │
   ▼
Launcher ── Game Bar ready ──► Xbox Game Bar widget ──► Sensor broker
   │                                  │                       │
   └── unavailable / disabled ──► Desktop overlay             └── heartbeat shutdown
```

| Component | Stack | What keeps it alive |
| --- | --- | --- |
| Launcher | .NET 10 + WPF | Nothing; exits after selecting a mode |
| Game Bar widget | C# + UWP XAML + Xbox Game Bar SDK | Game Bar widget lifetime |
| Sensor broker | .NET Framework 4.8 + LibreHardwareMonitor | Widget heartbeat; exits within seconds when it stops |
| Desktop overlay | .NET 10 + WPF + LibreHardwareMonitor | Visible overlay window |

The full-trust broker exists because a sandboxed UWP widget cannot read every hardware sensor directly. It writes only a local telemetry snapshot into the package's LocalState folder and never opens a network connection.

## Build from source

### Requirements

- Windows 10 version 1903 or newer; Windows 11 recommended
- Visual Studio 2022 or newer with **Universal Windows Platform development** and **.NET desktop development**
- Windows 11 SDK 10.0.26100
- .NET 10 SDK
- .NET Framework 4.8 developer pack
- Xbox Game Bar for live widget testing

Build every desktop component without launching it:

```powershell
..\build.ps1 -DesktopOnly
```

Build the full Game Bar package from a Developer PowerShell:

```powershell
..\build.ps1
```

Create a portable launcher and desktop-fallback package with both shortcuts:

```powershell
..\package.ps1
```

The UWP project needs the UWP/.NET Native reference assemblies installed by Visual Studio; a standalone .NET SDK is not sufficient.

## Privacy and anti-cheat posture

Quiet Monitor reads sensors locally and does not collect analytics, transmit data, inject into games, install a driver, or hook a rendering API. This avoids the riskiest anti-cheat integration pattern. No third-party project can guarantee acceptance by every game's policy, so check the rules for competitive titles when in doubt.

## Project links

- [Contributing](CONTRIBUTING.md)
- [Third-party notices](NOTICE.md)
- [MIT License](../LICENSE)
- [Build history and downloadable artifacts](https://github.com/flennium/quiet-monitor/actions/workflows/build.yml)
- [Issues](https://github.com/flennium/quiet-monitor/issues)

---

<div align="center">

Built for quick answers, not permanent background activity.

</div>
