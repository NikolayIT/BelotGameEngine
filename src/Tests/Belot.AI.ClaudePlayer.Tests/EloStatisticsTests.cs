namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Linq;

    using Belot.GamesSimulator;

    using Xunit;

    public class EloStatisticsTests
    {
        [Fact]
        public void StandardErrorUsesMirroredPairsAsIndependentUnits()
        {
            Assert.Equal(0.5, EloStatistics.StandardError(new[] { 0d, 1d }));
            Assert.Equal(0, EloStatistics.StandardError(new[] { 0.5, 0.5, 0.5 }));
            Assert.Equal(0, EloStatistics.StandardError(Array.Empty<double>()));
            Assert.Equal(0, EloStatistics.StandardError(new[] { 1d }));
        }

        [Fact]
        public void TwoPlayerFitMatchesRegularizedOddsAndKeepsAnchorFixed()
        {
            var wins = new double[,]
            {
                { 0, 800 },
                { 200, 0 },
            };
            var games = new double[,]
            {
                { 0, 1000 },
                { 1000, 0 },
            };
            var ratings = EloStatistics.Fit(wins, games, 2, 1200);
            Assert.Equal(1200, ratings[0]);
            Assert.Equal(1200 - (400 * Math.Log10(805d / 205)), ratings[1], 6);
        }

        [Fact]
        public void BootstrapHasNoVarianceWhenEveryPairSplits()
        {
            var scores = new double[9][];
            scores[1] = Enumerable.Repeat(0.5, 8).ToArray();
            scores[2] = Enumerable.Repeat(0.5, 4).ToArray();
            scores[5] = Enumerable.Repeat(0.5, 4).ToArray();
            Assert.All(EloStatistics.BootstrapErrors(scores, 3, 32, 7, 1200), x => Assert.Equal(0, x));
        }

        [Fact]
        public void BootstrapPreservesCorrelatedOutcomesOnSharedDealSeeds()
        {
            var scores = new double[9][];
            scores[1] = new[] { 0d, 0.5, 1, 0.5, 0, 1, 0.5, 1 };
            scores[2] = (double[])scores[1].Clone();
            scores[5] = Enumerable.Repeat(0.5, 8).ToArray();
            var errors = EloStatistics.BootstrapErrors(scores, 3, 100, 7, 1200);
            Assert.True(errors[1] > 0);
            Assert.Equal(errors[1], errors[2], 10);
        }

        [Fact]
        public void BootstrapIsReproducibleWithDifferentMatchupLengths()
        {
            var scores = new double[9][];
            scores[1] = new[] { 0d, 0.5, 1, 0.5, 0, 1, 0.5, 1 };
            scores[2] = new[] { 0d, 0.5, 1, 0.5 };
            scores[5] = new[] { 1d, 0.5, 0, 0.5 };
            var first = EloStatistics.BootstrapErrors(scores, 3, 100, 7, 1200);
            var second = EloStatistics.BootstrapErrors(scores, 3, 100, 7, 1200);
            Assert.Equal(first, second);
            Assert.Equal(0, first[0], 10);
            Assert.All(first.Skip(1), x => Assert.True(double.IsFinite(x) && x > 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => EloStatistics.BootstrapErrors(scores, 3, 1, 7, 1200));
        }
    }
}
