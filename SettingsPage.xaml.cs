using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace QuietMonitor
{
    public sealed partial class SettingsPage : Page
    {
        private readonly Windows.Foundation.Collections.IPropertySet _values = ApplicationData.Current.LocalSettings.Values;

        public SettingsPage()
        {
            InitializeComponent();
            OpacitySlider.Value = ReadDouble("opacity", 0.9) * 100;
            SelectRefresh(ReadInt("refreshMs", 1000));
            CpuLoadCheck.IsChecked = ReadBool("cpuLoad", true);
            CpuTemperatureCheck.IsChecked = ReadBool("cpuTemperature", true);
            GpuLoadCheck.IsChecked = ReadBool("gpuLoad", true);
            GpuTemperatureCheck.IsChecked = ReadBool("gpuTemperature", true);
            GpuHotspotCheck.IsChecked = ReadBool("gpuHotspot", true);
            GpuPowerCheck.IsChecked = ReadBool("gpuPower", true);
            GpuFanCheck.IsChecked = ReadBool("gpuFan", false);
            MemoryCheck.IsChecked = ReadBool("memory", true);
            VramCheck.IsChecked = ReadBool("vram", true);
        }

        private void SaveClicked(object sender, RoutedEventArgs e)
        {
            _values["opacity"] = OpacitySlider.Value / 100;
            _values["refreshMs"] = int.Parse(((ComboBoxItem)RefreshCombo.SelectedItem).Tag.ToString());
            _values["cpuLoad"] = CpuLoadCheck.IsChecked == true;
            _values["cpuTemperature"] = CpuTemperatureCheck.IsChecked == true;
            _values["gpuLoad"] = GpuLoadCheck.IsChecked == true;
            _values["gpuTemperature"] = GpuTemperatureCheck.IsChecked == true;
            _values["gpuHotspot"] = GpuHotspotCheck.IsChecked == true;
            _values["gpuPower"] = GpuPowerCheck.IsChecked == true;
            _values["gpuFan"] = GpuFanCheck.IsChecked == true;
            _values["memory"] = MemoryCheck.IsChecked == true;
            _values["vram"] = VramCheck.IsChecked == true;
            StatusText.Text = "Saved";
        }

        private void OpacityChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (OpacityValue != null) OpacityValue.Text = $"{e.NewValue:0}%";
        }

        private void SelectRefresh(int milliseconds)
        {
            foreach (ComboBoxItem item in RefreshCombo.Items)
            {
                if (item.Tag?.ToString() == milliseconds.ToString())
                {
                    RefreshCombo.SelectedItem = item;
                    return;
                }
            }
            RefreshCombo.SelectedIndex = 1;
        }

        private bool ReadBool(string key, bool fallback) => _values.ContainsKey(key) && _values[key] is bool value ? value : fallback;
        private int ReadInt(string key, int fallback) => _values.ContainsKey(key) && _values[key] is int value ? value : fallback;
        private double ReadDouble(string key, double fallback) => _values.ContainsKey(key) && _values[key] is double value ? value : fallback;
    }
}
