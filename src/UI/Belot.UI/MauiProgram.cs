namespace Belot.UI
{
    using Microsoft.Extensions.Logging;

    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Before anything reads a setting (the language is resolved on first use).
            Game.SettingsStore.Current = new Game.PreferencesSettingsStore();

#if ANDROID
            Game.UiScale.Current.UpdateSystemFontScale(global::Android.App.Application.Context.Resources?.Configuration?.FontScale ?? 1);
#endif

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
