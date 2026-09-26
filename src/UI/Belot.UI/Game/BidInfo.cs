namespace Belot.UI.Game
{
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>A bid, and the contract after it.</summary>
    public sealed record BidInfo(PlayerPosition Seat, BidType Bid, bool IsAuto, PlayerPosition ContractPlayer, BidType ContractType) : GameEvent;
}
