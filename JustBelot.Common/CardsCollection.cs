namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using JustBelot.Common.Extensions;

    /// <summary>
    /// IList of Card wrapper
    /// </summary>
    public class CardsCollection : IList<Card>
    {
        public static readonly CardsCollection FullDeckOfCards = null;

        protected IList<Card> cards;

        static CardsCollection()
        {
            FullDeckOfCards = new CardsCollection
                            {
                                new Card(CardType.Seven, CardSuit.Clubs),
                                new Card(CardType.Seven, CardSuit.Diamonds),
                                new Card(CardType.Seven, CardSuit.Hearts),
                                new Card(CardType.Seven, CardSuit.Spades),
                                new Card(CardType.Eight, CardSuit.Clubs),
                                new Card(CardType.Eight, CardSuit.Diamonds),
                                new Card(CardType.Eight, CardSuit.Hearts),
                                new Card(CardType.Eight, CardSuit.Spades),
                                new Card(CardType.Nine, CardSuit.Clubs),
                                new Card(CardType.Nine, CardSuit.Diamonds),
                                new Card(CardType.Nine, CardSuit.Hearts),
                                new Card(CardType.Nine, CardSuit.Spades),
                                new Card(CardType.Ten, CardSuit.Clubs),
                                new Card(CardType.Ten, CardSuit.Diamonds),
                                new Card(CardType.Ten, CardSuit.Hearts),
                                new Card(CardType.Ten, CardSuit.Spades),
                                new Card(CardType.Jack, CardSuit.Clubs),
                                new Card(CardType.Jack, CardSuit.Diamonds),
                                new Card(CardType.Jack, CardSuit.Hearts),
                                new Card(CardType.Jack, CardSuit.Spades),
                                new Card(CardType.Queen, CardSuit.Clubs),
                                new Card(CardType.Queen, CardSuit.Diamonds),
                                new Card(CardType.Queen, CardSuit.Hearts),
                                new Card(CardType.Queen, CardSuit.Spades),
                                new Card(CardType.King, CardSuit.Clubs),
                                new Card(CardType.King, CardSuit.Diamonds),
                                new Card(CardType.King, CardSuit.Hearts),
                                new Card(CardType.King, CardSuit.Spades),
                                new Card(CardType.Ace, CardSuit.Clubs),
                                new Card(CardType.Ace, CardSuit.Diamonds),
                                new Card(CardType.Ace, CardSuit.Hearts),
                                new Card(CardType.Ace, CardSuit.Spades)
                            };
        }
 
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

        public void Add(IEnumerable<Card> cardsCollection)
        {
            foreach (var card in cardsCollection)
            {
                this.cards.Add(card);
            }
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
                this.cards = this.OrderBy(x => x, CardComparer.AllTrumps).ToList();
            }

            if (contract == ContractType.NoTrumps)
            {
                this.cards = this.OrderBy(x => x, CardComparer.NoTrumps).ToList();
            }

            if (contract == ContractType.Spades)
            {
                this.cards = this.OrderBy(x => x, CardComparer.Spades).ToList();
            }

            if (contract == ContractType.Hearts)
            {
                this.cards = this.OrderBy(x => x, CardComparer.Hearts).ToList();
            }

            if (contract == ContractType.Diamonds)
            {
                this.cards = this.OrderBy(x => x, CardComparer.Diamonds).ToList();
            }

            if (contract == ContractType.Clubs)
            {
                this.cards = this.OrderBy(x => x, CardComparer.Clubs).ToList();
            }
        }
    }
}
