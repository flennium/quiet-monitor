using System.IO;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using QuietMonitor.Models;

namespace QuietMonitor.Services;

public sealed class HardwareMonitorService : IDisposable
{
    private readonly object _sync = new();
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsStorageEnabled = false
    };
    private bool _isOpen;

    public void Open()
    {
        lock (_sync)
        {
            if (_isOpen) return;
            _computer.Open();
            _isOpen = true;
        }
    }

    public SensorSnapshot Read()
    {
        lock (_sync)
        {
            if (!_isOpen)
            {
                _computer.Open();
                _isOpen = true;
            }

            double cpuLoad = 0;
            double? cpuTemp = null;
            GpuReading? bestGpu = null;

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware) subHardware.Update();

            var sensors = hardware.Sensors.Concat(hardware.SubHardware.SelectMany(x => x.Sensors));
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                cpuLoad = Find(sensors, SensorType.Load, "CPU Total") ?? FindMax(sensors, SensorType.Load) ?? 0;
                cpuTemp = Find(sensors, SensorType.Temperature, "CPU Package") ?? FindMax(sensors, SensorType.Temperature);
            }
            else if (hardware.HardwareType is HardwareType.GpuAmd or HardwareType.GpuNvidia or HardwareType.GpuIntel)
            {
                var candidate = new GpuReading(
                    hardware.Name,
                    Find(sensors, SensorType.Load, "GPU Core") ?? FindMax(sensors, SensorType.Load) ?? 0,
                    Find(sensors, SensorType.Temperature, "GPU Core") ?? Find(sensors, SensorType.Temperature, "GPU Temperature"),
                    FindContaining(sensors, SensorType.Temperature, "hot"),
                    FindContaining(sensors, SensorType.Power, "package") ?? FindMax(sensors, SensorType.Power),
                    FindMax(sensors, SensorType.Fan),
                    (FindContaining(sensors, SensorType.SmallData, "used") ?? 0) / 1024d,
                    (FindContaining(sensors, SensorType.SmallData, "total") ?? 0) / 1024d);

                if (bestGpu is null || candidate.PreferenceScore > bestGpu.PreferenceScore) bestGpu = candidate;
            }
        }

        var memory = GetMemory();
            return new SensorSnapshot(cpuLoad, cpuTemp, bestGpu?.Load ?? 0, bestGpu?.Temperature,
                bestGpu?.Hotspot, bestGpu?.UsedVramGb ?? 0, bestGpu?.TotalVramGb ?? 0,
                bestGpu?.PowerWatts, bestGpu?.FanRpm, memory.UsedGb, memory.TotalGb,
                0, 0, DateTime.Now);
        }
    }

    private static double? Find(IEnumerable<ISensor> sensors, SensorType type, string name) =>
        sensors.FirstOrDefault(s => s.SensorType == type && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

    private static double? FindContaining(IEnumerable<ISensor> sensors, SensorType type, string fragment) =>
        sensors.FirstOrDefault(s => s.SensorType == type && s.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase))?.Value;

    private static double? FindMax(IEnumerable<ISensor> sensors, SensorType type) =>
        sensors.Where(s => s.SensorType == type && s.Value.HasValue).Select(s => (double?)s.Value!.Value).DefaultIfEmpty().Max();

    private static (double UsedGb, double TotalGb) GetMemory()
    {
        var status = new MemoryStatusEx();
        return GlobalMemoryStatusEx(status)
            ? ((status.TotalPhysical - status.AvailablePhysical) / 1073741824d, status.TotalPhysical / 1073741824d)
            : (0, 0);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (!_isOpen) return;
            _computer.Close();
            _isOpen = false;
        }
    }

    private sealed record GpuReading(string Name, double Load, double? Temperature, double? Hotspot,
        double? PowerWatts, double? FanRpm, double UsedVramGb, double TotalVramGb)
    {
        public double PreferenceScore => (Name.Contains("RX", StringComparison.OrdinalIgnoreCase) ? 1000 : 0) + TotalVramGb;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);
}
