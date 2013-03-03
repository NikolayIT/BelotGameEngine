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

        public IEnumerable<Card> GetAllowedCards(Contract contract, IList<Card> currentTrickCards)
        {
            if (currentTrickCards == null || !currentTrickCards.Any())
            {
                // The player is first and can play any card
                return this;
            }

            var firstCard = currentTrickCards[0];

            // For all trumps the player should play bigger card from the same suit if available.
            // If bigger card is not available, the player should play any card of the same suit if available.
            if (contract.Type == ContractType.AllTrumps)
            {
                if (this.Any(x => x.Suit == firstCard.Suit))
                {
                    if (this.Any(card => card.Suit == firstCard.Suit && card.Type.GetOrderForAllTrumps() > firstCard.Type.GetOrderForAllTrumps()))
                    {
                        // Has bigger card(s)
                        return this.Where(card => card.Suit == firstCard.Suit && card.Type.GetOrderForAllTrumps() > firstCard.Type.GetOrderForAllTrumps());
                    }

                    // Any other card from the same suit
                    return this.Where(card => card.Suit == firstCard.Suit);
                }

                // No card of the same suit available
                return this;
            }

            // For no trumps the player should play card from the same suit if available.
            if (contract.Type == ContractType.NoTrumps)
            {
                if (this.Any(x => x.Suit == firstCard.Suit))
                {
                    return this.Where(x => x.Suit == firstCard.Suit);
                }
                
                // No card of the same suit available
                return this;
            }

            // Playing Clubs, Diamonds, Hearts or Spades
            if (firstCard.Suit == contract.Type.ToCardSuit())
            {
                // Trump card played
                if (this.Any(x => x.Suit == firstCard.Suit))
                {
                    if (this.Any(card => card.Suit == firstCard.Suit && card.Type.GetOrderForAllTrumps() > firstCard.Type.GetOrderForAllTrumps()))
                    {
                        // Has bigger card(s)
                        return this.Where(card => card.Suit == firstCard.Suit && card.Type.GetOrderForAllTrumps() > firstCard.Type.GetOrderForAllTrumps());
                    }

                    // Any other card from the same suit
                    return this.Where(card => card.Suit == firstCard.Suit);
                }

                // No card of the same suit available
                return this;
            }
            else
            {
                // Non-trump card played
                if (this.Any(x => x.Suit == firstCard.Suit))
                {
                    // If the player has the same card suit, he should play a card from the suit
                    return this.Where(x => x.Suit == firstCard.Suit);
                }
                else
                {
                    // The player has not a card with the same suit
                    if (this.Any(x => x.Suit == contract.Type.ToCardSuit()))
                    {
                        var currentPlayerTeamIsCurrentTrickWinner = false;
                        if (currentTrickCards.Count > 1)
                        {
                            // The teammate played card
                            Card bestCard;
                            if (currentTrickCards.Any(x => x.Suit == contract.Type.ToCardSuit()))
                            {
                                // Someone played trump
                                bestCard = currentTrickCards.Where(x => x.Suit == contract.Type.ToCardSuit()).OrderByDescending(x => x.Type.GetOrderForAllTrumps()).First();
                            }
                            else
                            {
                                // No one played trump
                                bestCard = currentTrickCards.OrderByDescending(x => x.Type.GetOrderForNoTrumps()).First();
                            }

                            if (currentTrickCards[currentTrickCards.Count - 2] == bestCard)
                            {
                                // The teammate has the best card in current trick
                                currentPlayerTeamIsCurrentTrickWinner = true;
                            }
                        }

                        // The player has trump card
                        if (currentPlayerTeamIsCurrentTrickWinner)
                        {
                            // The current trick winner is the player or his teammate.
                            // The player is not obligatory to play any trump
                            return this;
                        }
                        else
                        {
                            // The current trick winner is the rivals of the current player
                            if (currentTrickCards.Any(x => x.Suit == contract.Type.ToCardSuit()))
                            {
                                // Someone of the rivals has played trump card and is winning the trick
                                var biggestTrumpCard = currentTrickCards.OrderByDescending(x => x.Type.GetOrderForAllTrumps()).First();
                                if (this.Any(x => x.Suit == contract.Type.ToCardSuit() && x.Type.GetOrderForAllTrumps() > biggestTrumpCard.Type.GetOrderForAllTrumps()))
                                {
                                    // The player has bigger trump card(s) and should play one of them
                                    return this.Where(x => x.Suit == contract.Type.ToCardSuit() && x.Type.GetOrderForAllTrumps() > biggestTrumpCard.Type.GetOrderForAllTrumps());
                                }
                                else
                                {
                                    // The player hasn't any bigger trump card so he can play any card
                                    return this;
                                }
                            }
                            else
                            {
                                // No one played trump card, but the player should play one of them
                                return this.Where(x => x.Suit == contract.Type.ToCardSuit());
                            }
                        }
                    }
                    else
                    {
                        // The player has not any trump card or card from the played suit
                        return this;
                    }
                }
            }
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

        // TODO: Define comparators
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
            this.cards = this.cards.OrderByDescending(card => card.Type.GetOrderForAllTrumps() + CardSuitOrderValue(card.Suit)).ToList();
        }

        private void SortForNoTrump()
        {
            this.cards = this.cards.OrderByDescending(card => card.Type.GetOrderForNoTrumps() + CardSuitOrderValue(card.Suit)).ToList();
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
