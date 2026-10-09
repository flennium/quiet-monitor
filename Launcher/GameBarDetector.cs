using Microsoft.Win32;

namespace QuietMonitor.Launcher;

public static class GameBarDetector
{
    public static bool IsAvailable() => HasProtocolHandler() && HasXboxGameBarPackage() && IsEnabledByWindowsSettings();

    private static bool HasProtocolHandler()
    {
        using var key = Registry.ClassesRoot.OpenSubKey("ms-gamebar");
        return key is not null;
    }

    private static bool HasXboxGameBarPackage()
    {
        try
        {
            using var packages = Registry.CurrentUser.OpenSubKey(
                @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");
            return packages?.GetSubKeyNames().Any(name =>
                name.StartsWith("Microsoft.XboxGamingOverlay_", StringComparison.OrdinalIgnoreCase)) == true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsEnabledByWindowsSettings()
    {
        return ReadDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled") != 0
            && ReadDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled") != 0;
    }

    private static int ReadDword(RegistryKey root, string path, string name)
    {
        using var key = root.OpenSubKey(path);
        return key?.GetValue(name) is int value ? value : 1;
    }
}
