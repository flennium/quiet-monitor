using System.Globalization;

namespace QuietMonitor.SensorBroker
{
    internal sealed class TelemetrySnapshot
    {
        public double CpuLoad { get; set; }
        public double? CpuTemperature { get; set; }
        public double GpuLoad { get; set; }
        public double? GpuTemperature { get; set; }
        public double? GpuHotspot { get; set; }
        public double? GpuPower { get; set; }
        public double? GpuFan { get; set; }
        public double UsedRam { get; set; }
        public double TotalRam { get; set; }
        public double UsedVram { get; set; }
        public double TotalVram { get; set; }

        public string ToJson() => "{" +
            Pair("cpuLoad", CpuLoad) + "," +
            Pair("cpuTemperature", CpuTemperature) + "," +
            Pair("gpuLoad", GpuLoad) + "," +
            Pair("gpuTemperature", GpuTemperature) + "," +
            Pair("gpuHotspot", GpuHotspot) + "," +
            Pair("gpuPower", GpuPower) + "," +
            Pair("gpuFan", GpuFan) + "," +
            Pair("usedRam", UsedRam) + "," +
            Pair("totalRam", TotalRam) + "," +
            Pair("usedVram", UsedVram) + "," +
            Pair("totalVram", TotalVram) + "}";

        private static string Pair(string name, double? value) => $"\"{name}\":{(value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "null")}";
    }
}
