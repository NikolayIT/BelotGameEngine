namespace Belot.UI
{
    public partial class App : Application
    {
        public App()
        {
            // Resolve the device/saved language and set the thread culture before any page builds.
            _ = Localization.LocalizationManager.Instance;

            this.InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
#if WINDOWS
            // A table for four needs room; a phone-shaped window would waste the desktop.
            window.Width = 1100;
            window.Height = 820;
            window.MinimumWidth = 720;
            window.MinimumHeight = 640;
#endif
            return window;
        }
    }
}
