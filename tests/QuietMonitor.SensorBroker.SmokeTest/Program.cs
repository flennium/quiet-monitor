using System;
using QuietMonitor.SensorBroker;

internal static class Program
{
    private static int Main()
    {
        try
        {
            using (var reader = new SensorReader())
            {
                var snapshot = reader.Read();
                Assert(snapshot.CpuLoad >= 0 && snapshot.CpuLoad <= 100, "CPU load is in range");
                if (snapshot.CpuTemperature == null) Console.WriteLine("INFO: CPU temperature is unavailable on this non-elevated hardware path and will render as —, never 0 °C");
                Assert(snapshot.CpuTemperature == null || snapshot.CpuTemperature > 0 && snapshot.CpuTemperature < 150, "CPU temperature is unavailable or plausible, never a false zero");
                Assert(snapshot.GpuLoad >= 0 && snapshot.GpuLoad <= 100, "GPU load is in range");
                Assert(snapshot.TotalRam > 1 && snapshot.UsedRam > 0 && snapshot.UsedRam <= snapshot.TotalRam, "RAM reading is plausible");
                Assert(snapshot.GpuTemperature == null || snapshot.GpuTemperature > 0 && snapshot.GpuTemperature < 150, "GPU temperature is plausible");
                Assert(snapshot.TotalVram >= 0 && snapshot.UsedVram >= 0 && snapshot.UsedVram <= snapshot.TotalVram, "VRAM reading is plausible");
                var json = snapshot.ToJson();
                foreach (var key in new[] { "cpuLoad", "cpuTemperature", "gpuLoad", "gpuTemperature", "usedRam", "totalRam", "usedVram", "totalVram" })
                    Assert(json.Contains($"\"{key}\":"), $"telemetry JSON contains {key}");
                var cpuTemperature = snapshot.CpuTemperature.HasValue ? $"{snapshot.CpuTemperature.Value:0.#}C" : "unavailable";
                Console.WriteLine($"PASS: live hardware sample CPU={snapshot.CpuLoad:0}% CPU temp={cpuTemperature} GPU={snapshot.GpuLoad:0}% GPU temp={snapshot.GpuTemperature:0.#}C RAM={snapshot.UsedRam:0.0}/{snapshot.TotalRam:0.0}GB VRAM={snapshot.UsedVram:0.0}/{snapshot.TotalVram:0.0}GB");
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception}");
            return 1;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine($"PASS: {message}");
    }
}
