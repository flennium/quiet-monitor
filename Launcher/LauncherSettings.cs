using System.IO;
using System.Text.Json;

namespace QuietMonitor.Launcher;

public sealed class LauncherSettings
{
    public bool UseGameBarWhenAvailable { get; set; } = true;

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuietMonitor");
    private static string SettingsPath => Path.Combine(SettingsDirectory, "launcher-settings.json");

    public static LauncherSettings Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(SettingsPath)) ?? new LauncherSettings()
                : new LauncherSettings();
        }
        catch (JsonException)
        {
            return new LauncherSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
