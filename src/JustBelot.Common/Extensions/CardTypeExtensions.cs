namespace JustBelot.Common.Extensions
{
    using System;

    public static class CardTypeExtensions
    {
        public static int GetOrderForAllTrumps(this CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Jack:
                    return 7;
                case CardType.Nine:
                    return 6;
                case CardType.Ace:
                    return 5;
                case CardType.Ten:
                    return 4;
                case CardType.King:
                    return 3;
                case CardType.Queen:
                    return 2;
                case CardType.Eight:
                    return 1;
                case CardType.Seven:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException("cardType");
            }
        }

        public static int GetOrderForNoTrumps(this CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Ace:
                    return 7;
                case CardType.Ten:
                    return 6;
                case CardType.King:
                    return 5;
                case CardType.Queen:
                    return 4;
                case CardType.Jack:
                    return 3;
                case CardType.Nine:
                    return 2;
                case CardType.Eight:
                    return 1;
                case CardType.Seven:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException("cardType");
            }
        }
    }
}
