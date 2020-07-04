namespace Belot.Engine.Tests.GameMechanics
{
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;

    using Xunit;

    public class ScoreManagerTests
    {
        [Theory]
        [InlineData(BidType.AllTrumps, 0, false, 0)]
        [InlineData(BidType.AllTrumps, 258, false, 26)]
        [InlineData(BidType.AllTrumps, 155, true, 16)]
        [InlineData(BidType.AllTrumps, 153, false, 15)]
        [InlineData(BidType.AllTrumps, 154, true, 15)]
        [InlineData(BidType.AllTrumps, 154, false, 16)]
        [InlineData(BidType.AllTrumps, 3, false, 0)]
        [InlineData(BidType.NoTrumps, 130, false, 13)]
        [InlineData(BidType.NoTrumps, 134, false, 13)]
        [InlineData(BidType.NoTrumps, 136, false, 14)]
        [InlineData(BidType.NoTrumps, 260, true, 26)]
        [InlineData(BidType.NoTrumps, 0, true, 0)]
        [InlineData(BidType.Spades, 0, false, 0)]
        [InlineData(BidType.Diamonds, 162, false, 16)]
        [InlineData(BidType.Hearts, 87, true, 9)]
        [InlineData(BidType.Clubs, 85, false, 8)]
        [InlineData(BidType.Clubs, 86, false, 9)]
        [InlineData(BidType.Hearts, 86, true, 8)]
        [InlineData(BidType.Spades, 76, false, 8)]
        [InlineData(BidType.Diamonds, 76, true, 7)]
        public void RoundPointsShouldWorkCorrectly(BidType bidType, int points, bool isWinner, int expectedResult)
        {
            var actualResult = ScoreManager.RoundPoints(bidType, points, isWinner);
            Assert.Equal(expectedResult, actualResult);
        }
    }
}
