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
        public void ShuffleAndDealShouldProduceAll32DistinctCards()
        {
            var deck = new Deck();
            for (var round = 0; round < 10; round++)
            {
                deck.Shuffle();
                var seen = new CardCollection();
                for (var i = 0; i < 32; i++)
                {
                    seen.Add(deck.GetNextCard());
                }

                Assert.Equal(32, seen.Count);
            }
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
