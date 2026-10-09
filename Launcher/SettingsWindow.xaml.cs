using System.Windows;
using System.Windows.Media;

namespace QuietMonitor.Launcher;

public partial class SettingsWindow : Window
{
    private bool _loaded;

    public SettingsWindow()
    {
        InitializeComponent();
        PreferGameBarCheck.IsChecked = LauncherSettings.Load().UseGameBarWhenAvailable;
        var available = GameBarDetector.IsAvailable();
        GameBarStatus.Text = available ? "Installed and enabled" : "Unavailable or disabled — desktop fallback will be used";
        StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(available ? "#6FD4B0" : "#F1B760"));
        _loaded = true;
    }

    private void PreferenceChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        new LauncherSettings { UseGameBarWhenAvailable = PreferGameBarCheck.IsChecked == true }.Save();
        FeedbackText.Text = "Preference saved";
    }

    private void OpenMonitor_Click(object sender, RoutedEventArgs e)
    {
        new LauncherSettings { UseGameBarWhenAvailable = PreferGameBarCheck.IsChecked == true }.Save();
        var result = App.LaunchPreferredMode();
        FeedbackText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.Success ? "#6FD4B0" : "#FF806F"));
        FeedbackText.Text = result.Message;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
