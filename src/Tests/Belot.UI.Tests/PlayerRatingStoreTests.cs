namespace Belot.UI.Tests
{
    using Belot.UI.Game;

    using Xunit;

    [Collection(AppState.Name)]
    public class PlayerRatingStoreTests
    {
        public PlayerRatingStoreTests() => AppState.Reset();

        [Fact]
        public void InconsistentSavedCountersShouldKeepStatisticsWithinTheirBounds()
        {
            SettingsStore.Current.Set("player.games", 10);
            SettingsStore.Current.Set("player.wins", 15);
            SettingsStore.Current.Set("player.streak", -100);
            SettingsStore.Current.Set("player.bestStreak", 20);

            Assert.Equal(10, PlayerRatingStore.GamesPlayed);
            Assert.Equal(10, PlayerRatingStore.Wins);
            Assert.Equal(0, PlayerRatingStore.Losses);
            Assert.Equal(0, PlayerRatingStore.CurrentStreak);
            Assert.Equal(10, PlayerRatingStore.BestWinStreak);
        }

        [Fact]
        public void NegativeSavedCountersShouldRecoverOnTheNextResult()
        {
            SettingsStore.Current.Set("player.games", -5);
            SettingsStore.Current.Set("player.wins", -2);
            SettingsStore.Current.Set("player.streak", -5);
            SettingsStore.Current.Set("player.bestStreak", -3);

            Assert.Equal(0, PlayerRatingStore.GamesPlayed);
            Assert.Equal(0, PlayerRatingStore.Wins);
            Assert.Equal(0, PlayerRatingStore.CurrentStreak);
            Assert.Equal(0, PlayerRatingStore.BestWinStreak);

            PlayerRatingStore.RecordResult(1200, 1200, true);

            Assert.Equal(1, PlayerRatingStore.GamesPlayed);
            Assert.Equal(1, PlayerRatingStore.Wins);
            Assert.Equal(1, PlayerRatingStore.CurrentStreak);
            Assert.Equal(1, PlayerRatingStore.BestWinStreak);
        }

        [Fact]
        public void AFirstLossWithAnOlderSavedRatingShouldKeepThePreviousPeak()
        {
            SettingsStore.Current.Set("player.elo", 1500);

            PlayerRatingStore.RecordResult(1200, 1200, false);

            Assert.True(PlayerRatingStore.CurrentElo < 1500);
            Assert.Equal(1500, PlayerRatingStore.PeakElo);
        }

        [Fact]
        public void ThePeakShouldNeverFallBelowTheStartingRating()
        {
            SettingsStore.Current.Set("player.elo", 800);
            SettingsStore.Current.Set("player.peakElo", 900);

            Assert.Equal(PlayerRatingStore.DefaultElo, PlayerRatingStore.PeakElo);
        }

        [Fact]
        public void NormalResultsShouldTrackStreaksAndResetEveryStatistic()
        {
            foreach (var won in new[] { true, true, false, false, false, true })
            {
                PlayerRatingStore.RecordResult(1200, 1200, won);
            }

            Assert.Equal(6, PlayerRatingStore.GamesPlayed);
            Assert.Equal(3, PlayerRatingStore.Wins);
            Assert.Equal(3, PlayerRatingStore.Losses);
            Assert.Equal(1, PlayerRatingStore.CurrentStreak);
            Assert.Equal(2, PlayerRatingStore.BestWinStreak);

            PlayerRatingStore.Reset();

            Assert.Equal(PlayerRatingStore.DefaultElo, PlayerRatingStore.CurrentElo);
            Assert.Equal(PlayerRatingStore.DefaultElo, PlayerRatingStore.PeakElo);
            Assert.Equal(0, PlayerRatingStore.GamesPlayed);
            Assert.Equal(0, PlayerRatingStore.Wins);
            Assert.Equal(0, PlayerRatingStore.CurrentStreak);
            Assert.Equal(0, PlayerRatingStore.BestWinStreak);
        }

        [Theory]
        [InlineData(-5, -2, 0, 0)]
        [InlineData(5, -2, 5, 0)]
        [InlineData(5, 8, 5, 5)]
        public void OpponentCountersShouldRecoverBeforeRecording(int games, int wins, int expectedGames, int expectedWins)
        {
            SettingsStore.Current.Set("opp.games.smart", games);
            SettingsStore.Current.Set("opp.wins.smart", wins);

            Assert.Equal((expectedGames, expectedWins), OpponentStatsStore.For("smart"));

            OpponentStatsStore.Record("smart", false);

            Assert.Equal((expectedGames + 1, expectedWins), OpponentStatsStore.For("smart"));
        }
    }
}
