using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuietMonitor.Launcher;

public partial class SettingsWindow : Window
{
    private LauncherSettings _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = LauncherSettings.Load();
        LoadControls();
        var available = GameBarDetector.IsAvailable();
        GameBarStatus.Text = available ? "Installed and enabled" : "Unavailable or disabled — desktop fallback will be used";
        StatusDot.Fill = Brush(available ? "#6FD4B0" : "#F1B760");
    }

    private void LoadControls()
    {
        PreferGameBarCheck.IsChecked = _settings.UseGameBarWhenAvailable;
        SelectTag(HotkeyCombo, _settings.Hotkey);
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
            Hotkey = SelectedTag(HotkeyCombo),
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

    private static void SelectTag(ComboBox combo, string tag)
    {
        combo.SelectedItem = combo.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Tag?.ToString() == tag) ?? combo.Items[0];
    }

    private static string SelectedTag(ComboBox combo) => ((ComboBoxItem)combo.SelectedItem).Tag?.ToString() ?? string.Empty;
    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private void Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
