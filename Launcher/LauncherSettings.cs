using System.IO;
using System.Reflection;
using System.Text.Json;

namespace QuietMonitor.Launcher;

public sealed class LauncherSettings
{
    public bool UseGameBarWhenAvailable { get; set; } = true;
    public string Hotkey { get; set; } = "CTRL+ALT+Q";
    public int RefreshIntervalMs { get; set; } = 1000;
    public string Position { get; set; } = "TopRight";
    public double Scale { get; set; } = 1.0;
    public double Opacity { get; set; } = 0.9;
    public bool ShowCpuLoad { get; set; } = true;
    public bool ShowCpuTemperature { get; set; } = true;
    public bool ShowGpuLoad { get; set; } = true;
    public bool ShowGpuTemperature { get; set; } = true;
    public bool ShowGpuHotspot { get; set; } = true;
    public bool ShowGpuPower { get; set; } = true;
    public bool ShowGpuFan { get; set; }
    public bool ShowMemory { get; set; } = true;
    public bool ShowVram { get; set; } = true;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuietMonitor");
    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static LauncherSettings Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(SettingsPath)) ?? new LauncherSettings()
                : new LauncherSettings();
        }
        catch
        {
            return new LauncherSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(SettingsPath, json);
        SyncGameBarSettings(json);
        ApplyShortcutHotkey();
    }

    private static void SyncGameBarSettings(string json)
    {
        try
        {
            var packages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
            var package = Directory.Exists(packages)
                ? Directory.EnumerateDirectories(packages, "flennium.QuietMonitor_*").FirstOrDefault()
                : null;
            if (package == null) return;
            var localState = Path.Combine(package, "LocalState");
            Directory.CreateDirectory(localState);
            File.WriteAllText(Path.Combine(localState, "settings.json"), json);
        }
        catch
        {
            // The desktop settings remain valid if Windows temporarily locks package storage.
        }
    }

    private void ApplyShortcutHotkey()
    {
        string[] shortcuts =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Quiet Monitor.lnk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "Quiet Monitor", "Quiet Monitor.lnk")
        };

        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return;
        var shell = Activator.CreateInstance(shellType);
        if (shell == null) return;
        try
        {
            foreach (var shortcutPath in shortcuts.Where(File.Exists))
            {
                var shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                if (shortcut == null) continue;
                shortcut.GetType().InvokeMember("Hotkey", BindingFlags.SetProperty, null, shortcut, new object[] { Hotkey });
                shortcut.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                if (System.Runtime.InteropServices.Marshal.IsComObject(shortcut)) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
            }
        }
        finally
        {
            if (System.Runtime.InteropServices.Marshal.IsComObject(shell)) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }
    }
}
