namespace Belot.Engine.Cards
{
    using System;
    using System.Runtime.CompilerServices;

    using Belot.Engine.Game;

    public static class CardExtensions
    {
        public static string ToFriendlyString(this CardSuit cardSuit)
        {
            return cardSuit switch
                {
                    CardSuit.Club => "\u2663", // ♣
                    CardSuit.Diamond => "\u2666", // ♦
                    CardSuit.Heart => "\u2665", // ♥
                    CardSuit.Spade => "\u2660", // ♠
                    _ => throw new ArgumentException("Invalid card suit.", nameof(cardSuit)),
                };
        }

        public static string ToFriendlyString(this CardType cardType)
        {
            return cardType switch
                {
                    CardType.Seven => "7",
                    CardType.Eight => "8",
                    CardType.Nine => "9",
                    CardType.Ten => "10",
                    CardType.Jack => "J",
                    CardType.Queen => "Q",
                    CardType.King => "K",
                    CardType.Ace => "A",
                    _ => throw new ArgumentException("Invalid card type.", nameof(cardType)),
                };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BidType ToBidType(this CardSuit cardSuit) =>
            cardSuit == CardSuit.Club ? BidType.Clubs :
            cardSuit == CardSuit.Diamond ? BidType.Diamonds :
            cardSuit == CardSuit.Heart ? BidType.Hearts :
            cardSuit == CardSuit.Spade ? BidType.Spades : BidType.Pass;
    }
}
