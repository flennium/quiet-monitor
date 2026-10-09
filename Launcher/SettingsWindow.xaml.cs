using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace QuietMonitor.Launcher;

public partial class SettingsWindow : Window
{
    private LauncherSettings _settings;
    private string _hotkey = "CTRL+ALT+Q";
    private bool _isRecordingHotkey;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = LauncherSettings.Load();
        LoadControls();
        var available = GameBarDetector.IsAvailable();
        GameBarStatus.Text = available ? "Installed and enabled" : "Unavailable or disabled — desktop fallback will be used";
        AutomationProperties.SetName(GameBarStatus, $"Xbox Game Bar: {GameBarStatus.Text}");
        StatusDot.Fill = Brush(available ? "#6FD4B0" : "#F1B760");
    }

    private void LoadControls()
    {
        PreferGameBarCheck.IsChecked = _settings.UseGameBarWhenAvailable;
        _hotkey = _settings.Hotkey;
        HotkeyRecordButton.Content = HotkeyDisplay(_hotkey);
        SelectTag(ThemeCombo, _settings.Theme);
        ApplyTheme(_settings.Theme);
        SelectTag(PositionCombo, _settings.Position);
        SelectTag(ScaleCombo, _settings.Scale.ToString("0.##", CultureInfo.InvariantCulture));
        SelectTag(RefreshCombo, _settings.RefreshIntervalMs.ToString(CultureInfo.InvariantCulture));
        OpacitySlider.Value = _settings.Opacity * 100;
        CpuLoadCheck.IsChecked = _settings.ShowCpuLoad;
        CpuTemperatureCheck.IsChecked = _settings.ShowCpuTemperature;
        GpuLoadCheck.IsChecked = _settings.ShowGpuLoad;
        GpuTemperatureCheck.IsChecked = _settings.ShowGpuTemperature;
        GpuHotspotCheck.IsChecked = _settings.ShowGpuHotspot;
        GpuPowerCheck.IsChecked = _settings.ShowGpuPower;
        GpuFanCheck.IsChecked = _settings.ShowGpuFan;
        MemoryCheck.IsChecked = _settings.ShowMemory;
        VramCheck.IsChecked = _settings.ShowVram;
    }

    private void Save()
    {
        _settings = new LauncherSettings
        {
            UseGameBarWhenAvailable = PreferGameBarCheck.IsChecked == true,
            Hotkey = _hotkey,
            Theme = SelectedTag(ThemeCombo),
            Position = SelectedTag(PositionCombo),
            Scale = double.Parse(SelectedTag(ScaleCombo), CultureInfo.InvariantCulture),
            RefreshIntervalMs = int.Parse(SelectedTag(RefreshCombo), CultureInfo.InvariantCulture),
            Opacity = OpacitySlider.Value / 100,
            ShowCpuLoad = CpuLoadCheck.IsChecked == true,
            ShowCpuTemperature = CpuTemperatureCheck.IsChecked == true,
            ShowGpuLoad = GpuLoadCheck.IsChecked == true,
            ShowGpuTemperature = GpuTemperatureCheck.IsChecked == true,
            ShowGpuHotspot = GpuHotspotCheck.IsChecked == true,
            ShowGpuPower = GpuPowerCheck.IsChecked == true,
            ShowGpuFan = GpuFanCheck.IsChecked == true,
            ShowMemory = MemoryCheck.IsChecked == true,
            ShowVram = VramCheck.IsChecked == true
        };
        _settings.Save();
        FeedbackText.Foreground = Brush("#6FD4B0");
        FeedbackText.Text = "Saved for both overlay modes";
    }

    private void Save_Click(object sender, RoutedEventArgs e) => Save();

    private void OpenMonitor_Click(object sender, RoutedEventArgs e)
    {
        Save();
        var result = App.LaunchPreferredMode();
        FeedbackText.Foreground = Brush(result.Success ? "#6FD4B0" : "#FF806F");
        FeedbackText.Text = result.Message;
    }

    private void OpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValueText != null) OpacityValueText.Text = $"{e.NewValue:0}%";
    }

    private void RecordHotkey_Click(object sender, RoutedEventArgs e)
    {
        _isRecordingHotkey = true;
        HotkeyRecordButton.Content = "Press shortcut…";
        HotkeyRecordButton.Background = (Brush)FindResource("Accent");
        HotkeyRecordButton.Foreground = (Brush)FindResource("AccentInk");
        HotkeyFeedbackText.Text = "Recording. Press Ctrl, Alt, or Shift with a letter, number, or function key. Press Escape to cancel.";
        AnnounceHotkeyFeedback();
        Keyboard.Focus(HotkeyRecordButton);
    }

    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        _isRecordingHotkey = false;
        _hotkey = string.Empty;
        HotkeyRecordButton.Content = "Not set";
        HotkeyRecordButton.ClearValue(Control.BackgroundProperty);
        HotkeyRecordButton.ClearValue(Control.ForegroundProperty);
        HotkeyFeedbackText.Text = "Shortcut disabled. Save to apply it.";
        AnnounceHotkeyFeedback();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isRecordingHotkey) return;
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            _isRecordingHotkey = false;
            HotkeyRecordButton.Content = HotkeyDisplay(_hotkey);
            HotkeyRecordButton.ClearValue(Control.BackgroundProperty);
            HotkeyRecordButton.ClearValue(Control.ForegroundProperty);
            HotkeyFeedbackText.Text = "Shortcut recording cancelled.";
            AnnounceHotkeyFeedback();
            return;
        }
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;
        var modifiers = Keyboard.Modifiers;
        var recorded = BuildHotkey(key, modifiers);
        if (recorded == null)
        {
            HotkeyFeedbackText.Foreground = (Brush)FindResource("Warning");
            HotkeyFeedbackText.Text = "Use Ctrl, Alt, or Shift with a letter, number, or function key.";
            AnnounceHotkeyFeedback();
            return;
        }
        _hotkey = recorded;
        _isRecordingHotkey = false;
        HotkeyRecordButton.Content = HotkeyDisplay(_hotkey);
        HotkeyRecordButton.ClearValue(Control.BackgroundProperty);
        HotkeyRecordButton.ClearValue(Control.ForegroundProperty);
        HotkeyFeedbackText.Foreground = (Brush)FindResource("Good");
        HotkeyFeedbackText.Text = $"Recorded {HotkeyDisplay(_hotkey)}. Save to apply it.";
        AnnounceHotkeyFeedback();
    }

    internal static string? BuildHotkey(Key key, ModifierKeys modifiers)
    {
        if ((modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.None) return null;
        var keyName = key >= Key.A && key <= Key.Z ? key.ToString().ToUpperInvariant()
            : key >= Key.D0 && key <= Key.D9 ? ((int)(key - Key.D0)).ToString()
            : key >= Key.F1 && key <= Key.F24 ? key.ToString().ToUpperInvariant()
            : null;
        if (keyName == null) return null;
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("CTRL");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("ALT");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("SHIFT");
        parts.Add(keyName);
        return string.Join("+", parts);
    }

    private void ThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo?.SelectedItem is ComboBoxItem) ApplyTheme(SelectedTag(ThemeCombo));
    }

    private static void ApplyTheme(string theme)
    {
        var light = theme == "Light" || theme == "System" && IsWindowsLightTheme();
        SetBrush("Ink", light ? "#F4F7F9" : "#09111B");
        SetBrush("Panel", light ? "#FFFFFF" : "#101C29");
        SetBrush("Raised", light ? "#E7EEF3" : "#172638");
        SetBrush("Line", light ? "#C8D5DE" : "#2B4055");
        SetBrush("Text", light ? "#14222D" : "#F4F8FB");
        SetBrush("Muted", light ? "#536B7B" : "#A8BAC9");
        SetBrush("ControlSurface", light ? "#FFFFFF" : "#172638");
        SetBrush("ControlText", light ? "#10202C" : "#F4F8FB");
    }

    private void AnnounceHotkeyFeedback()
    {
        var peer = UIElementAutomationPeer.FromElement(HotkeyFeedbackText) ?? new FrameworkElementAutomationPeer(HotkeyFeedbackText);
        peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private static bool IsWindowsLightTheme() =>
        Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0) is int value && value != 0;

    private static void SetBrush(string key, string hex) => Application.Current.Resources[key] = Brush(hex);
    private static string HotkeyDisplay(string hotkey) => string.IsNullOrWhiteSpace(hotkey)
        ? "Not set"
        : string.Join(" + ", hotkey.Split('+').Select(part => part.Length <= 1 ? part : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));

    private static void SelectTag(ComboBox combo, string tag)
    {
        combo.SelectedItem = combo.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Tag?.ToString() == tag) ?? combo.Items[0];
    }

    private static string SelectedTag(ComboBox combo) => ((ComboBoxItem)combo.SelectedItem).Tag?.ToString() ?? string.Empty;
    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private void Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
