namespace Belot.UI.Tests
{
    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    // Tests that use the app's static state (the settings store, the language, the rating and
    // history stores) run one at a time, each on a fresh in-memory store in English.
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class AppState
    {
        public const string Name = "App state";

        public static MemorySettingsStore Reset(string language = LocalizationManager.English)
        {
            var store = new MemorySettingsStore();
            SettingsStore.Current = store;
            LocalizationManager.Instance.SetLanguage(language);
            return store;
        }
    }
}
