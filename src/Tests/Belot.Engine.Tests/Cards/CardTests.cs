namespace Belot.Engine.Tests.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;

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

        // The canonical Belot strength orders (etc/Rules.md §2; identical to the tables recovered
        // from the 2001 belot.exe): trump J > 9 > A > 10 > K > Q > 8 > 7, plain A > 10 > K > Q > J > 9 > 8 > 7.
        // Any refactoring of Card's internals must keep reproducing these exact values.
        [Theory]
        [InlineData(CardType.Seven, 1, 1)]
        [InlineData(CardType.Eight, 2, 2)]
        [InlineData(CardType.Nine, 7, 3)]
        [InlineData(CardType.Ten, 5, 7)]
        [InlineData(CardType.Jack, 8, 4)]
        [InlineData(CardType.Queen, 3, 5)]
        [InlineData(CardType.King, 4, 6)]
        [InlineData(CardType.Ace, 6, 8)]
        public void StrengthOrderTablesShouldMatchTheCanonicalBelotOrders(
            CardType cardType,
            int expectedTrumpOrder,
            int expectedNoTrumpOrder)
        {
            foreach (var cardSuit in Card.AllSuits)
            {
                var card = Card.GetCard(cardSuit, cardType);
                Assert.Equal(expectedTrumpOrder, card.TrumpOrder);
                Assert.Equal(expectedNoTrumpOrder, card.NoTrumpOrder);
            }
        }

        [Fact]
        public void GetCardShouldReturnTheSameFlyweightInstanceEveryTime()
        {
            foreach (var cardSuit in Card.AllSuits)
            {
                foreach (var cardType in Card.AllTypes)
                {
                    Assert.Same(Card.GetCard(cardSuit, cardType), Card.GetCard(cardSuit, cardType));
                    Assert.Same(Card.GetCard(cardSuit, cardType), Card.AllCards[((int)cardSuit * 8) + (int)cardType]);
                }
            }
        }

        [Fact]
        public void AllCardsShouldContain32DistinctNonNullCards()
        {
            Assert.Equal(32, Card.AllCards.Length);
            Assert.Equal(32, Card.AllCards.Distinct().Count());
            Assert.All(Card.AllCards, Assert.NotNull);
        }
    }
}
