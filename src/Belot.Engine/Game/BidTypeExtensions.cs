namespace Belot.Engine.Game
{
    using System;
    using System.Runtime.CompilerServices;

    using Belot.Engine.Cards;

    public static class BidTypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CardSuit ToCardSuit(this BidType bidType) =>
            bidType.HasFlag(BidType.Clubs) ? CardSuit.Club :
            bidType.HasFlag(BidType.Diamonds) ? CardSuit.Diamond :
            bidType.HasFlag(BidType.Hearts) ? CardSuit.Heart :
            bidType.HasFlag(BidType.Spades) ? CardSuit.Spade : throw new ArgumentException();
    }
}
