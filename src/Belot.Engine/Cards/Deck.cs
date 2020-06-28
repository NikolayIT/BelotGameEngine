namespace Belot.Engine.Cards
{
    using System.Collections.Generic;
    using System.Linq;

    public class Deck
    {
        private IList<Card> listOfCards;

        private int currentCardIndex;

        public Deck()
        {
            this.listOfCards = new CardCollection(CardCollection.AllBelotCardsBitMask).ToList();
            this.Shuffle();
        }

        public void Shuffle()
        {
            this.listOfCards = this.listOfCards.Shuffle();
            this.currentCardIndex = this.listOfCards.Count - 1;
        }

        public Card GetNextCard()
        {
            if (this.currentCardIndex < 0)
            {
                throw new BelotGameException("Deck is empty!");
            }

            var card = this.listOfCards[this.currentCardIndex];
            this.currentCardIndex--;
            return card;
        }
    }
}
