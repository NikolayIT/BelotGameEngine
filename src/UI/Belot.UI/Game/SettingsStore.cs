namespace Belot.UI.Game
{
    using System;

    /// <summary>The app's <see cref="ISettingsStore"/>. No MAUI types here: the UI tests compile this file.</summary>
    public static class SettingsStore
    {
        private static ISettingsStore? current;

        public static ISettingsStore Current
        {
            get => current ?? throw new InvalidOperationException("SettingsStore.Current is set when the app starts (MauiProgram).");
            set => current = value;
        }
    }
}
