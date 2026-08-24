namespace Belot.Engine.Tests
{
    using System;

    using Belot.Engine.Players;
    using Belot.Engine.Tests.FakeObjects;

    using Xunit;

    public class BelotGameTests
    {
        // The internal deck is not seedable, so these are invariant tests: whatever the shuffles
        // produce, a finished game must have a strict winner with at least 151 points, a
        // consistent Winner property, and exactly one EndOfGame callback per player carrying the
        // same result object.
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(42)]
        public void PlayGameEndsWithAStrictWinnerOver151(int seed)
        {
            var south = new SeededRandomPlayer((seed * 4) + 0, bidRandomly: true);
            var east = new SeededRandomPlayer((seed * 4) + 1, bidRandomly: true);
            var north = new SeededRandomPlayer((seed * 4) + 2, bidRandomly: true);
            var west = new SeededRandomPlayer((seed * 4) + 3, bidRandomly: true);

            var game = new BelotGame(south, east, north, west);
            var result = game.PlayGame(PlayerPosition.South);

            var winnerPoints = Math.Max(result.SouthNorthPoints, result.EastWestPoints);
            var loserPoints = Math.Min(result.SouthNorthPoints, result.EastWestPoints);
            Assert.True(winnerPoints >= 151, $"The winner finished with only {winnerPoints} points.");
            Assert.True(winnerPoints > loserPoints, "A finished game cannot end level.");
            Assert.True(result.RoundsPlayed >= 1);
            Assert.Equal(
                result.SouthNorthPoints > result.EastWestPoints
                    ? PlayerPosition.SouthNorthTeam
                    : PlayerPosition.EastWestTeam,
                result.Winner);

            foreach (var player in new[] { south, east, north, west })
            {
                Assert.Equal(1, player.EndOfGameCalls);
                Assert.Same(result, player.LastGameResult);
            }
        }
    }
}
