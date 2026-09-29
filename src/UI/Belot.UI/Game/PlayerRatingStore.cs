namespace Belot.UI.Game
{
    using System;

    /// <summary>
    /// The person's ELO rating, persisted on the device (see <see cref="SettingsStore"/>). A new
    /// player starts at <see cref="DefaultElo"/>. The computer levels are fixed anchors (see
    /// <see cref="AiLevel.Elo"/>), so only the person's number moves. Belot is played in pairs: the
    /// person's side is rated as the average of the person and the partner, against the rivals'
    /// level, and the whole change goes to the person.
    /// </summary>
    public static class PlayerRatingStore
    {
        public const int DefaultElo = 1000;

        // Standard ELO step size. 32 gives noticeable but not wild swings over a handful of games.
        private const int KFactor = 32;

        private const string EloKey = "player.elo";
        private const string GamesKey = "player.games";
        private const string WinsKey = "player.wins";
        private const string PeakEloKey = "player.peakElo";
        private const string StreakKey = "player.streak";
        private const string BestStreakKey = "player.bestStreak";

        public static int CurrentElo => SettingsStore.Current.Get(EloKey, DefaultElo);

        public static int GamesPlayed => Math.Max(0, SettingsStore.Current.Get(GamesKey, 0));

        public static int Wins => Math.Clamp(SettingsStore.Current.Get(WinsKey, 0), 0, GamesPlayed);

        public static int Losses => Math.Max(0, GamesPlayed - Wins);

        /// <summary>Gets the highest rating ever reached (never below the current or starting rating).</summary>
        public static int PeakElo => Math.Max(DefaultElo, Math.Max(CurrentElo, SettingsStore.Current.Get(PeakEloKey, DefaultElo)));

        /// <summary>Gets the signed run of results: +n = n wins in a row, -n = n losses in a row.</summary>
        public static int CurrentStreak => Math.Clamp(SettingsStore.Current.Get(StreakKey, 0), -Losses, Wins);

        public static int BestWinStreak => Math.Clamp(Math.Max(CurrentStreak, SettingsStore.Current.Get(BestStreakKey, 0)), 0, Wins);

        /// <summary>The chance that the person's side wins: (person + partner) / 2 against the rivals.</summary>
        /// <param name="personElo">The person's rating.</param>
        /// <param name="partnerElo">The partner's level rating.</param>
        /// <param name="rivalsElo">The rivals' rating as a pair (the average of their levels).</param>
        /// <returns>The expected score, between 0 and 1.</returns>
        public static double ExpectedScore(int personElo, int partnerElo, int rivalsElo) =>
            1.0 / (1.0 + Math.Pow(10.0, (rivalsElo - ((personElo + partnerElo) / 2.0)) / 400.0));

        public static RatingChange RecordResult(int partnerElo, int rivalsElo, bool won)
        {
            var oldElo = CurrentElo;
            var peak = PeakElo;
            var games = GamesPlayed;
            var wins = Wins;
            var streak = CurrentStreak;
            var bestStreak = BestWinStreak;
            var expected = ExpectedScore(oldElo, partnerElo, rivalsElo);
            var newElo = (int)Math.Round(oldElo + (KFactor * ((won ? 1.0 : 0.0) - expected)));

            SettingsStore.Current.Set(EloKey, newElo);
            SettingsStore.Current.Set(GamesKey, games + 1);
            SettingsStore.Current.Set(WinsKey, wins + (won ? 1 : 0));
            SettingsStore.Current.Set(PeakEloKey, Math.Max(peak, newElo));

            streak = won ? (streak > 0 ? streak + 1 : 1) : (streak < 0 ? streak - 1 : -1);
            SettingsStore.Current.Set(StreakKey, streak);
            SettingsStore.Current.Set(BestStreakKey, Math.Max(bestStreak, streak));

            return new RatingChange(oldElo, newElo, won);
        }

        public static void Reset()
        {
            SettingsStore.Current.Remove(EloKey);
            SettingsStore.Current.Remove(GamesKey);
            SettingsStore.Current.Remove(WinsKey);
            SettingsStore.Current.Remove(PeakEloKey);
            SettingsStore.Current.Remove(StreakKey);
            SettingsStore.Current.Remove(BestStreakKey);
        }
    }
}
