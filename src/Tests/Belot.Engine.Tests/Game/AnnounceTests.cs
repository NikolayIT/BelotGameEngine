namespace Belot.Engine.Tests.Game
{
    using System;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    using Xunit;

    public class AnnounceTests
    {
        [Theory]
        [InlineData(AnnounceType.FourJacks, CardSuit.Spade, CardType.Jack, AnnounceType.FourNines, CardSuit.Spade, CardType.Nine, 1)]
        [InlineData(AnnounceType.FourNines, CardSuit.Spade, CardType.Nine, AnnounceType.FourOfAKind, CardSuit.Spade, CardType.Ace, 1)]
        [InlineData(AnnounceType.FourOfAKind, CardSuit.Spade, CardType.Ace, AnnounceType.FourOfAKind, CardSuit.Spade, CardType.King, 1)]
        [InlineData(AnnounceType.FourOfAKind, CardSuit.Spade, CardType.King, AnnounceType.FourOfAKind, CardSuit.Spade, CardType.Queen, 1)]
        [InlineData(AnnounceType.FourOfAKind, CardSuit.Spade, CardType.Queen, AnnounceType.FourOfAKind, CardSuit.Spade, CardType.Ten, 1)]
        [InlineData(AnnounceType.SequenceOf8, CardSuit.Spade, CardType.Ace, AnnounceType.SequenceOf7, CardSuit.Diamond, CardType.Ace, 1)]
        [InlineData(AnnounceType.SequenceOf7, CardSuit.Spade, CardType.Ace, AnnounceType.SequenceOf6, CardSuit.Diamond, CardType.Ace, 1)]
        [InlineData(AnnounceType.SequenceOf6, CardSuit.Spade, CardType.Ace, AnnounceType.SequenceOf5, CardSuit.Diamond, CardType.Ace, 1)]
        [InlineData(AnnounceType.SequenceOf5, CardSuit.Spade, CardType.Ace, AnnounceType.SequenceOf4, CardSuit.Diamond, CardType.Ace, 1)]
        [InlineData(AnnounceType.SequenceOf4, CardSuit.Spade, CardType.Ace, AnnounceType.SequenceOf3, CardSuit.Diamond, CardType.Ace, 1)]
        [InlineData(AnnounceType.SequenceOf3, CardSuit.Spade, CardType.Ace, AnnounceType.SequenceOf3, CardSuit.Diamond, CardType.King, 1)]
        [InlineData(AnnounceType.SequenceOf3, CardSuit.Spade, CardType.King, AnnounceType.SequenceOf3, CardSuit.Diamond, CardType.Queen, 1)]
        [InlineData(AnnounceType.SequenceOf3, CardSuit.Spade, CardType.Queen, AnnounceType.SequenceOf3, CardSuit.Diamond, CardType.Jack, 1)]
        [InlineData(AnnounceType.SequenceOf3, CardSuit.Spade, CardType.Jack, AnnounceType.SequenceOf3, CardSuit.Diamond, CardType.Ten, 1)]
        [InlineData(AnnounceType.SequenceOf3, CardSuit.Spade, CardType.Ten, AnnounceType.SequenceOf3, CardSuit.Diamond, CardType.Nine, 1)]
        [InlineData(AnnounceType.SequenceOf4, CardSuit.Spade, CardType.Ace, AnnounceType.SequenceOf4, CardSuit.Diamond, CardType.Ace, 0)]
        [InlineData(AnnounceType.SequenceOf4, CardSuit.Spade, CardType.Ace, AnnounceType.SequenceOf5, CardSuit.Diamond, CardType.Ace, -1)]
        public void CompareToWorksCorrectly(
            AnnounceType firstAnnounceType,
            CardSuit firstCardSuit,
            CardType firstCardType,
            AnnounceType secondAnnounceType,
            CardSuit secondCardSuit,
            CardType secondCardType,
            int expectedCompareResult)
        {
            var firstAnnounce = new Announce(firstAnnounceType, Card.GetCard(firstCardSuit, firstCardType));
            var secondAnnounce = new Announce(secondAnnounceType, Card.GetCard(secondCardSuit, secondCardType));

            var compareResult = firstAnnounce.CompareTo(secondAnnounce);
            Assert.Equal(expectedCompareResult, compareResult);

            var reverseCompareResult = secondAnnounce.CompareTo(firstAnnounce);
            Assert.Equal(-expectedCompareResult, reverseCompareResult);

            var sameReferenceResult = firstAnnounce.CompareTo(firstAnnounce);
            Assert.Equal(0, sameReferenceResult);

            var comparedWithNullResult = firstAnnounce.CompareTo(null);
            Assert.Equal(1, comparedWithNullResult);
        }

        [Fact]
        public void ConstructorShouldSetCorrectPropertyValues()
        {
            var announceCard = Card.GetCard(CardSuit.Diamond, CardType.Queen);
            var announce = new Announce(AnnounceType.SequenceOf3, announceCard);
            Assert.Equal(AnnounceType.SequenceOf3, announce.Type);
            Assert.Equal(announceCard, announce.Card);
            Assert.Null(announce.IsActive);
            Assert.Equal(PlayerPosition.Unknown, announce.Player);

            announce.IsActive = true;
            Assert.True(announce.IsActive);

            announce.Player = PlayerPosition.West;
            Assert.Equal(PlayerPosition.West, announce.Player);
        }

        [Fact]
        public void ValuePropertyHasValidPositiveValueForEachAnnounceType()
        {
            foreach (AnnounceType announceType in Enum.GetValues(typeof(AnnounceType)))
            {
                var announceCard = Card.GetCard(CardSuit.Heart, CardType.Ace);
                var announce = new Announce(announceType, announceCard);
                Assert.True(announce.Value >= 20);
            }

            var invalidAnnounceType = Enum.GetValues(typeof(AnnounceType)).OfType<AnnounceType>().Max() + 1;
            var invalidAnnounce = new Announce(invalidAnnounceType, Card.GetCard(CardSuit.Spade, CardType.Jack));
            Assert.Equal(0, invalidAnnounce.Value);
        }

        [Fact]
        public void ToStringShouldReturnValidStringValues()
        {
            foreach (AnnounceType announceType in Enum.GetValues(typeof(AnnounceType)))
            {
                var announceCard = Card.GetCard(CardSuit.Heart, CardType.Ace);
                var announce = new Announce(announceType, announceCard);
                Assert.False(string.IsNullOrWhiteSpace(announce.ToString()));
            }

            var invalidAnnounceType = Enum.GetValues(typeof(AnnounceType)).OfType<AnnounceType>().Max() + 1;
            var invalidAnnounce = new Announce(invalidAnnounceType, Card.GetCard(CardSuit.Spade, CardType.Jack));
            Assert.Throws<BelotGameException>(() => invalidAnnounce.ToString());
        }
    }
}
