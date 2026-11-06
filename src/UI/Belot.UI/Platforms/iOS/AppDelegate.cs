namespace Belot.UI
{
    using Belot.UI.Game;

    using Foundation;

    using Microsoft.Maui;
    using Microsoft.Maui.Hosting;

    using UIKit;

    [Register(nameof(AppDelegate))]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        private NSObject? fontScaleObserver;

        public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
        {
            var launched = base.FinishedLaunching(application, launchOptions);

            // The same as MainActivity.OnConfigurationChanged: a change of the system's text size
            // keeps the Shell, the table and the match, and only the text follows it.
            this.fontScaleObserver = UIApplication.Notifications.ObserveContentSizeCategoryChanged((_, _) => OnFontScaleChanged());
#if STORE_SHOTS
            StoreShotsDriver.Start();
#endif
            return launched;
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.fontScaleObserver?.Dispose();
            }

            base.Dispose(disposing);
        }

        private static void OnFontScaleChanged()
        {
            UiScale.Current.UpdateSystemFontScale(UIFontMetrics.DefaultMetrics.GetScaledValue(1));
            if (Microsoft.Maui.Controls.Application.Current is not { } application)
            {
                return;
            }

            foreach (var window in application.Windows)
            {
                RefreshFonts(window);
            }
        }

        private static void RefreshFonts(IVisualTreeElement element)
        {
            if (element is ITextStyle && element is IElement text)
            {
                text.Handler?.UpdateValue(nameof(ITextStyle.Font));
            }

            foreach (var child in element.GetVisualChildren())
            {
                RefreshFonts(child);
            }

            if (element is IView view)
            {
                view.InvalidateMeasure();
            }
        }
    }
}
