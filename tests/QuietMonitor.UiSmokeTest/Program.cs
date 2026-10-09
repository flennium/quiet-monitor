using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

internal static class Program
{
    private const string InstallDirectory = @"C:\Program Files\Quiet Monitor";
    private static readonly string RepositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    private static readonly string LauncherBuildDirectory = Path.Combine(RepositoryRoot, "Launcher", "bin", "Release", "net10.0-windows", "win-x64");
    private static readonly string ReportPath = Path.Combine(AppContext.BaseDirectory, "ui-smoke-report.txt");
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuietMonitor",
        "settings.json");

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            File.WriteAllText(ReportPath, string.Empty);
            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                var candidate = Path.Combine(LauncherBuildDirectory, $"{name.Name}.dll");
                if (!File.Exists(candidate)) candidate = Path.Combine(InstallDirectory, $"{name.Name}.dll");
                return File.Exists(candidate) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate) : null;
            };

            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(LauncherBuildDirectory, "QuietMonitor.dll"));
            var windowType = assembly.GetType("QuietMonitor.Launcher.SettingsWindow", throwOnError: true)!;
            var appType = assembly.GetType("QuietMonitor.Launcher.App", throwOnError: true)!;
            var app = (Application)Activator.CreateInstance(appType)!;
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            appType.GetMethod("InitializeComponent")!.Invoke(app, null);
            var buildHotkey = windowType.GetMethod("BuildHotkey", BindingFlags.Static | BindingFlags.NonPublic)!;
            Assert((string?)buildHotkey.Invoke(null, new object[] { Key.F10, ModifierKeys.Control | ModifierKeys.Shift }) == "CTRL+SHIFT+F10", "keybind recorder accepts arbitrary supported chords");
            Assert(buildHotkey.Invoke(null, new object[] { Key.Q, ModifierKeys.None }) == null, "keybind recorder rejects unmodified typing keys");

            using (var first = new WindowScope(CreateWindow(windowType)))
            {
                Assert(Check(first.Window, "PreferGameBarCheck").IsChecked == true, "Game Bar preference defaults on");
                Assert(FindButton(first.Window, "HotkeyRecordButton").Content?.ToString() == "Ctrl + Alt + Q", "hotkey recorder shows the default chord");
                Assert(SelectedTag(Combo(first.Window, "ThemeCombo")) == "System", "theme follows Windows by default");
                Assert(SelectedTag(Combo(first.Window, "PositionCombo")) == "TopRight", "position defaults to top right");
                Assert(Math.Abs(Slider(first.Window, "OpacitySlider").Value - 90) < 0.01, "opacity defaults to 90%");
                Assert(Combo(first.Window, "ThemeCombo").Foreground.ToString().Equals("#FFF4F8FB", StringComparison.OrdinalIgnoreCase), "dark combo-box text has readable contrast");
                Assert(Text(first.Window, "GameBarStatus").Text.Contains("Unavailable", StringComparison.OrdinalIgnoreCase), "missing Xbox Game Bar host is detected");
                SelectTag(Combo(first.Window, "ThemeCombo"), "Light");
                Assert(first.Window.Background.ToString().Equals("#FFF4F7F9", StringComparison.OrdinalIgnoreCase), "light theme applies immediately");
                FindButton(first.Window, "HotkeyRecordButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(FindButton(first.Window, "HotkeyRecordButton").Content?.ToString() == "Press shortcut…", "shortcut control enters recording mode");

                Check(first.Window, "PreferGameBarCheck").IsChecked = false;
                SetHotkey(first.Window, "CTRL+SHIFT+F10");
                SelectTag(Combo(first.Window, "ThemeCombo"), "Dark");
                SelectTag(Combo(first.Window, "PositionCombo"), "BottomLeft");
                SelectTag(Combo(first.Window, "ScaleCombo"), "1.2");
                SelectTag(Combo(first.Window, "RefreshCombo"), "2000");
                Slider(first.Window, "OpacitySlider").Value = 75;
                Check(first.Window, "CpuTemperatureCheck").IsChecked = false;
                Check(first.Window, "GpuFanCheck").IsChecked = true;
                Click(first.Window, "Save");
                Assert(Text(first.Window, "FeedbackText").Text == "Saved for both overlay modes", "Save button reports success");
            }

            ValidateSavedSettings(expectDefaults: false);
            ValidateShortcut("Ctrl+Shift+F10");
            ValidatePackageSettingsCopy();
            if (args.Contains("--leave-custom", StringComparer.OrdinalIgnoreCase))
            {
                File.AppendAllText(ReportPath, "PASS: custom overlay settings left in place for rendered-window testing\n");
                Application.Current.Shutdown();
                return 0;
            }

            using (var persisted = new WindowScope(CreateWindow(windowType)))
            {
                Assert(Check(persisted.Window, "PreferGameBarCheck").IsChecked == false, "Game Bar preference reloads");
                Assert(FindButton(persisted.Window, "HotkeyRecordButton").Content?.ToString() == "Ctrl + Shift + F10", "recorded hotkey reloads");
                Assert(SelectedTag(Combo(persisted.Window, "ThemeCombo")) == "Dark", "theme reloads");
                Assert(SelectedTag(Combo(persisted.Window, "PositionCombo")) == "BottomLeft", "position reloads");
                Assert(Math.Abs(Slider(persisted.Window, "OpacitySlider").Value - 75) < 0.01, "opacity reloads");
                Assert(Check(persisted.Window, "CpuTemperatureCheck").IsChecked == false, "disabled metric reloads");
                Assert(Check(persisted.Window, "GpuFanCheck").IsChecked == true, "enabled metric reloads");

                FindButton(persisted.Window, "ClearHotkeyButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Click(persisted.Window, "Save");
                using (var disabled = JsonDocument.Parse(File.ReadAllText(SettingsPath)))
                    Assert(disabled.RootElement.GetProperty("Hotkey").GetString() == string.Empty, "Off keybind persists");
                ValidateShortcut(string.Empty);

                Check(persisted.Window, "PreferGameBarCheck").IsChecked = true;
                SetHotkey(persisted.Window, "CTRL+ALT+Q");
                SelectTag(Combo(persisted.Window, "ThemeCombo"), "System");
                SelectTag(Combo(persisted.Window, "PositionCombo"), "TopRight");
                SelectTag(Combo(persisted.Window, "ScaleCombo"), "1");
                SelectTag(Combo(persisted.Window, "RefreshCombo"), "1000");
                Slider(persisted.Window, "OpacitySlider").Value = 90;
                Check(persisted.Window, "CpuTemperatureCheck").IsChecked = true;
                Check(persisted.Window, "GpuFanCheck").IsChecked = false;
                Click(persisted.Window, "Save");
            }

            ValidateSavedSettings(expectDefaults: true);
            ValidateShortcut("Alt+Ctrl+Q");
            ValidatePackageSettingsCopy();
            Console.WriteLine("PASS: installed control-center UI defaults, Save action, persistence, package sync, and shortcut hotkey");
            File.AppendAllText(ReportPath, "PASS: installed control-center UI defaults, Save action, persistence, package sync, and shortcut hotkey\n");
            Application.Current.Shutdown();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception}");
            File.AppendAllText(ReportPath, $"FAIL: {exception}\n");
            Application.Current?.Shutdown();
            return 1;
        }
    }

    private static Window CreateWindow(Type type) => (Window)Activator.CreateInstance(type)!;
    private static CheckBox Check(Window window, string name) => (CheckBox)window.FindName(name);
    private static ComboBox Combo(Window window, string name) => (ComboBox)window.FindName(name);
    private static Slider Slider(Window window, string name) => (Slider)window.FindName(name);
    private static Button FindButton(Window window, string name) => (Button)window.FindName(name);
    private static TextBlock Text(Window window, string name) => (TextBlock)window.FindName(name);

    private static string SelectedTag(ComboBox combo) => ((ComboBoxItem)combo.SelectedItem).Tag?.ToString() ?? string.Empty;

    private static void SelectTag(ComboBox combo, string tag)
    {
        combo.SelectedItem = combo.Items.Cast<ComboBoxItem>().Single(item => (item.Tag?.ToString() ?? string.Empty) == tag);
    }

    private static void SetHotkey(Window window, string hotkey)
    {
        window.GetType().GetField("_hotkey", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, hotkey);
        FindButton(window, "HotkeyRecordButton").Content = string.Join(" + ", hotkey.Split('+').Select(part => part.Length == 1 ? part : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }

    private static void Click(DependencyObject root, string content)
    {
        var button = Descendants(root).OfType<Button>().Single(item => item.Content?.ToString() == content);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private static void ValidateSavedSettings(bool expectDefaults)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(SettingsPath));
        var root = json.RootElement;
        Assert(root.GetProperty("UseGameBarWhenAvailable").GetBoolean() == expectDefaults, "Game Bar setting persisted");
        Assert(root.GetProperty("Hotkey").GetString() == (expectDefaults ? "CTRL+ALT+Q" : "CTRL+SHIFT+F10"), "hotkey persisted");
        Assert(root.GetProperty("Theme").GetString() == (expectDefaults ? "System" : "Dark"), "theme persisted");
        Assert(root.GetProperty("Position").GetString() == (expectDefaults ? "TopRight" : "BottomLeft"), "position persisted");
        Assert(root.GetProperty("RefreshIntervalMs").GetInt32() == (expectDefaults ? 1000 : 2000), "refresh interval persisted");
        Assert(Math.Abs(root.GetProperty("Opacity").GetDouble() - (expectDefaults ? 0.9 : 0.75)) < 0.001, "opacity persisted");
        Assert(root.GetProperty("ShowCpuTemperature").GetBoolean() == expectDefaults, "CPU temperature visibility persisted");
        Assert(root.GetProperty("ShowGpuFan").GetBoolean() != expectDefaults, "GPU fan visibility persisted");
    }

    private static void ValidateShortcut(string expectedHotkey)
    {
        var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Quiet Monitor", "Quiet Monitor.lnk");
        var shellType = Type.GetTypeFromProgID("WScript.Shell", throwOnError: true)!;
        var shell = Activator.CreateInstance(shellType)!;
        var shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath })!;
        var actual = shortcut.GetType().InvokeMember("Hotkey", BindingFlags.GetProperty, null, shortcut, null)?.ToString();
        Assert(actual == expectedHotkey, $"shortcut hotkey is {expectedHotkey} (actual: {actual})");
    }

    private static void ValidatePackageSettingsCopy()
    {
        var packages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
        var package = Directory.EnumerateDirectories(packages, "flennium.QuietMonitor_*").Single();
        var packageSettings = Path.Combine(package, "LocalState", "settings.json");
        Assert(File.Exists(packageSettings), "Game Bar settings copy exists");
        Assert(File.ReadAllText(packageSettings) == File.ReadAllText(SettingsPath), "Game Bar settings copy matches desktop settings");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine($"PASS: {message}");
        File.AppendAllText(ReportPath, $"PASS: {message}\n");
    }

    private sealed class WindowScope(Window window) : IDisposable
    {
        public Window Window { get; } = window;
        public void Dispose() => Window.Close();
    }
}
