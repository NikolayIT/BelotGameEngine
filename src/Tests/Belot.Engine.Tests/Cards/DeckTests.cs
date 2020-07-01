namespace Belot.Engine.Tests.Cards
{
    using Belot.Engine.Cards;

    using Xunit;

    public class DeckTests
    {
        [Fact]
        public void ShuffleShouldWorkCorrectlyWhenCalled32Times()
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

            Assert.Throws<BelotGameException>(() => deck.GetNextCard());
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

            Assert.Throws<BelotGameException>(() => deck.GetNextCard());
        }
    }
}
