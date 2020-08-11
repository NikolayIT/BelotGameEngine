namespace Belot.Engine.Cards
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <inheritdoc cref="ICollection" />
    /// <summary>
    /// Low memory (only 2 integers per instance) fast implementation of card collection.
    /// </summary>
    public class CardCollection : ICollection<Card>
    {
        private const int MaxCards = 32;
        private const int MaxCardsMinusOne = 31;

        private uint cards; // 32 bits for 32 possible cards

        public CardCollection()
        {
        }

        public CardCollection(CardCollection cardCollection)
            : this(cardCollection.cards)
        {
        }

        public CardCollection(CardCollection cardCollection, Func<Card, bool> predicate)
        {
            this.Count = 0;
            for (var currentHashCode = 0; currentHashCode < MaxCards; currentHashCode++)
            {
                if (((cardCollection.cards >> currentHashCode) & 1) == 1
                    && predicate(Card.AllCards[currentHashCode]))
                {
                    this.cards |= 1U << currentHashCode;
                    this.Count++;
                }
            }
        }

        internal CardCollection(uint bitMask)
        {
            this.cards = bitMask;
            this.UpdateCount();
        }

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public bool Any(Func<Card, bool> predicate)
        {
            for (var currentHashCode = 0; currentHashCode < MaxCards; currentHashCode++)
            {
                if (((this.cards >> currentHashCode) & 1) == 1
                    && predicate(Card.AllCards[currentHashCode]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyOfSuit(CardSuit suit)
        {
            if (suit == CardSuit.Club)
            {
                return (this.cards & 0b00000000000000000000000011111111u) != 0;
            }
            else if (suit == CardSuit.Diamond)
            {
                return (this.cards & 0b00000000000000001111111100000000u) != 0;
            }
            else if (suit == CardSuit.Heart)
            {
                return (this.cards & 0b00000000111111110000000000000000u) != 0;
            }
            else
            {
                // suit == CardSuit.Spade
                return (this.cards & 0b11111111000000000000000000000000u) != 0;
            }
        }

        public IEnumerator<Card> GetEnumerator()
        {
            return new CardCollectionEnumerator(this.cards);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(Card item)
        {
            if (!this.Contains(item))
            {
                this.cards |= 1U << item.GetHashCode();
            }

            this.Count++;
        }

        public void Clear()
        {
            this.cards = 0;
            this.Count = 0;
        }

        public bool Contains(Card item)
        {
            return ((this.cards >> item.GetHashCode()) & 1) == 1;
        }

        public void CopyTo(Card[] array, int arrayIndex)
        {
            for (var currentHashCode = 0; currentHashCode < MaxCards; currentHashCode++)
            {
                if (((this.cards >> currentHashCode) & 1) == 1)
                {
                    array[arrayIndex++] = Card.AllCards[currentHashCode];
                }
            }
        }

        public bool Remove(Card item)
        {
            if (this.Contains(item))
            {
                this.cards &= ~(1U << item.GetHashCode());
                this.Count--;
                return true;
            }

            return false;
        }

        private void UpdateCount()
        {
            this.Count = 0;
            var bits = this.cards;
            while (bits != 0)
            {
                this.Count++;
                bits &= bits - 1;
            }
        }

        private class CardCollectionEnumerator : IEnumerator<Card>
        {
            private readonly uint cards;

            private int currentHashCode;

            public CardCollectionEnumerator(uint cards)
            {
                this.cards = cards;
                this.currentHashCode = -1;
            }

            public Card Current => Card.AllCards[this.currentHashCode];

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                while (this.currentHashCode < MaxCardsMinusOne)
                {
                    if (((this.cards >> ++this.currentHashCode) & 1) == 1)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                this.currentHashCode = -1;
            }
        }
    }
}
