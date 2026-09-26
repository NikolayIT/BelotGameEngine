namespace Belot.UI.Game
{
    /// <summary>
    /// Where the app keeps its settings, the player's rating and the game history: MAUI
    /// Preferences in the app (set in <c>MauiProgram</c>), memory in the UI tests.
    /// </summary>
    public interface ISettingsStore
    {
        T Get<T>(string key, T defaultValue);

        void Set<T>(string key, T value);

        void Remove(string key);
    }
}
