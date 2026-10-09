using System;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.ViewManagement;

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
            SelectTheme(ReadString("Theme", "System"));
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
            _settings["Theme"] = JsonValue.CreateStringValue(((ComboBoxItem)ThemeCombo.SelectedItem).Tag.ToString());
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

        private void ThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeCombo?.SelectedItem is ComboBoxItem item) ApplyTheme(item.Tag?.ToString() ?? "System");
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

        private void SelectTheme(string theme)
        {
            foreach (ComboBoxItem item in ThemeCombo.Items)
                if (item.Tag?.ToString() == theme) { ThemeCombo.SelectedItem = item; ApplyTheme(theme); return; }
            ThemeCombo.SelectedIndex = 0;
        }

        private void ApplyTheme(string theme)
        {
            var light = theme == "Light" || theme == "System" && new UISettings().GetColorValue(UIColorType.Background).R > 127;
            RequestedTheme = light ? ElementTheme.Light : theme == "Dark" ? ElementTheme.Dark : ElementTheme.Default;
            SetBrush("Ink", light ? Color.FromArgb(255, 244, 247, 249) : Color.FromArgb(255, 16, 21, 29));
            SetBrush("Panel", light ? Colors.White : Color.FromArgb(255, 23, 30, 40));
            SetBrush("Raised", light ? Color.FromArgb(255, 231, 238, 243) : Color.FromArgb(255, 32, 41, 54));
            SetBrush("Line", light ? Color.FromArgb(255, 200, 213, 222) : Color.FromArgb(255, 51, 65, 85));
            SetBrush("Paper", light ? Color.FromArgb(255, 20, 34, 45) : Color.FromArgb(255, 243, 240, 232));
            SetBrush("Muted", light ? Color.FromArgb(255, 83, 107, 123) : Color.FromArgb(255, 170, 180, 194));
            SetBrush("Blue", light ? Color.FromArgb(255, 8, 127, 150) : Color.FromArgb(255, 89, 214, 231));
        }

        private static void SetBrush(string key, Color color)
        {
            if (Application.Current.Resources[key] is SolidColorBrush brush) brush.Color = color;
        }

        private bool ReadBool(string key, bool fallback) => _settings.ContainsKey(key) && _settings[key].ValueType == JsonValueType.Boolean ? _settings[key].GetBoolean() : fallback;
        private int ReadInt(string key, int fallback) => _settings.ContainsKey(key) && _settings[key].ValueType == JsonValueType.Number ? (int)_settings[key].GetNumber() : fallback;
        private double ReadDouble(string key, double fallback) => _settings.ContainsKey(key) && _settings[key].ValueType == JsonValueType.Number ? _settings[key].GetNumber() : fallback;
        private string ReadString(string key, string fallback) => _settings.ContainsKey(key) && _settings[key].ValueType == JsonValueType.String ? _settings[key].GetString() : fallback;
    }
}
