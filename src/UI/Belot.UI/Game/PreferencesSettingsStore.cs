namespace Belot.UI.Game
{
    using Microsoft.Maui.Storage;

    /// <summary>
    /// The app's settings store: MAUI <see cref="Preferences"/> (Windows registry / Android
    /// SharedPreferences / iOS NSUserDefaults).
    /// </summary>
    internal sealed class PreferencesSettingsStore : ISettingsStore
    {
        public T Get<T>(string key, T defaultValue) => Preferences.Default.Get(key, defaultValue);

        public void Set<T>(string key, T value) => Preferences.Default.Set(key, value);

        public void Remove(string key) => Preferences.Default.Remove(key);
    }
}
