using System.Diagnostics;
using System.IO;
using System.Windows;

namespace QuietMonitor.Launcher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Any(arg => arg.Equals("--settings", StringComparison.OrdinalIgnoreCase)))
        {
            MainWindow = new SettingsWindow();
            MainWindow.Show();
            return;
        }

        LaunchPreferredMode();
        Shutdown();
    }

    public static LaunchResult LaunchPreferredMode()
    {
        var settings = LauncherSettings.Load();
        if (settings.UseGameBarWhenAvailable && GameBarDetector.IsAvailable())
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-gamebar:") { UseShellExecute = true });
                return new LaunchResult(true, "Xbox Game Bar opened. Choose Quiet Monitor and pin it once to keep it over games.");
            }
            catch
            {
                // A stale protocol registration should not prevent the desktop fallback.
            }
        }

        var overlayPath = FindDesktopOverlay();
        if (overlayPath is null)
            return new LaunchResult(false, "The desktop overlay executable was not found. Reinstall Quiet Monitor or use the complete release package.");

        Process.Start(new ProcessStartInfo(overlayPath) { UseShellExecute = true });
        return new LaunchResult(true, settings.UseGameBarWhenAvailable
            ? "Xbox Game Bar is unavailable, so the desktop overlay was opened."
            : "The desktop overlay was opened.");
    }

    private static string? FindDesktopOverlay()
    {
        var baseDirectory = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(baseDirectory, "QuietMonitor.DesktopOverlay.exe"),
            Path.Combine(baseDirectory, "DesktopOverlay", "QuietMonitor.DesktopOverlay.exe"),
            Path.GetFullPath(Path.Combine(baseDirectory, "..", "DesktopOverlay", "QuietMonitor.DesktopOverlay.exe"))
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}

public sealed record LaunchResult(bool Success, string Message);
