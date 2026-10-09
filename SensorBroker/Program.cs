using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace QuietMonitor.SensorBroker
{
    internal static class Program
    {
        private const string MutexName = "Local\\QuietMonitor.SensorBroker";

        [STAThread]
        private static void Main()
        {
            var packageFamilyName = GetPackageFamilyName();
            if (packageFamilyName == null) return;
            var localStatePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages", packageFamilyName, "LocalState");
            Directory.CreateDirectory(localStatePath);

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

        private static string GetPackageFamilyName()
        {
            uint length = 0;
            var result = GetCurrentPackageFamilyName(ref length, null);
            if (result != 122 || length == 0) return null;

            var value = new StringBuilder((int)length);
            return GetCurrentPackageFamilyName(ref length, value) == 0 ? value.ToString() : null;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetCurrentPackageFamilyName(ref uint packageFamilyNameLength, StringBuilder packageFamilyName);

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
