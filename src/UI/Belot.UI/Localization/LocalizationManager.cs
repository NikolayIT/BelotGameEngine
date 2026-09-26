namespace Belot.UI.Localization
{
    using System.ComponentModel;
    using System.Globalization;

    using Belot.UI.Game;

    /// <summary>
    /// App-wide language state for the two supported languages (English + Bulgarian). Defaults to
    /// the device locale (Bulgarian on a Bulgarian device, English otherwise) and can be overridden
    /// from the start page; the choice is persisted on the device. Exposes a string indexer so XAML
    /// can bind localized text via the <see cref="TrExtension"/> markup extension and refresh live
    /// when the language changes (the indexer's PropertyChanged is raised on every switch).
    /// </summary>
    public sealed class LocalizationManager : INotifyPropertyChanged
    {
        public const string English = "en";

        public const string Bulgarian = "bg";

        private const string LanguageKey = "app.language";

        private const string IndexerName = "Item[]";

        private static readonly LocalizationManager InstanceField = new();

        private string language;

        private LocalizationManager()
        {
            this.language = ResolveInitialLanguage();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static LocalizationManager Instance => InstanceField;

        public string Language => this.language;

        public bool IsBulgarian => this.language == Bulgarian;

        public string this[string key] => AppStrings.Get(this.language, key);

        public void SetLanguage(string value)
        {
            var normalized = value == Bulgarian ? Bulgarian : English;
            if (this.language == normalized)
            {
                return;
            }

            this.language = normalized;
            SettingsStore.Current.Set(LanguageKey, normalized);
            ApplyThreadCulture(normalized);

            // Empty name = "all properties changed" (refresh every bound value); the indexer name
            // is raised too for binding stacks that track it specifically.
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Language)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsBulgarian)));
        }

        public void Toggle() => this.SetLanguage(this.IsBulgarian ? English : Bulgarian);

        public string Format(string key, params object[] args) =>
            string.Format(CultureInfo.CurrentCulture, this[key], args);

        private static string ResolveInitialLanguage()
        {
            var saved = SettingsStore.Current.Get(LanguageKey, string.Empty);
            var resolved = saved == English || saved == Bulgarian
                ? saved
                : (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == Bulgarian ? Bulgarian : English);

            ApplyThreadCulture(resolved);
            return resolved;
        }

        private static void ApplyThreadCulture(string lang)
        {
            var culture = new CultureInfo(lang == Bulgarian ? "bg-BG" : "en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
    }
}
