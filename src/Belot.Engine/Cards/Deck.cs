namespace Belot.Engine.Cards
{
    using System.Collections.Generic;

    public class Deck
    {
        private readonly IList<Card> listOfCards;

        private int currentCardIndex;

        public Deck()
        {
            this.listOfCards = new CardCollection(CardCollection.AllBelotCardsBitMask).Shuffle();
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
