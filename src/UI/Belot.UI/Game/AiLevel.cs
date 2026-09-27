namespace Belot.UI.Game
{
    using System;
    using System.ComponentModel;

    using Belot.Engine.Players;
    using Belot.UI.Localization;

    /// <summary>
    /// A computer level the person can pick for each of the other three seats: a display
    /// description plus the factory of the player (one per seat and game; the players decide
    /// from their seat's view, so they keep nothing between decisions). The <see cref="Elo"/> values
    /// are pair ratings from the simulator's round robin (<c>dotnet run ... -- elo</c>): two of a
    /// level against two of another, anchored so the Dummy sits at 1200.
    /// </summary>
    public sealed class AiLevel : INotifyPropertyChanged
    {
        private readonly string nameKey;

        private readonly string taglineKey;

        public AiLevel(string id, string avatar, string nameKey, string taglineKey, int difficulty, int elo, Func<IPlayer> factory)
        {
            this.Id = id;
            this.Avatar = avatar;
            this.nameKey = nameKey;
            this.taglineKey = taglineKey;
            this.Difficulty = difficulty;
            this.Elo = elo;
            this.Factory = factory;

            // Re-raise the localized properties when the language switches so bound labels update
            // in place. These instances, the manager and the stats store are static singletons, so
            // the subscriptions live for the app's lifetime: no leak.
            LocalizationManager.Instance.PropertyChanged += (_, _) => this.RaiseDisplayChanged();
            OpponentStatsStore.Changed += () => this.Raise(nameof(this.RecordText), nameof(this.HasRecord));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id { get; }

        /// <summary>Gets the emoji avatar shown in the level lists.</summary>
        public string Avatar { get; }

        public string DisplayName => LocalizationManager.Instance[this.nameKey];

        public string Tagline => LocalizationManager.Instance[this.taglineKey];

        /// <summary>Gets the difficulty, 1 to 5, for the stars and the difficulty label.</summary>
        public int Difficulty { get; }

        public int Elo { get; }

        public Func<IPlayer> Factory { get; }

        public string DifficultyStars =>
            new string('★', Math.Clamp(this.Difficulty, 0, 5)) + new string('☆', 5 - Math.Clamp(this.Difficulty, 0, 5));

        public string DifficultyLabel => LocalizationManager.Instance[$"Diff_{Math.Clamp(this.Difficulty, 1, 5)}"];

        public string EloText => $"ELO {this.Elo}";

        /// <summary>Gets the person's lifetime record in games with this level among the rivals ("3W – 1L").</summary>
        public string RecordText
        {
            get
            {
                var (games, wins) = OpponentStatsStore.For(this.Id);
                return games > 0
                    ? LocalizationManager.Instance.Format("Level_RecordFormat", wins, games - wins)
                    : string.Empty;
            }
        }

        public bool HasRecord => OpponentStatsStore.For(this.Id).Games > 0;

        public IPlayer CreatePlayer() => this.Factory();

        private void RaiseDisplayChanged() => this.Raise(
            nameof(this.DisplayName),
            nameof(this.Tagline),
            nameof(this.DifficultyLabel),
            nameof(this.RecordText));

        private void Raise(params string[] names)
        {
            foreach (var name in names)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
