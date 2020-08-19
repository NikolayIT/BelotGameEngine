namespace Belot.Engine.Tests.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    using Xunit;

    public class CardTests
    {
        [Fact]
        public void ConstructorShouldUpdatePropertyValues()
        {
            var card = Card.GetCard(CardSuit.Spade, CardType.Queen);
            Assert.Equal(CardSuit.Spade, card.Suit);
            Assert.Equal(CardType.Queen, card.Type);
        }

        [Fact]
        public void GetCardShouldThrowAnExceptionWhenGivenInvalidCardType()
        {
            var cardTypes = Enum.GetValues(typeof(CardType));
            var cardTypeValue = cardTypes.OfType<CardType>().Max() + 1;
            Assert.Throws<IndexOutOfRangeException>(() => Card.GetCard(CardSuit.Spade, cardTypeValue));
        }

        [Theory]
        [InlineData(true, CardSuit.Spade, CardType.Ace, CardSuit.Spade, CardType.Ace)]
        [InlineData(false, CardSuit.Heart, CardType.Jack, CardSuit.Heart, CardType.Queen)]
        [InlineData(false, CardSuit.Heart, CardType.King, CardSuit.Spade, CardType.King)]
        [InlineData(false, CardSuit.Heart, CardType.Nine, CardSuit.Spade, CardType.Ten)]
        public void EqualsShouldWorkCorrectly(
            bool expectedValue,
            CardSuit firstCardSuit,
            CardType firstCardType,
            CardSuit secondCardSuit,
            CardType secondCardType)
        {
            var firstCard = Card.GetCard(firstCardSuit, firstCardType);
            var secondCard = Card.GetCard(secondCardSuit, secondCardType);
            Assert.Equal(expectedValue, firstCard.Equals(secondCard));
            Assert.Equal(expectedValue, secondCard.Equals(firstCard));
        }

        [Fact]
        public void EqualsShouldReturnFalseWhenGivenNullValue()
        {
            var card = Card.GetCard(CardSuit.Club, CardType.Nine);
            Assert.False(card.Equals(null));
            Assert.False(card == null);
        }

        [Fact]
        public void NotEqualsShouldReturnCorrectValues()
        {
            Assert.True(Card.GetCard(CardSuit.Spade, CardType.Seven) != null);
            Assert.True(Card.GetCard(CardSuit.Spade, CardType.Eight) != Card.GetCard(CardSuit.Spade, CardType.Nine));
            Assert.True(Card.GetCard(CardSuit.Spade, CardType.Eight) != Card.GetCard(CardSuit.Diamond, CardType.Eight));
            Assert.True(Card.GetCard(CardSuit.Heart, CardType.Ace) != Card.GetCard(CardSuit.Club, CardType.King));
        }

        [Fact]
        public void EqualsShouldReturnFalseWhenGivenNonCardObject()
        {
            var card = Card.GetCard(CardSuit.Club, CardType.Nine);

            // ReSharper disable once SuspiciousTypeConversion.Global
            var areEqual = card.Equals(new CardTests());
            Assert.False(areEqual);
        }

        [Fact]
        public void GetHashCodeShouldReturnDifferentValidValueForEachCardCombination()
        {
            var values = new HashSet<int>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    var cardHashCode = card.GetHashCode();
                    Assert.False(
                        values.Contains(cardHashCode),
                        $"Duplicate hash code \"{cardHashCode}\" for card \"{card}\"");
                    values.Add(cardHashCode);
                }
            }
        }

        [Fact]
        public void OrderPropertiesShouldReturnDifferentValues()
        {
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                var noTrumpValues = new HashSet<int>();
                var trumpValues = new HashSet<int>();
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);

                    var noTrumpOrder = card.NoTrumpOrder;
                    Assert.False(
                        noTrumpValues.Contains(noTrumpOrder),
                        $"Duplicate no trump order \"{noTrumpOrder}\" for card \"{card}\"");
                    noTrumpValues.Add(noTrumpOrder);

                    var trumpOrder = card.TrumpOrder;
                    Assert.False(
                        trumpValues.Contains(trumpOrder),
                        $"Duplicate trump order \"{trumpOrder}\" for card \"{card}\"");
                    trumpValues.Add(trumpOrder);
                }
            }
        }

        [Fact]
        public void ToStringShouldReturnDifferentValidValueForEachCardCombination()
        {
            var values = new HashSet<string>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    var cardToString = card.ToString();
                    Assert.False(
                        values.Contains(cardToString),
                        $"Duplicate string value \"{cardToString}\" for card \"{card}\"");
                    values.Add(cardToString);
                }
            }
        }

        [Theory]
        [InlineData(CardSuit.Club, CardType.Seven, 0)]
        [InlineData(CardSuit.Club, CardType.Ace, 7)]
        [InlineData(CardSuit.Spade, CardType.King, 30)]
        [InlineData(CardSuit.Spade, CardType.Ace, 31)]
        public void GetHashCodeShouldReturn1ForAceOfClubs(CardSuit cardSuit, CardType cardType, int expectedHashCode)
        {
            var card = Card.GetCard(cardSuit, cardType);
            var hashCode = card.GetHashCode();
            Assert.Equal(expectedHashCode, hashCode);
        }
    }
}
