namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// IList of Card wrapper
    /// </summary>
    public class CardsCollection : IList<Card>
    {
        private IList<Card> cards;
 
        public CardsCollection()
        {
            this.cards = new List<Card>();
        }

        public CardsCollection(IEnumerable<Card> cardsList)
            : this()
        {
            foreach (var card in cardsList)
            {
                this.cards.Add(card);
            }
        }

        public int Count
        {
            get
            {
                return this.cards.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public Card this[int index]
        {
            get
            {
                return this.cards[index];
            }

            set
            {
                this.cards[index] = value;
            }
        }

        public static CardsCollection GetFullCardDeck()
        {
            var cards = new CardsCollection();
            foreach (CardSuit cardSuit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardType in Enum.GetValues(typeof(CardType)))
                {
                    cards.Add(new Card(cardType, cardSuit));
                }
            }

            return cards;
        }

        public int IndexOf(Card item)
        {
            return this.cards.IndexOf(item);
        }

        public void Insert(int index, Card item)
        {
            this.cards.Insert(index, item);
        }

        public void Add(Card item)
        {
            this.cards.Add(item);
        }

        public void RemoveAt(int index)
        {
            this.cards.RemoveAt(index);
        }

        public bool Remove(Card item)
        {
            var removed = this.cards.Remove(item);
            return removed;
        }

        public void Clear()
        {
            this.cards.Clear();
        }

        public bool Contains(Card item)
        {
            return this.cards.Contains(item);
        }

        public void CopyTo(Card[] array, int arrayIndex)
        {
            this.cards.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Card> GetEnumerator()
        {
            return this.cards.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var card in this.cards)
            {
                sb.AppendFormat("{0} ", card);
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Returns the number of "belot" combinations (king + queen of the same suit)
        /// </summary>
        public int NumberOfQueenAndKingCombinations()
        {
            var numberOfCombinations = 0;
            foreach (CardSuit cardSuit in Enum.GetValues(typeof(CardSuit)))
            {
                var queen = false;
                var king = false;
                foreach (var card in this.cards)
                {
                    if (card.Suit == cardSuit)
                    {
                        if (card.Type == CardType.King)
                        {
                            king = true;
                        }

                        if (card.Type == CardType.Queen)
                        {
                            queen = true;
                        }
                    }
                }

                if (queen && king)
                {
                    numberOfCombinations++;
                }
            }

            return numberOfCombinations;
        }

        public void Sort(ContractType contract)
        {
            if (contract == ContractType.AllTrumps)
            {
                this.SortForAllTrump();
            }

            if (contract == ContractType.NoTrumps)
            {
                this.SortForNoTrump();
            }

            if (contract == ContractType.Spades)
            {
                this.SortForSuit(CardSuit.Spades);
            }

            if (contract == ContractType.Hearts)
            {
                this.SortForSuit(CardSuit.Hearts);
            }

            if (contract == ContractType.Diamonds)
            {
                this.SortForSuit(CardSuit.Diamonds);
            }

            if (contract == ContractType.Clubs)
            {
                this.SortForSuit(CardSuit.Clubs);
            }
        }

        // TODO: Define comparators
        private static int CardSuitOrderValue(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Spades:
                    return 40;
                case CardSuit.Hearts:
                    return 30;
                case CardSuit.Diamonds:
                    return 10;
                case CardSuit.Clubs:
                    return 20;
                default:
                    throw new ArgumentOutOfRangeException("suit");
            }
        }

        private void SortForAllTrump()
        {
            this.cards = this.cards.OrderByDescending(card =>
            {
                if (card.Type == CardType.Jack)
                {
                    return 8 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Nine)
                {
                    return 7 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Ace)
                {
                    return 6 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Ten)
                {
                    return 5 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.King)
                {
                    return 4 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Queen)
                {
                    return 3 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Eight)
                {
                    return 2 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Seven)
                {
                    return 1 + CardSuitOrderValue(card.Suit);
                }

                return 0;
            }).ToList();
        }

        private void SortForNoTrump()
        {
            this.cards = this.cards.OrderByDescending(card =>
            {
                if (card.Type == CardType.Ace)
                {
                    return 8 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Ten)
                {
                    return 7 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.King)
                {
                    return 6 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Queen)
                {
                    return 5 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Jack)
                {
                    return 4 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Nine)
                {
                    return 3 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Eight)
                {
                    return 2 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Seven)
                {
                    return 1 + CardSuitOrderValue(card.Suit);
                }

                return 0;
            }).ToList();
        }

        private void SortForSuit(CardSuit suit)
        {
            this.cards = this.cards.OrderByDescending(card =>
            {
                if (card.Suit == suit)
                {
                    if (card.Type == CardType.Jack)
                    {
                        return 108;
                    }

                    if (card.Type == CardType.Nine)
                    {
                        return 107;
                    }

                    if (card.Type == CardType.Ace)
                    {
                        return 106;
                    }

                    if (card.Type == CardType.Ten)
                    {
                        return 105;
                    }

                    if (card.Type == CardType.King)
                    {
                        return 104;
                    }

                    if (card.Type == CardType.Queen)
                    {
                        return 103;
                    }

                    if (card.Type == CardType.Eight)
                    {
                        return 102;
                    }

                    if (card.Type == CardType.Seven)
                    {
                        return 101;
                    }
                }

                if (card.Type == CardType.Ace)
                {
                    return 8 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Ten)
                {
                    return 7 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.King)
                {
                    return 6 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Queen)
                {
                    return 5 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Jack)
                {
                    return 4 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Nine)
                {
                    return 3 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Eight)
                {
                    return 2 + CardSuitOrderValue(card.Suit);
                }

                if (card.Type == CardType.Seven)
                {
                    return 1 + CardSuitOrderValue(card.Suit);
                }

                return 0;
            }).ToList();
        }
    }
}
