namespace Belot.Engine.Cards
{
    using System;
    using System.Runtime.CompilerServices;

    using Belot.Engine.Game;

    public static class CardExtensions
    {
        private static readonly int[] NoTrumpValues = { 0, 0, 0, 10, 2, 3, 4, 11 };
        private static readonly int[] TrumpValues = { 0, 0, 14, 10, 20, 3, 4, 11 };

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetValue(this Card card, BidType contract) =>
            contract.HasFlag(BidType.AllTrumps) ? TrumpValues[(int)card.Type] :
            contract.HasFlag(BidType.NoTrumps) ? NoTrumpValues[(int)card.Type] :
            contract == BidType.Pass ? 0 :
            contract.ToCardSuit() == card.Suit ? TrumpValues[(int)card.Type] : NoTrumpValues[(int)card.Type];
    }
}
