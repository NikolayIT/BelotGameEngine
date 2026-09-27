namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;

    using Belot.NeuralTrainer;

    using Xunit;

    public class MatchResultTests
    {
        [Theory]
        [InlineData(0.2)]
        [InlineData(0.5)]
        [InlineData(0.9)]
        public void EloUncertaintyMatchesNumericalDerivative(double score)
        {
            const double epsilon = 1e-6;
            var result = new MatchResult(2000, score, 0.01, 0);
            var above = new MatchResult(2000, score + epsilon, 0.01, 0);
            var below = new MatchResult(2000, score - epsilon, 0.01, 0);
            var numerical = (above.Elo - below.Elo) / (2 * epsilon) * result.Sigma;
            Assert.Equal(numerical, result.EloSigma, 6);
        }

        [Fact]
        public void ConfidenceBoundsRetainPrecisionAtThePromotionBoundary()
        {
            var failed = new MatchResult(1000, 0.53, 0.0154, 0);
            var passed = new MatchResult(1000, 0.53, 0.0152, 0);
            Assert.True(failed.Lower95 < 0.5);
            Assert.True(passed.Lower95 > 0.5);
            Assert.Equal(0.5, new MatchResult(20000, 0.5, 0, 0).Lower95);
            Assert.Equal(0, new MatchResult(20000, 0.5, 0, 0).Elo);
            Assert.Equal(1, new MatchResult(1000, 0.99, 0.02, 0).Upper95);
            Assert.Equal(0, new MatchResult(1000, 0.01, 0.02, 0).Lower95);
            Assert.True(double.IsNaN(new MatchResult(1000, 1, 0, 0).EloSigma));
        }
    }
}
