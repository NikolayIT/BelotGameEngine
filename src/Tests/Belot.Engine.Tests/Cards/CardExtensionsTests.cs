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
    }
}
