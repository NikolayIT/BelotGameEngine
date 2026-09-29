namespace Belot.UI
{
    using Android.App;
    using Android.Content.PM;
    using Android.Content.Res;

    using Belot.UI.Game;

    using Microsoft.Maui;

    // Portrait only: four seats around a table need the height.
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ScreenOrientation = ScreenOrientation.SensorPortrait, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.FontScale)]
    public class MainActivity : MauiAppCompatActivity
    {
        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            UiScale.Current.UpdateSystemFontScale(newConfig.FontScale);

            // Keep the existing Shell, table and match when accessibility text size changes.
            // Board text bindings use the bounded factor above. Other text keeps native SP
            // scaling; refresh its handlers and formatted spans using the updated resources.
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
