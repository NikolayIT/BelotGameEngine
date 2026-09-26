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
            // The table is laid out portrait (like the phone) and scales to the window: open a tall
            // window that fits the screen, about the proportions of a phone held upright.
            var display = DeviceDisplay.Current.MainDisplayInfo;
            var screenHeight = display.Density > 0 ? display.Height / display.Density : 1080;
            window.Height = Math.Clamp(screenHeight - 120, 600, 980);
            window.Width = Math.Round(window.Height * 0.66);
            window.MinimumWidth = 360;
            window.MinimumHeight = 560;
#endif
            return window;
        }
    }
}
