namespace Belot.Engine.Cards
{
    using System;
    using System.Runtime.CompilerServices;

    using Belot.Engine.Game;

    public static class CardExtensions
    {
        public static string ToFriendlyString(this CardSuit cardSuit)
        {
            switch (cardSuit)
            {
                case CardSuit.Club: return "\u2663"; // ♣
                case CardSuit.Diamond: return "\u2666"; // ♦
                case CardSuit.Heart: return "\u2665"; // ♥
                case CardSuit.Spade: return "\u2660"; // ♠
                default: throw new ArgumentException("cardSuit");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BidType ToBidType(this CardSuit cardSuit) =>
            cardSuit == CardSuit.Club ? BidType.Clubs :
            cardSuit == CardSuit.Diamond ? BidType.Diamonds :
            cardSuit == CardSuit.Heart ? BidType.Hearts :
            cardSuit == CardSuit.Spade ? BidType.Spades : BidType.Pass;

        public static string ToFriendlyString(this CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Seven:
                    return "7";
                case CardType.Eight:
                    return "8";
                case CardType.Nine:
                    return "9";
                case CardType.Ten:
                    return "10";
                case CardType.Jack:
                    return "J";
                case CardType.Queen:
                    return "Q";
                case CardType.King:
                    return "K";
                case CardType.Ace:
                    return "A";
                default:
                    throw new ArgumentException("cardType");
            }
        }
    }
}
