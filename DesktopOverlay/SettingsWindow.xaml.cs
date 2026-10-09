using System.Windows;
using System.Windows.Controls;
using QuietMonitor.Models;
using QuietMonitor.Services;

namespace QuietMonitor;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        LoadControls();
    }

    private void LoadControls()
    {
        SelectTag(PositionCombo, _settings.Position);
        SelectTag(ScaleCombo, _settings.Scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        SelectTag(RefreshCombo, _settings.RefreshIntervalMs.ToString());
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

    private void SaveChanges_Click(object sender, RoutedEventArgs e)
    {
        _settings = new AppSettings
        {
            Position = SelectedTag(PositionCombo),
            Scale = double.Parse(SelectedTag(ScaleCombo), System.Globalization.CultureInfo.InvariantCulture),
            RefreshIntervalMs = int.Parse(SelectedTag(RefreshCombo)),
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
        _settingsService.Save(_settings);
        ((App)Application.Current).ApplySettings(_settings);
        StatusText.Text = "Changes saved and applied";
    }

    private static void SelectTag(ComboBox comboBox, string tag)
    {
        comboBox.SelectedItem = comboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => Equals(item.Tag?.ToString(), tag)) ?? comboBox.Items[0];
    }

    private static string SelectedTag(ComboBox comboBox) => ((ComboBoxItem)comboBox.SelectedItem).Tag?.ToString() ?? string.Empty;

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValueText is not null) OpacityValueText.Text = $"{e.NewValue:0}%";
    }

    private void StopOverlay_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
