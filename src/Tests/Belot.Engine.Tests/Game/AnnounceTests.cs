namespace Belot.Engine.Tests.Game
{
    using Belot.Engine.Cards;
    using Belot.Engine.Game;

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
        }
    }
}
