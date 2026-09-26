namespace Belot.Engine.Cards
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;

    public class Deck
    {
        private readonly Card[] listOfCards;

        private readonly Random random;

        private int currentCardIndex;

        public Deck()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Deck"/> class.
        /// </summary>
        /// <param name="random">The source of the shuffles; null for a shared per-thread one. Every
        /// shuffle takes the same number of draws, so a seeded source deals the same sequence of
        /// decks.</param>
        public Deck(Random random)
        {
            this.listOfCards = Card.AllCards.ToArray();
            this.random = random;
        }

        public void Shuffle()
        {
            this.listOfCards.Shuffle(this.random ?? ThreadSafeRandom.Current);
            this.currentCardIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Card GetNextCard() => this.listOfCards[this.currentCardIndex++];
    }
}
