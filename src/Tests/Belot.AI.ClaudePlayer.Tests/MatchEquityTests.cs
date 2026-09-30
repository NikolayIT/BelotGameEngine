namespace Belot.AI.ClaudePlayer.Tests
{
    using System.IO;

    using Belot.AI.ClaudePlayer.Search;

    using Xunit;

    public class MatchEquityTests
    {
        [Theory]
        [InlineData(151, 100, 10, 0, false, 1)]
        [InlineData(151, 100, 0, 5, false, 0)]
        [InlineData(151, 160, 10, 10, false, -1)]
        [InlineData(200, 100, 26, 0, true, 0)]
        [InlineData(151, 151, 5, 5, false, 0)]
        [InlineData(150, 100, 10, 0, false, 0)]
        public void EndsTheMatchAsTheEngineDoes(int ours, int theirs, int gained, int lost, bool capot, int expected)
        {
            Assert.Equal(expected, MatchEquity.Ends(ours, theirs, gained, lost, capot));
        }

        [Fact]
        public void EqualScoresAreEvenAndMorePointsAreBetter()
        {
            var equity = MatchEquity.FromCsv(new StringReader("16,0,0,0,3\n10,6,0,0,5\n0,0,1,1,1\n25,0,1,0,1\n"));
            for (var score = 0; score <= 150; score += 10)
            {
                Assert.InRange(equity.Win(score, score), 0.49, 0.51);
                Assert.True(equity.Win(score + 10, score) > equity.Win(score, score));
                Assert.InRange(equity.Win(score, 40) + equity.Win(40, score), 0.99, 1.01);
            }

            Assert.True(equity.Win(145, 0) > 0.9);
            Assert.True(equity.PointsPerEquity(0, 0) > 10);
        }

        [Fact]
        public void TheEmbeddedTableIsSensible()
        {
            var equity = MatchEquity.Embedded;
            Assert.InRange(equity.Win(0, 0), 0.49, 0.51);
            Assert.InRange(equity.Win(100, 100), 0.49, 0.51);
            Assert.True(equity.Win(140, 60) > 0.85);
            Assert.True(equity.Win(60, 140) < 0.15);

            // Scoring 11 at 140-100 wins the match outright; a capot does not end it.
            Assert.Equal(1, equity.AfterDeal(140, 100, 11, 5, false));
            Assert.True(equity.AfterDeal(140, 100, 25, 0, true) < 1);
        }
    }
}
