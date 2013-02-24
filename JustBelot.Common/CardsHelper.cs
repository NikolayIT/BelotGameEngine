namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class CardsHelper
    {
        public static IEnumerable<Card> GetFullCardDeck()
        {
            var cards = new List<Card>();
            foreach (CardSuit cardSuit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardType in Enum.GetValues(typeof(CardType)))
                {
                    cards.Add(new Card(cardType, cardSuit));
                }
            }

            return cards;
        }

        public static string CardTypeToString(CardType cardType)
        {
            string cardTypeAsString = null;
            switch (cardType)
            {
                case CardType.Ace:
                    cardTypeAsString = "A";
                    break;
                case CardType.King:
                    cardTypeAsString = "K";
                    break;
                case CardType.Queen:
                    cardTypeAsString = "Q";
                    break;
                case CardType.Jack:
                    cardTypeAsString = "J";
                    break;
                case CardType.Ten:
                    cardTypeAsString = "10";
                    break;
                case CardType.Nine:
                    cardTypeAsString = "9";
                    break;
                case CardType.Eight:
                    cardTypeAsString = "8";
                    break;
                case CardType.Seven:
                    cardTypeAsString = "7";
                    break;
            }
            return cardTypeAsString;
        }

        public static string CardSuitToString(CardSuit cardSuit)
        {
            string cardSuitAsString = null;
            switch (cardSuit)
            {
                case CardSuit.Spades:
                    cardSuitAsString = "\u2660";
                    break;
                case CardSuit.Hearts:
                    cardSuitAsString = "\u2665";
                    break;
                case CardSuit.Diamonds:
                    cardSuitAsString = "\u2666";
                    break;
                case CardSuit.Clubs:
                    cardSuitAsString = "\u2663";
                    break;
            }
            return cardSuitAsString;
        }

        /// <summary>
        /// Returns the number of "belot" combinations (king + queen of the same suit)
        /// </summary>
        public static int NumberOfQueenAndKingCombinations(IEnumerable<Card> cards)
        {
            var numberOfCombinations = 0;
            foreach (CardSuit cardSuit in Enum.GetValues(typeof(CardSuit)))
            {
                var queen = false;
                var king = false;
                foreach (var card in cards)
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

        public static IList<Card> SortForAllTrump(IList<Card> cards)
        {
            return cards.OrderByDescending(card =>
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

        public static IList<Card> SortForNoTrump(IList<Card> cards)
        {
            return cards.OrderByDescending(card =>
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

        public static IList<Card> SortForSuit(IList<Card> cards, CardSuit suit)
        {
            return cards.OrderByDescending(card =>
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
    }
}
