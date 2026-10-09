using System;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace QuietMonitor
{
    public sealed partial class SettingsPage : Page
    {
        private JsonObject _settings = new JsonObject();

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync("settings.json");
                _settings = JsonObject.Parse(await FileIO.ReadTextAsync(file));
            }
            catch
            {
                _settings = new JsonObject();
            }

            OpacitySlider.Value = ReadDouble("Opacity", 0.9) * 100;
            SelectRefresh(ReadInt("RefreshIntervalMs", 1000));
            CpuLoadCheck.IsChecked = ReadBool("ShowCpuLoad", true);
            CpuTemperatureCheck.IsChecked = ReadBool("ShowCpuTemperature", true);
            GpuLoadCheck.IsChecked = ReadBool("ShowGpuLoad", true);
            GpuTemperatureCheck.IsChecked = ReadBool("ShowGpuTemperature", true);
            GpuHotspotCheck.IsChecked = ReadBool("ShowGpuHotspot", true);
            GpuPowerCheck.IsChecked = ReadBool("ShowGpuPower", true);
            GpuFanCheck.IsChecked = ReadBool("ShowGpuFan", false);
            MemoryCheck.IsChecked = ReadBool("ShowMemory", true);
            VramCheck.IsChecked = ReadBool("ShowVram", true);
        }

        private async void SaveClicked(object sender, RoutedEventArgs e)
        {
            _settings["Opacity"] = JsonValue.CreateNumberValue(OpacitySlider.Value / 100);
            _settings["RefreshIntervalMs"] = JsonValue.CreateNumberValue(int.Parse(((ComboBoxItem)RefreshCombo.SelectedItem).Tag.ToString()));
            SetBool("ShowCpuLoad", CpuLoadCheck);
            SetBool("ShowCpuTemperature", CpuTemperatureCheck);
            SetBool("ShowGpuLoad", GpuLoadCheck);
            SetBool("ShowGpuTemperature", GpuTemperatureCheck);
            SetBool("ShowGpuHotspot", GpuHotspotCheck);
            SetBool("ShowGpuPower", GpuPowerCheck);
            SetBool("ShowGpuFan", GpuFanCheck);
            SetBool("ShowMemory", MemoryCheck);
            SetBool("ShowVram", VramCheck);
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("settings.json", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, _settings.Stringify());
            StatusText.Text = "Saved for the Game Bar overlay";
        }

        private void SetBool(string key, CheckBox checkBox) => _settings[key] = JsonValue.CreateBooleanValue(checkBox.IsChecked == true);

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

        private bool ReadBool(string key, bool fallback) => _settings.ContainsKey(key) && _settings[key].ValueType == JsonValueType.Boolean ? _settings[key].GetBoolean() : fallback;
        private int ReadInt(string key, int fallback) => _settings.ContainsKey(key) && _settings[key].ValueType == JsonValueType.Number ? (int)_settings[key].GetNumber() : fallback;
        private double ReadDouble(string key, double fallback) => _settings.ContainsKey(key) && _settings[key].ValueType == JsonValueType.Number ? _settings[key].GetNumber() : fallback;
    }
}
