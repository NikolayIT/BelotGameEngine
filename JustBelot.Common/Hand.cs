namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// IList<Card> wrapper
    /// </summary>
    public class Hand : IList<Card>
    {
        private IList<Card> cards;
 
        public Hand()
        {
            this.cards = new List<Card>();
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

        public void Sort(ContractType contract)
        {
            if (contract == ContractType.AllTrumps)
            {
                this.cards = CardsHelper.SortForAllTrump(this.cards);
            }

            if (contract == ContractType.NoTrumps)
            {
                this.cards = CardsHelper.SortForNoTrump(this.cards);
            }

            if (contract == ContractType.Spades)
            {
                this.cards = CardsHelper.SortForSuit(this.cards, CardSuit.Spades);
            }

            if (contract == ContractType.Hearts)
            {
                this.cards = CardsHelper.SortForSuit(this.cards, CardSuit.Hearts);
            }

            if (contract == ContractType.Diamonds)
            {
                this.cards = CardsHelper.SortForSuit(this.cards, CardSuit.Diamonds);
            }

            if (contract == ContractType.Clubs)
            {
                this.cards = CardsHelper.SortForSuit(this.cards, CardSuit.Clubs);
            }
        }
    }
}
