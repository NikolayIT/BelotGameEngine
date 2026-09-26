namespace Belot.UI.Tests
{
    using System.Collections.Generic;

    using Belot.UI.Game;

    public sealed class MemorySettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, object?> values = new();

        public T Get<T>(string key, T defaultValue) => this.values.TryGetValue(key, out var value) ? (T)value! : defaultValue;

        public void Set<T>(string key, T value) => this.values[key] = value;

        public void Remove(string key) => this.values.Remove(key);
    }
}
