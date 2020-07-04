namespace Belot.Engine.Cards
{
    using System.Linq;

    public class Deck
    {
        private readonly Card[] listOfCards;

        private int currentCardIndex;

        public Deck()
        {
            this.listOfCards = Card.AllCards.ToArray();
        }

        public void Shuffle()
        {
            this.listOfCards.Shuffle();
            this.currentCardIndex = 0;
        }

        public Card GetNextCard() => this.listOfCards[this.currentCardIndex++];
    }
}
