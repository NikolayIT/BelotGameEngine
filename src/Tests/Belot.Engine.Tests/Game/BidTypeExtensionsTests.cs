namespace Belot.Engine.Tests.Game
{
    using System;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    using Xunit;

    public class BidTypeExtensionsTests
    {
        [Theory]
        [InlineData(BidType.Clubs, CardSuit.Club)]
        [InlineData(BidType.Diamonds, CardSuit.Diamond)]
        [InlineData(BidType.Hearts, CardSuit.Heart)]
        [InlineData(BidType.Spades, CardSuit.Spade)]
        [InlineData(BidType.Clubs | BidType.Double, CardSuit.Club)]
        [InlineData(BidType.Diamonds | BidType.Double, CardSuit.Diamond)]
        [InlineData(BidType.Hearts | BidType.Double, CardSuit.Heart)]
        [InlineData(BidType.Spades | BidType.Double, CardSuit.Spade)]
        [InlineData(BidType.Clubs | BidType.ReDouble, CardSuit.Club)]
        [InlineData(BidType.Diamonds | BidType.ReDouble, CardSuit.Diamond)]
        [InlineData(BidType.Hearts | BidType.ReDouble, CardSuit.Heart)]
        [InlineData(BidType.Spades | BidType.ReDouble, CardSuit.Spade)]
        public void ToCardSuitShouldWorkCorrectly(BidType bidType, CardSuit expectedCardSuit)
        {
            var actualResult = bidType.ToCardSuit();
            Assert.Equal(expectedCardSuit, actualResult);
        }

        [Theory]
        [InlineData(BidType.Pass)]
        [InlineData(BidType.NoTrumps)]
        [InlineData(BidType.AllTrumps)]
        [InlineData(BidType.NoTrumps | BidType.Double)]
        [InlineData(BidType.AllTrumps | BidType.Double)]
        [InlineData(BidType.NoTrumps | BidType.ReDouble)]
        [InlineData(BidType.AllTrumps | BidType.ReDouble)]
        public void ToCardSuitShouldShouldThrowExceptionWhenGivenInvalidValues(BidType bidType)
        {
            Assert.Throws<ArgumentException>(() => bidType.ToCardSuit());
        }
    }
}
