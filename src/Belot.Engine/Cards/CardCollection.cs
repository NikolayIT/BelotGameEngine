namespace Belot.Engine.Cards
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <inheritdoc cref="ICollection" />
    /// <summary>
    /// Low memory (only 2 integers per instance) fast implementation of card collection.
    /// </summary>
    public class CardCollection : ICollection<Card>
    {
        // Maps (lowestSetBit * DeBruijnSequence) >> 27 to the bit index. Portable
        // (netstandard2.0) trailing zero count, so loops visit only the set bits.
        private static readonly int[] DeBruijnBitPositions =
            {
                0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
                31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9,
            };

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
            var bits = cardCollection.cards;
            while (bits != 0)
            {
                var currentHashCode = TrailingZeroCount(bits);
                bits &= bits - 1;
                if (predicate(Card.AllCards[currentHashCode]))
                {
                    this.cards |= 1U << currentHashCode;
                    this.Count++;
                }
            }
        }

        internal CardCollection(uint bitMask)
        {
            this.cards = bitMask;
            this.Count = PopCount(bitMask);
        }

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        internal uint BitMask => this.cards;

        public bool Any(Func<Card, bool> predicate)
        {
            var bits = this.cards;
            while (bits != 0)
            {
                if (predicate(Card.AllCards[TrailingZeroCount(bits)]))
                {
                    return true;
                }

                bits &= bits - 1;
            }

            return false;
        }

        public Card FirstOrDefault()
        {
            return this.cards == 0 ? null : Card.AllCards[TrailingZeroCount(this.cards)];
        }

        public int GetCount(Func<Card, bool> predicate)
        {
            var count = 0;
            var bits = this.cards;
            while (bits != 0)
            {
                if (predicate(Card.AllCards[TrailingZeroCount(bits)]))
                {
                    count++;
                }

                bits &= bits - 1;
            }

            return count;
        }

        public CardCollection Where(Func<Card, bool> predicate)
        {
            return new CardCollection(this, predicate);
        }

        public Card Lowest<TKey>(Func<Card, TKey> orderByFunc)
            where TKey : IComparable
        {
            Card minCard = null;
            var bits = this.cards;
            while (bits != 0)
            {
                var card = Card.AllCards[TrailingZeroCount(bits)];
                bits &= bits - 1;
                if (minCard == null || orderByFunc(card).CompareTo(orderByFunc(minCard)) < 0)
                {
                    minCard = card;
                }
            }

            return minCard;
        }

        public Card Highest<TKey>(Func<Card, TKey> orderByFunc)
            where TKey : IComparable
        {
            Card maxCard = null;
            var bits = this.cards;
            while (bits != 0)
            {
                var card = Card.AllCards[TrailingZeroCount(bits)];
                bits &= bits - 1;
                if (maxCard == null || orderByFunc(card).CompareTo(orderByFunc(maxCard)) > 0)
                {
                    maxCard = card;
                }
            }

            return maxCard;
        }

        public bool HasAnyOfSuit(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Club:
                    return (this.cards & 0b00000000000000000000000011111111u) != 0;
                case CardSuit.Diamond:
                    return (this.cards & 0b00000000000000001111111100000000u) != 0;
                case CardSuit.Heart:
                    return (this.cards & 0b00000000111111110000000000000000u) != 0;
                default: // CardSuit.Spade
                    return (this.cards & 0b11111111000000000000000000000000u) != 0;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this.cards);
        }

        IEnumerator<Card> IEnumerable<Card>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Card card)
        {
            if (!this.Contains(card))
            {
                this.cards |= 1U << card.GetHashCode();
                this.Count++;
            }
        }

        public void Clear()
        {
            this.cards = 0;
            this.Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Card card)
        {
            return ((this.cards >> card.GetHashCode()) & 1) == 1;
        }

        public void CopyTo(Card[] array, int arrayIndex)
        {
            var bits = this.cards;
            while (bits != 0)
            {
                array[arrayIndex++] = Card.AllCards[TrailingZeroCount(bits)];
                bits &= bits - 1;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TrailingZeroCount(uint bits)
        {
            return DeBruijnBitPositions[((bits & (0u - bits)) * 0x077CB531u) >> 27];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PopCount(uint bits)
        {
            bits -= (bits >> 1) & 0x55555555u;
            bits = (bits & 0x33333333u) + ((bits >> 2) & 0x33333333u);
            return (int)((((bits + (bits >> 4)) & 0x0F0F0F0Fu) * 0x01010101u) >> 24);
        }

        public struct Enumerator : IEnumerator<Card>
        {
            private readonly uint cards;

            private uint remainingCards;

            private int currentHashCode;

            internal Enumerator(uint cards)
            {
                this.cards = cards;
                this.remainingCards = cards;
                this.currentHashCode = -1;
            }

            public Card Current => Card.AllCards[this.currentHashCode];

            object IEnumerator.Current => Card.AllCards[this.currentHashCode];

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (this.remainingCards == 0)
                {
                    return false;
                }

                this.currentHashCode = TrailingZeroCount(this.remainingCards);
                this.remainingCards &= this.remainingCards - 1;
                return true;
            }

            public void Reset()
            {
                this.remainingCards = this.cards;
                this.currentHashCode = -1;
            }
        }
    }
}
