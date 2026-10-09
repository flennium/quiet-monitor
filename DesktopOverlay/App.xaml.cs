using System.Threading;
using System.Windows;
using QuietMonitor.Services;

namespace QuietMonitor;

public partial class App : Application
{
    private const string InstanceMutexName = "Local\\QuietMonitor.Instance";
    private const string StopEventName = "Local\\QuietMonitor.Stop";
    private const string SettingsEventName = "Local\\QuietMonitor.Settings";

    private Mutex? _instanceMutex;
    private EventWaitHandle? _stopEvent;
    private EventWaitHandle? _settingsEvent;
    private RegisteredWaitHandle? _stopRegistration;
    private RegisteredWaitHandle? _settingsRegistration;
    private OverlayWindow? _overlay;
    private SettingsWindow? _settings;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var wantsSettings = e.Args.Any(arg => arg.Equals("--settings", StringComparison.OrdinalIgnoreCase));

        _instanceMutex = new Mutex(true, InstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            SignalExistingInstance(wantsSettings ? SettingsEventName : StopEventName);
            Shutdown();
            return;
        }
        _ownsMutex = true;

        _stopEvent = new EventWaitHandle(false, EventResetMode.AutoReset, StopEventName);
        _settingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, SettingsEventName);
        _stopRegistration = ThreadPool.RegisterWaitForSingleObject(_stopEvent, (_, _) => Dispatcher.Invoke(Shutdown), null, Timeout.Infinite, false);
        _settingsRegistration = ThreadPool.RegisterWaitForSingleObject(_settingsEvent, (_, _) => Dispatcher.Invoke(ShowSettings), null, Timeout.Infinite, false);

        _overlay = new OverlayWindow();
        MainWindow = _overlay;
        _overlay.Show();
        if (wantsSettings) ShowSettings();
    }

    public void ShowSettings()
    {
        if (_settings is null)
        {
            _settings = new SettingsWindow();
            _settings.Closed += (_, _) => _settings = null;
        }

        if (!_settings.IsVisible) _settings.Show();
        if (_settings.WindowState == WindowState.Minimized) _settings.WindowState = WindowState.Normal;
        _settings.Activate();
    }

    public void ApplySettings(Models.AppSettings settings) => _overlay?.ApplySettings(settings);

    protected override void OnExit(ExitEventArgs e)
    {
        _stopRegistration?.Unregister(null);
        _settingsRegistration?.Unregister(null);
        _stopEvent?.Dispose();
        _settingsEvent?.Dispose();
        if (_ownsMutex) _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static void SignalExistingInstance(string eventName)
    {
        try
        {
            using var signal = EventWaitHandle.OpenExisting(eventName);
            signal.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The first instance is still starting. A second click can retry safely.
        }
    }
}
