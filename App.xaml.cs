using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace QuietMonitor
{
    sealed partial class App : Application
    {
        private XboxGameBarWidget _overlayWidget;
        private XboxGameBarWidget _settingsWidget;

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            XboxGameBarWidgetActivatedEventArgs widgetArgs = null;
            if (args.Kind == ActivationKind.Protocol && args is IProtocolActivatedEventArgs protocolArgs && protocolArgs.Uri.Scheme == "ms-gamebarwidget")
            {
                widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
            }

            if (widgetArgs == null || !widgetArgs.IsLaunchActivation) return;

            var frame = new Frame();
            Window.Current.Content = frame;
            var widget = new XboxGameBarWidget(widgetArgs, Window.Current.CoreWindow, frame);

            if (widgetArgs.AppExtensionId == "QuietMonitor.Settings")
            {
                _settingsWidget = widget;
                frame.Navigate(typeof(SettingsPage), widget);
                Window.Current.Closed += SettingsWindowClosed;
            }
            else
            {
                _overlayWidget = widget;
                frame.Navigate(typeof(OverlayPage), widget);
                Window.Current.Closed += OverlayWindowClosed;
            }

            Window.Current.Activate();
        }

        private void OverlayWindowClosed(object sender, Windows.UI.Core.CoreWindowEventArgs e)
        {
            _overlayWidget = null;
            Window.Current.Closed -= OverlayWindowClosed;
        }

        private void SettingsWindowClosed(object sender, Windows.UI.Core.CoreWindowEventArgs e)
        {
            _settingsWidget = null;
            Window.Current.Closed -= SettingsWindowClosed;
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            _overlayWidget = null;
            _settingsWidget = null;
        }
    }
}
