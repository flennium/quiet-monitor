# Testing Quiet Monitor

Quiet Monitor's release acceptance test starts from a removed installation and exercises the installer, package registration, control-center UI, settings persistence, shortcut keybinds, desktop overlay, live sensors, and uninstall cleanup.

## Automated local smoke tests

Run the control-center test on Windows after installing the unified package:

```powershell
dotnet build Launcher/QuietMonitor.Launcher.csproj -c Release
dotnet run --project tests/QuietMonitor.UiSmokeTest/QuietMonitor.UiSmokeTest.csproj -c Release
```

The test covers defaults, readable control styling, Game Bar availability detection, Save feedback, every persisted setting group, Game Bar settings synchronization, keybind changes, keybind Off, and restoration of `Ctrl+Alt+Q`.

Run the hardware-reader test on the target PC:

```powershell
dotnet run --project tests/QuietMonitor.SensorBroker.SmokeTest/QuietMonitor.SensorBroker.SmokeTest.csproj -c Release
```

It validates live CPU, GPU, RAM, VRAM, and temperature ranges plus the JSON contract consumed by the Game Bar widget.

## Manual release checklist

1. Uninstall the previous desktop app and remove the previous `flennium.QuietMonitor` AppX package.
2. Confirm no Quiet Monitor process, installation directory, package data, settings directory, shortcut, or trusted package certificate remains.
3. Install the signed unified installer and verify its exit code and log.
4. Confirm the desktop version and AppX manifest version match `VERSION`.
5. Open the control center and visually inspect all sections, scrolling, contrast, status text, and buttons.
6. Exercise each keybind option, including Off, and verify the per-user Start Menu shortcut changes without elevation.
7. Test top-left, top-right, bottom-left, and bottom-right placement; compact, balanced, and large sizes; opacity; refresh interval; and every reading toggle.
8. Start the desktop overlay twice and confirm the second invocation closes it. Confirm its window is topmost, click-through, absent from the taskbar, and leaves no process after closing.
9. When Xbox Game Bar is installed and enabled, open and pin the Quiet Monitor widget, open its settings widget, verify live readings, close it, and confirm the sensor broker exits after its heartbeat timeout.
10. When Xbox Game Bar is absent or disabled, confirm the launcher automatically opens the desktop overlay.
11. Uninstall and repeat the clean-state checks from step 2.

Exclusive fullscreen coverage requires Xbox Game Bar. The desktop overlay is intended for borderless and windowed applications.
