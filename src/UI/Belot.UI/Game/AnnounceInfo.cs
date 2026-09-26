namespace Belot.UI.Game
{
    using System.Collections.Generic;

    using Belot.Engine.Players;

    /// <summary>A seat declared its combinations (none, possibly) in the first trick.</summary>
    public sealed record AnnounceInfo(PlayerPosition Seat, IReadOnlyList<Combination> Combinations) : GameEvent;
}
