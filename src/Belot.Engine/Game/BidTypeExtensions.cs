namespace Belot.Engine.Game
{
    using System;

    using Belot.Engine.Cards;

    public static class BidTypeExtensions
    {
        public static CardSuit ToCardSuit(this BidType announceType)
        {
            if (announceType.HasFlag(BidType.Clubs))
            {
                return CardSuit.Club;
            }

            if (announceType.HasFlag(BidType.Diamonds))
            {
                return CardSuit.Diamond;
            }

            if (announceType.HasFlag(BidType.Hearts))
            {
                return CardSuit.Heart;
            }

            if (announceType.HasFlag(BidType.Spades))
            {
                return CardSuit.Spade;
            }

            throw new ArgumentException("Invalid announce type.", nameof(announceType));
        }
    }
}
