namespace QuietMonitor.Models;

public sealed class AppSettings
{
    public int RefreshIntervalMs { get; set; } = 1000;
    public string Position { get; set; } = "TopRight";
    public double Scale { get; set; } = 1.0;
    public double Opacity { get; set; } = 0.9;
    public bool ShowCpuLoad { get; set; } = true;
    public bool ShowCpuTemperature { get; set; } = true;
    public bool ShowGpuLoad { get; set; } = true;
    public bool ShowGpuTemperature { get; set; } = true;
    public bool ShowGpuHotspot { get; set; } = true;
    public bool ShowGpuPower { get; set; } = true;
    public bool ShowGpuFan { get; set; }
    public bool ShowMemory { get; set; } = true;
    public bool ShowVram { get; set; } = true;
}
