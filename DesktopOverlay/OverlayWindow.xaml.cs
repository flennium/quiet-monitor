using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using QuietMonitor.Models;
using QuietMonitor.Services;

namespace QuietMonitor;

public partial class OverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    private readonly HardwareMonitorService _hardware = new();
    private readonly SettingsService _settingsService = new();
    private readonly DispatcherTimer _timer = new();
    private AppSettings _settings = new();
    private bool _isReading;

    public OverlayWindow()
    {
        InitializeComponent();
        _timer.Tick += async (_, _) => await UpdateReadingsAsync();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySettings(_settingsService.Load());
        await UpdateReadingsAsync();
        _timer.Start();
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _timer.Interval = TimeSpan.FromMilliseconds(settings.RefreshIntervalMs);
        OverlaySurface.Opacity = settings.Opacity;
        LayoutTransform = new ScaleTransform(settings.Scale, settings.Scale);
        CpuLoadRow.Visibility = Visible(settings.ShowCpuLoad);
        CpuTemperatureRow.Visibility = Visible(settings.ShowCpuTemperature);
        GpuLoadRow.Visibility = Visible(settings.ShowGpuLoad);
        GpuTemperatureRow.Visibility = Visible(settings.ShowGpuTemperature);
        GpuHotspotRow.Visibility = Visible(settings.ShowGpuHotspot);
        GpuPowerRow.Visibility = Visible(settings.ShowGpuPower);
        GpuFanRow.Visibility = Visible(settings.ShowGpuFan);
        MemoryRow.Visibility = Visible(settings.ShowMemory);
        VramRow.Visibility = Visible(settings.ShowVram);
        Dispatcher.BeginInvoke(PositionOverlay, DispatcherPriority.Loaded);
    }

    private async Task UpdateReadingsAsync()
    {
        if (_isReading) return;
        _isReading = true;
        try
        {
            var snapshot = await Task.Run(_hardware.Read);
            CpuLoadText.Text = $"{snapshot.CpuLoad:0}%";
            CpuTemperatureText.Text = Temperature(snapshot.CpuTemperature);
            GpuLoadText.Text = $"{snapshot.GpuLoad:0}%";
            GpuTemperatureText.Text = Temperature(snapshot.GpuTemperature);
            GpuHotspotText.Text = Temperature(snapshot.GpuHotspotTemperature);
            GpuPowerText.Text = snapshot.GpuPowerWatts is { } power ? $"{power:0} W" : "—";
            GpuFanText.Text = snapshot.GpuFanRpm is { } fan ? $"{fan:0} rpm" : "—";
            MemoryText.Text = $"{snapshot.UsedRamGb:0.0} / {snapshot.TotalRamGb:0.0} GB";
            VramText.Text = snapshot.TotalVramGb > 0 ? $"{snapshot.UsedVramGb:0.0} / {snapshot.TotalVramGb:0.0} GB" : "—";
            ApplyTemperatureColor(CpuTemperatureText, snapshot.CpuTemperature, 80, 90);
            ApplyTemperatureColor(GpuTemperatureText, snapshot.GpuTemperature, 78, 88);
            ApplyTemperatureColor(GpuHotspotText, snapshot.GpuHotspotTemperature, 95, 105);
            LiveDot.Fill = new SolidColorBrush(Color.FromRgb(111, 212, 176));
        }
        catch
        {
            LiveDot.Fill = new SolidColorBrush(Color.FromRgb(240, 113, 103));
        }
        finally
        {
            _isReading = false;
        }
    }

    private void PositionOverlay()
    {
        var area = SystemParameters.WorkArea;
        var margin = 18d;
        Left = _settings.Position.Contains("Right", StringComparison.Ordinal) ? area.Right - ActualWidth - margin : area.Left + margin;
        Top = _settings.Position.Contains("Bottom", StringComparison.Ordinal) ? area.Bottom - ActualHeight - margin : area.Top + margin;
    }

    private static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private static string Temperature(double? value) => value is { } temperature ? $"{temperature:0} °C" : "—";

    private static void ApplyTemperatureColor(System.Windows.Controls.TextBlock text, double? value, double warning, double critical)
    {
        var color = value >= critical ? Color.FromRgb(240, 113, 103)
            : value >= warning ? Color.FromRgb(242, 184, 75)
            : Color.FromRgb(244, 241, 233);
        text.Foreground = new SolidColorBrush(color);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExTransparent | WsExToolWindow | WsExNoActivate));
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _hardware.Dispose();
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr newStyle);
}
