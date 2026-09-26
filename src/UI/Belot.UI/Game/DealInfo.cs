namespace Belot.UI.Game
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    /// <summary>A new deal: the person's first five cards.</summary>
    public sealed record DealInfo(
        int RoundNumber,
        PlayerPosition FirstToPlay,
        IReadOnlyList<Card> MyHand,
        IReadOnlyList<int> CardCounts,
        int SouthNorthPoints,
        int EastWestPoints,
        int HangingPoints)
    {
        /// <summary>Gets the dealer: the seat before the first to bid.</summary>
        public PlayerPosition Dealer => Seats.Previous(this.FirstToPlay);
    }
}
