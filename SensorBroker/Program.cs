using System;
using System.IO;
using System.Text;
using System.Threading;

namespace QuietMonitor.SensorBroker
{
    internal static class Program
    {
        private const string MutexName = "Local\\QuietMonitor.SensorBroker";

        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 0) return;

            string localStatePath;
            try
            {
                localStatePath = Encoding.UTF8.GetString(Convert.FromBase64String(args[0]));
            }
            catch
            {
                return;
            }

            if (!Directory.Exists(localStatePath)) return;

            using (var mutex = new Mutex(true, MutexName, out var isFirstInstance))
            {
                if (!isFirstInstance) return;

                var heartbeatPath = Path.Combine(localStatePath, "widget-alive.txt");
                var telemetryPath = Path.Combine(localStatePath, "telemetry.json");
                using (var reader = new SensorReader())
                {
                    while (HeartbeatIsCurrent(heartbeatPath))
                    {
                        try
                        {
                            WriteTelemetry(telemetryPath, reader.Read().ToJson());
                        }
                        catch
                        {
                            // A missing sensor should not keep the broker alive after the widget closes.
                        }
                        Thread.Sleep(750);
                    }
                }
            }
        }

        private static bool HeartbeatIsCurrent(string path)
        {
            if (!File.Exists(path)) return false;
            return DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < TimeSpan.FromSeconds(6);
        }

        private static void WriteTelemetry(string destination, string json)
        {
            var temporary = destination + ".tmp";
            File.WriteAllText(temporary, json, Encoding.UTF8);
            File.Copy(temporary, destination, true);
            File.Delete(temporary);
        }
    }
}
