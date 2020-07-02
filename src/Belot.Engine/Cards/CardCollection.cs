namespace Belot.Engine.Cards
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <inheritdoc cref="ICollection" />
    /// <summary>
    /// Low memory (only 1 integer per instance) fast implementation of card collection.
    /// </summary>
    public class CardCollection : ICollection<Card>, ICloneable
    {
        // TODO: Add method for fast checking "is any of suit"
        private const int MaxCards = 32;
        private const int MaxCardsMinusOne = 31;

        public uint cards; // 32 bits for 32 possible cards

        public CardCollection(uint bitMask = 0)
        {
            this.cards = bitMask;
        }

        public CardCollection(CardCollection cardCollection, Func<Card, bool> predicate)
        {
            for (var currentHashCode = 0; currentHashCode < MaxCards; currentHashCode++)
            {
                if (((cardCollection.cards >> currentHashCode) & 1) == 1
                    && predicate(Card.AllCards[currentHashCode]))
                {
                    this.cards |= 1U << currentHashCode;
                }
            }
        }

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

            if (suit == CardSuit.Diamond)
            {
                return (this.cards & 0b00000000000000001111111100000000u) != 0;
            }

            if (suit == CardSuit.Heart)
            {
                return (this.cards & 0b00000000111111110000000000000000u) != 0;
            }

            if (suit == CardSuit.Spade)
            {
                return (this.cards & 0b11111111000000000000000000000000u) != 0;
            }

            return false;
        }

        public int Count
        {
            get
            {
                var bits = this.cards;
                var cardsCount = 0;
                while (bits != 0)
                {
                    cardsCount++;
                    bits &= bits - 1;
                }

                return cardsCount;
            }
        }

        public bool IsReadOnly => false;

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
        }

        public void Clear()
        {
            this.cards = 0;
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
                return true;
            }

            return false;
        }

        public object Clone()
        {
            return new CardCollection(this.cards);
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
