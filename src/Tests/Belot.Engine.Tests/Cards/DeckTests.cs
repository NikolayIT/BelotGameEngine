namespace Belot.Engine.Tests.Cards
{
    using System;

    using Belot.Engine.Cards;

    using Xunit;

    public class DeckTests
    {
        [Fact]
        public void ShuffleShouldNotCrash()
        {
            var deck = new Deck();
            for (var i = 0; i < 100; i++)
            {
                deck.Shuffle();
            }
        }

        [Fact]
        public void GetNextCardShouldThrowExceptionWhenCalled33Times()
        {
            var deck = new Deck();
            for (var i = 1; i <= 32; i++)
            {
                deck.GetNextCard();
            }

            Assert.Throws<IndexOutOfRangeException>(() => deck.GetNextCard());
        }

        [Fact]
        public void GetNextCardShouldThrowExceptionWhenCalled33TimesAfterShuffle()
        {
            var deck = new Deck();
            deck.Shuffle();
            for (var i = 1; i <= 32; i++)
            {
                deck.GetNextCard();
            }

            Assert.Throws<IndexOutOfRangeException>(() => deck.GetNextCard());
        }
    }
}
