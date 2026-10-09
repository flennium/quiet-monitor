using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

namespace QuietMonitor.SensorBroker
{
    internal sealed class SensorReader : IDisposable
    {
        private readonly Computer _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true
        };

        public SensorReader() => _computer.Open();

        public TelemetrySnapshot Read()
        {
            var snapshot = new TelemetrySnapshot();
            GpuReading? bestGpu = null;

            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                foreach (var subHardware in hardware.SubHardware) subHardware.Update();
                var sensors = hardware.Sensors.Concat(hardware.SubHardware.SelectMany(item => item.Sensors)).ToArray();

                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    snapshot.CpuLoad = Find(sensors, SensorType.Load, "CPU Total") ?? FindMax(sensors, SensorType.Load) ?? 0;
                    snapshot.CpuTemperature = Find(sensors, SensorType.Temperature, "CPU Package") ?? FindMax(sensors, SensorType.Temperature);
                }
                else if (hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuIntel)
                {
                    var candidate = new GpuReading
                    {
                        Name = hardware.Name,
                        Load = Find(sensors, SensorType.Load, "GPU Core") ?? FindMax(sensors, SensorType.Load) ?? 0,
                        Temperature = Find(sensors, SensorType.Temperature, "GPU Core") ?? Find(sensors, SensorType.Temperature, "GPU Temperature"),
                        Hotspot = FindContaining(sensors, SensorType.Temperature, "hot"),
                        Power = FindContaining(sensors, SensorType.Power, "package") ?? FindMax(sensors, SensorType.Power),
                        Fan = FindMax(sensors, SensorType.Fan),
                        UsedVram = (FindContaining(sensors, SensorType.SmallData, "used") ?? 0) / 1024d,
                        TotalVram = (FindContaining(sensors, SensorType.SmallData, "total") ?? 0) / 1024d
                    };
                    if (bestGpu == null || candidate.Score > bestGpu.Score) bestGpu = candidate;
                }
            }

            var memory = GetMemory();
            snapshot.UsedRam = memory.Item1;
            snapshot.TotalRam = memory.Item2;
            if (bestGpu != null)
            {
                snapshot.GpuLoad = bestGpu.Load;
                snapshot.GpuTemperature = bestGpu.Temperature;
                snapshot.GpuHotspot = bestGpu.Hotspot;
                snapshot.GpuPower = bestGpu.Power;
                snapshot.GpuFan = bestGpu.Fan;
                snapshot.UsedVram = bestGpu.UsedVram;
                snapshot.TotalVram = bestGpu.TotalVram;
            }
            return snapshot;
        }

        private static double? Find(IEnumerable<ISensor> sensors, SensorType type, string name) => sensors.FirstOrDefault(sensor => sensor.SensorType == type && sensor.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;
        private static double? FindContaining(IEnumerable<ISensor> sensors, SensorType type, string fragment) => sensors.FirstOrDefault(sensor => sensor.SensorType == type && sensor.Name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)?.Value;
        private static double? FindMax(IEnumerable<ISensor> sensors, SensorType type) => sensors.Where(sensor => sensor.SensorType == type && sensor.Value.HasValue).Select(sensor => (double?)sensor.Value.GetValueOrDefault()).DefaultIfEmpty().Max();

        private static Tuple<double, double> GetMemory()
        {
            var status = new MemoryStatusEx();
            return GlobalMemoryStatusEx(status)
                ? Tuple.Create((status.TotalPhysical - status.AvailablePhysical) / 1073741824d, status.TotalPhysical / 1073741824d)
                : Tuple.Create(0d, 0d);
        }

        public void Dispose() => _computer.Close();

        private sealed class GpuReading
        {
            public string Name { get; set; } = string.Empty;
            public double Load { get; set; }
            public double? Temperature { get; set; }
            public double? Hotspot { get; set; }
            public double? Power { get; set; }
            public double? Fan { get; set; }
            public double UsedVram { get; set; }
            public double TotalVram { get; set; }
            public double Score => (Name.IndexOf("RX", StringComparison.OrdinalIgnoreCase) >= 0 ? 1000 : 0) + TotalVram;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private sealed class MemoryStatusEx
        {
            public uint Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
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
}
