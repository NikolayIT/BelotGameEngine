namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;

    using JustBelot.Common.Extensions;

    public static class CardComparer
    {
        private static readonly IComparer<Card> SortCardsForAllTrumpsComparerInstance = new SortCardsForAllTrumpsComparer();
        private static readonly IComparer<Card> SortCardsForNoTrumpsComparerInstance = new SortCardsForNoTrumpsComparer();
        private static readonly IComparer<Card> SortCardsForSpadesComparerInstance = new SortCardsForTrumpComparer(CardSuit.Spades);
        private static readonly IComparer<Card> SortCardsForHeartsComparerInstance = new SortCardsForTrumpComparer(CardSuit.Hearts);
        private static readonly IComparer<Card> SortCardsForDiamondsComparerInstance = new SortCardsForTrumpComparer(CardSuit.Diamonds);
        private static readonly IComparer<Card> SortCardsForClubsComparerInstance = new SortCardsForTrumpComparer(CardSuit.Clubs);

        public static IComparer<Card> AllTrumps
        {
            get
            {
                return SortCardsForAllTrumpsComparerInstance;
            }
        }

        public static IComparer<Card> NoTrumps
        {
            get
            {
                return SortCardsForNoTrumpsComparerInstance;
            }
        }

        public static IComparer<Card> Spades
        {
            get
            {
                return SortCardsForSpadesComparerInstance;
            }
        }

        public static IComparer<Card> Hearts
        {
            get
            {
                return SortCardsForHeartsComparerInstance;
            }
        }

        public static IComparer<Card> Diamonds
        {
            get
            {
                return SortCardsForDiamondsComparerInstance;
            }
        }

        public static IComparer<Card> Clubs
        {
            get
            {
                return SortCardsForClubsComparerInstance;
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

        private class SortCardsForAllTrumpsComparer : IComparer<Card>
        {
            public int Compare(Card x, Card y)
            {
                var firstValue = CardSuitOrderValue(x.Suit);
                firstValue += x.Type.GetOrderForAllTrumps();

                var secondValue = CardSuitOrderValue(y.Suit);
                secondValue += y.Type.GetOrderForAllTrumps();

                return -firstValue.CompareTo(secondValue);
            }
        }

        private class SortCardsForNoTrumpsComparer : IComparer<Card>
        {
            public int Compare(Card x, Card y)
            {
                var firstValue = CardSuitOrderValue(x.Suit);
                firstValue += x.Type.GetOrderForNoTrumps();

                var secondValue = CardSuitOrderValue(y.Suit);
                secondValue += y.Type.GetOrderForNoTrumps();

                return -firstValue.CompareTo(secondValue);
            }
        }

        private class SortCardsForTrumpComparer : IComparer<Card>
        {
            private CardSuit suit;

            public SortCardsForTrumpComparer(CardSuit suit)
            {
                this.suit = suit;
            }

            public int Compare(Card x, Card y)
            {
                int firstValue = 0;
                int secondValue = 0;

                if (x.Suit == this.suit)
                {
                    firstValue += 100;
                    firstValue += x.Type.GetOrderForAllTrumps();
                }
                else
                {
                    firstValue += CardSuitOrderValue(x.Suit);
                    firstValue += x.Type.GetOrderForNoTrumps();
                }

                if (y.Suit == this.suit)
                {
                    secondValue += 100;
                    secondValue += x.Type.GetOrderForAllTrumps();
                }
                else
                {
                    secondValue += CardSuitOrderValue(y.Suit);
                    secondValue += x.Type.GetOrderForNoTrumps();
                }

                return -firstValue.CompareTo(secondValue);
            }
        }
    }
}
