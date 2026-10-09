namespace QuietMonitor.Models;

public sealed record SensorSnapshot(
    double CpuLoad,
    double? CpuTemperature,
    double GpuLoad,
    double? GpuTemperature,
    double? GpuHotspotTemperature,
    double UsedVramGb,
    double TotalVramGb,
    double? GpuPowerWatts,
    double? GpuFanRpm,
    double UsedRamGb,
    double TotalRamGb,
    double UsedDiskGb,
    double TotalDiskGb,
    DateTime CapturedAt);
