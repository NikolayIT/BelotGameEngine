namespace Belot.UI.Game
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Lifetime win/loss tallies per AI opponent, persisted on the device (one pair of counters
    /// per opponent id; see <see cref="SettingsStore"/>). Unlike <see cref="MatchHistoryStore"/>, which is
    /// capped, these run forever — they feed the "Your record: 3W – 1L" line on the start page
    /// and the per-opponent table on the statistics page.
    /// </summary>
    public static class OpponentStatsStore
    {
        private const string GamesKeyPrefix = "opp.games.";
        private const string WinsKeyPrefix = "opp.wins.";

        /// <summary>Raised after a result is recorded or the store is cleared, so views can refresh.</summary>
        public static event Action? Changed;

        public static (int Games, int Wins) For(string opponentId)
        {
            var games = Math.Max(0, SettingsStore.Current.Get(GamesKeyPrefix + opponentId, 0));
            var wins = SettingsStore.Current.Get(WinsKeyPrefix + opponentId, 0);
            return (games, Math.Clamp(wins, 0, games));
        }

        public static void Record(string opponentId, bool won)
        {
            if (string.IsNullOrEmpty(opponentId))
            {
                return;
            }

            var (games, wins) = For(opponentId);
            SettingsStore.Current.Set(GamesKeyPrefix + opponentId, games + 1);
            SettingsStore.Current.Set(WinsKeyPrefix + opponentId, wins + (won ? 1 : 0));

            Changed?.Invoke();
        }

        public static void Clear(IEnumerable<string> opponentIds)
        {
            foreach (var id in opponentIds)
            {
                SettingsStore.Current.Remove(GamesKeyPrefix + id);
                SettingsStore.Current.Remove(WinsKeyPrefix + id);
            }

            Changed?.Invoke();
        }
    }
}
