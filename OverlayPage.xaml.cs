using System;
using System.Threading.Tasks;
using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace QuietMonitor
{
    public sealed partial class OverlayPage : Page
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private XboxGameBarWidget _widget;
        private StorageFolder _localFolder;
        private bool _brokerStarted;
        private bool _polling;
        private JsonObject _settings = new JsonObject();

        public OverlayPage()
        {
            InitializeComponent();
            _timer.Tick += async (_, __) => await PollAsync();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _widget = e.Parameter as XboxGameBarWidget;
            _localFolder = ApplicationData.Current.LocalFolder;
            _widget.SettingsClicked += WidgetSettingsClicked;
            _widget.VisibleChanged += WidgetVisibleChanged;
            await LoadSettingsAsync();
            ApplySettings();
            await EnsureBrokerAsync();
            UpdateTimerState();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _timer.Stop();
            if (_widget != null)
            {
                _widget.SettingsClicked -= WidgetSettingsClicked;
                _widget.VisibleChanged -= WidgetVisibleChanged;
            }
            base.OnNavigatedFrom(e);
        }

        private async Task EnsureBrokerAsync()
        {
            if (_brokerStarted) return;
            await WriteHeartbeatAsync();
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            _brokerStarted = true;
        }

        private void WidgetVisibleChanged(XboxGameBarWidget sender, object args) => UpdateTimerState();

        private async void UpdateTimerState()
        {
            if (_widget != null && _widget.Visible)
            {
                await LoadSettingsAsync();
                ApplySettings();
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        private async void WidgetSettingsClicked(XboxGameBarWidget sender, object args) => await sender.ActivateSettingsAsync();

        private async Task PollAsync()
        {
            if (_polling) return;
            _polling = true;
            try
            {
                ApplySettings();
                await WriteHeartbeatAsync();
                var file = await _localFolder.GetFileAsync("telemetry.json");
                var json = JsonObject.Parse(await FileIO.ReadTextAsync(file));
                SetValues(json);
                LiveDot.Fill = new SolidColorBrush(Color.FromArgb(255, 111, 212, 176));
                LiveText.Text = "Live";
            }
            catch
            {
                LiveDot.Fill = new SolidColorBrush(Color.FromArgb(255, 240, 113, 103));
                LiveText.Text = "Waiting for sensors";
            }
            finally
            {
                _polling = false;
            }
        }

        private async Task WriteHeartbeatAsync()
        {
            var file = await _localFolder.CreateFileAsync("widget-alive.txt", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        }

        private void SetValues(JsonObject json)
        {
            CpuLoadText.Text = Percent(json, "cpuLoad");
            SetTemperature(CpuTemperatureText, Number(json, "cpuTemperature"), 80, 90);
            GpuLoadText.Text = Percent(json, "gpuLoad");
            SetTemperature(GpuTemperatureText, Number(json, "gpuTemperature"), 78, 88);
            SetTemperature(GpuHotspotText, Number(json, "gpuHotspot"), 95, 105);
            GpuPowerText.Text = Unit(json, "gpuPower", "W", 0);
            GpuFanText.Text = Unit(json, "gpuFan", "rpm", 0);
            MemoryText.Text = Pair(json, "usedRam", "totalRam", "GB");
            VramText.Text = Pair(json, "usedVram", "totalVram", "GB");
        }

        private void ApplySettings()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(ReadInt(_settings, "RefreshIntervalMs", 1000));
            Surface.Opacity = ReadDouble(_settings, "Opacity", 0.9);
            CpuLoadRow.Visibility = Visible(ReadBool(_settings, "ShowCpuLoad", true));
            CpuTemperatureRow.Visibility = Visible(ReadBool(_settings, "ShowCpuTemperature", true));
            GpuLoadRow.Visibility = Visible(ReadBool(_settings, "ShowGpuLoad", true));
            GpuTemperatureRow.Visibility = Visible(ReadBool(_settings, "ShowGpuTemperature", true));
            GpuHotspotRow.Visibility = Visible(ReadBool(_settings, "ShowGpuHotspot", true));
            GpuPowerRow.Visibility = Visible(ReadBool(_settings, "ShowGpuPower", true));
            GpuFanRow.Visibility = Visible(ReadBool(_settings, "ShowGpuFan", false));
            MemoryRow.Visibility = Visible(ReadBool(_settings, "ShowMemory", true));
            VramRow.Visibility = Visible(ReadBool(_settings, "ShowVram", true));
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var file = await _localFolder.GetFileAsync("settings.json");
                _settings = JsonObject.Parse(await FileIO.ReadTextAsync(file));
            }
            catch
            {
                _settings = new JsonObject();
            }
        }

        private static double? Number(JsonObject json, string key) => json.ContainsKey(key) && json[key].ValueType == JsonValueType.Number ? json[key].GetNumber() : (double?)null;
        private static string Percent(JsonObject json, string key) => Number(json, key) is double value ? $"{value:0}%" : "—";
        private static string Unit(JsonObject json, string key, string unit, int decimals) => Number(json, key) is double value ? $"{value.ToString($"F{decimals}")} {unit}" : "—";
        private static string Pair(JsonObject json, string used, string total, string unit) => Number(json, used) is double u && Number(json, total) is double t && t > 0 ? $"{u:0.0} / {t:0.0} {unit}" : "—";

        private static void SetTemperature(TextBlock text, double? value, double warning, double critical)
        {
            text.Text = value is double temperature ? $"{temperature:0} °C" : "—";
            var color = value >= critical ? Color.FromArgb(255, 240, 113, 103)
                : value >= warning ? Color.FromArgb(255, 242, 184, 75)
                : Color.FromArgb(255, 243, 240, 232);
            text.Foreground = new SolidColorBrush(color);
        }

        private static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
        private static bool ReadBool(JsonObject values, string key, bool fallback) => values.ContainsKey(key) && values[key].ValueType == JsonValueType.Boolean ? values[key].GetBoolean() : fallback;
        private static int ReadInt(JsonObject values, string key, int fallback) => values.ContainsKey(key) && values[key].ValueType == JsonValueType.Number ? (int)values[key].GetNumber() : fallback;
        private static double ReadDouble(JsonObject values, string key, double fallback) => values.ContainsKey(key) && values[key].ValueType == JsonValueType.Number ? values[key].GetNumber() : fallback;
    }
}
