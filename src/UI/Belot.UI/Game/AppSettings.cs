namespace Belot.UI.Game
{
    public enum GameSpeed
    {
        Relaxed = 0,
        Normal = 1,
        Fast = 2,
    }

    /// <summary>
    /// Device-persisted app options (see <see cref="SettingsStore"/>). All values have sensible
    /// defaults so a fresh install needs no setup screen. The speed presets translate into the
    /// game's <see cref="GamePace"/>: the computer's thinking pause, how long a finished trick
    /// stays on the table, and the pause before a card or pass the rules made for somebody.
    /// </summary>
    public static class AppSettings
    {
        private const string SpeedKey = "settings.speed";
        private const string HapticsKey = "settings.haptics";
        private const string AssistsKey = "settings.assists";
        private const string PlayerNameKey = "settings.playerName";
        private const string PartnerKey = "settings.partner";
        private const string WestKey = "settings.west";
        private const string EastKey = "settings.east";

        public static GameSpeed Speed
        {
            get => NormalizeSpeed((GameSpeed)SettingsStore.Current.Get(SpeedKey, (int)GameSpeed.Normal));
            set => SettingsStore.Current.Set(SpeedKey, (int)NormalizeSpeed(value));
        }

        public static bool HapticsEnabled
        {
            get => SettingsStore.Current.Get(HapticsKey, true);
            set => SettingsStore.Current.Set(HapticsKey, value);
        }

        /// <summary>Gets or sets a value indicating whether the beginner assists are on: belote badges on own cards and the hint button.</summary>
        public static bool AssistsEnabled
        {
            get => SettingsStore.Current.Get(AssistsKey, true);
            set => SettingsStore.Current.Set(AssistsKey, value);
        }

        public static string PlayerName
        {
            get => SettingsStore.Current.Get(PlayerNameKey, string.Empty);
            set => SettingsStore.Current.Set(PlayerNameKey, value ?? string.Empty);
        }

        /// <summary>Gets or sets the id of the partner's level last chosen (see <see cref="AiLevels"/>).</summary>
        public static string PartnerLevel
        {
            get => SettingsStore.Current.Get(PartnerKey, "smart");
            set => SettingsStore.Current.Set(PartnerKey, value ?? string.Empty);
        }

        /// <summary>Gets or sets the id of the left rival's (West's) level last chosen.</summary>
        public static string WestLevel
        {
            get => SettingsStore.Current.Get(WestKey, "smart");
            set => SettingsStore.Current.Set(WestKey, value ?? string.Empty);
        }

        /// <summary>Gets or sets the id of the right rival's (East's) level last chosen.</summary>
        public static string EastLevel
        {
            get => SettingsStore.Current.Get(EastKey, "smart");
            set => SettingsStore.Current.Set(EastKey, value ?? string.Empty);
        }

        public static GamePace Pace => Speed switch
        {
            GameSpeed.Relaxed => new GamePace(900, 1600, 450),
            GameSpeed.Fast => new GamePace(200, 600, 150),
            _ => new GamePace(500, 1100, 300),
        };

        private static GameSpeed NormalizeSpeed(GameSpeed speed) =>
            speed is GameSpeed.Relaxed or GameSpeed.Normal or GameSpeed.Fast ? speed : GameSpeed.Normal;
    }
}
