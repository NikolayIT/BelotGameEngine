namespace Belot.Engine.Tests.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    using Xunit;

    public class CardExtensionsTests
    {
        [Fact]
        public void CardSuitToFriendlyStringShouldReturnDifferentValidValueForEachPossibleParameter()
        {
            var values = new HashSet<string>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                var stringValue = cardSuitValue.ToFriendlyString();
                Assert.False(values.Contains(stringValue), $"Duplicate string value \"{stringValue}\" for card suit \"{cardSuitValue}\"");
                values.Add(stringValue);
            }
        }

        [Fact]
        public void CardSuitToFriendlyStringShouldThrowAnExceptionWhenCalledOnAnInvalidValue()
        {
            var cardSuits = Enum.GetValues(typeof(CardSuit));
            var cardSuit = cardSuits.OfType<CardSuit>().Max() + 1;
            Assert.Throws<ArgumentException>(() => cardSuit.ToFriendlyString());
        }

        [Fact]
        public void CardTypeToFriendlyStringShouldReturnDifferentValidValueForEachPossibleParameter()
        {
            var values = new HashSet<string>();
            foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
            {
                var stringValue = cardTypeValue.ToFriendlyString();
                Assert.False(values.Contains(stringValue), $"Duplicate string value \"{stringValue}\" for card suit \"{cardTypeValue}\"");
                values.Add(stringValue);
            }
        }

        [Fact]
        public void CardTypeToFriendlyStringShouldThrowAnExceptionWhenCalledOnAnInvalidValue()
        {
            var cardTypes = Enum.GetValues(typeof(CardType));
            var cardType = cardTypes.OfType<CardType>().Max() + 1;
            Assert.Throws<ArgumentException>(() => cardType.ToFriendlyString());
        }

        [Fact]
        public void CardSuitToBidTypeShouldReturnDifferentValidValueForEachPossibleParameter()
        {
            var values = new HashSet<BidType>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                var bidType = cardSuitValue.ToBidType();
                Assert.False(values.Contains(bidType), $"Duplicate string value \"{bidType}\" for card suit \"{cardSuitValue}\"");
                values.Add(bidType);
            }
        }

        [Fact]
        public void CardSuitToBidTypeShouldReturnPassWhenCalledOnAnInvalidValue()
        {
            var cardSuits = Enum.GetValues(typeof(CardSuit));
            var cardSuit = cardSuits.OfType<CardSuit>().Max() + 1;
            var bidType = cardSuit.ToBidType();
            Assert.Equal(BidType.Pass, bidType);
        }

        [Fact]
        public void GetValueShouldReturnPositiveValueForEveryCardType()
        {
            foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
            {
                var card = Card.GetCard(CardSuit.Diamond, cardTypeValue);
                var allTrumpValue = card.GetValue(BidType.AllTrumps); // Not expecting exceptions here
                Assert.True(allTrumpValue >= 0);
                var noTrumpValue = card.GetValue(BidType.NoTrumps); // Not expecting exceptions here
                Assert.True(noTrumpValue >= 0);
                var trumpValue = card.GetValue(BidType.Diamonds); // Not expecting exceptions here
                Assert.True(trumpValue >= 0);
            }
        }

        [Theory]
        [InlineData(BidType.Pass, 0)]
        [InlineData(BidType.Clubs, 152)]
        [InlineData(BidType.Clubs | BidType.Double, 152)]
        [InlineData(BidType.Clubs | BidType.ReDouble, 152)]
        [InlineData(BidType.Diamonds, 152)]
        [InlineData(BidType.Diamonds | BidType.Double, 152)]
        [InlineData(BidType.Diamonds | BidType.ReDouble, 152)]
        [InlineData(BidType.Hearts, 152)]
        [InlineData(BidType.Hearts | BidType.Double, 152)]
        [InlineData(BidType.Hearts | BidType.ReDouble, 152)]
        [InlineData(BidType.Spades, 152)]
        [InlineData(BidType.Spades | BidType.Double, 152)]
        [InlineData(BidType.Spades | BidType.ReDouble, 152)]
        [InlineData(BidType.NoTrumps, 120)]
        [InlineData(BidType.NoTrumps | BidType.Double, 120)]
        [InlineData(BidType.NoTrumps | BidType.ReDouble, 120)]
        [InlineData(BidType.AllTrumps, 248)]
        [InlineData(BidType.AllTrumps | BidType.Double, 248)]
        [InlineData(BidType.AllTrumps | BidType.ReDouble, 248)]
        public void SumOfAllCardValuesShouldBeCorrect(BidType bidType, int expectedPoints)
        {
            var allPoints = Card.AllCards.Sum(x => x.GetValue(bidType));
            Assert.Equal(expectedPoints, allPoints);
        }
    }
}
