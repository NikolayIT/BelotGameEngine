namespace Belot.UI.Game
{
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>A declared combination: its card shows only for the person's own before the deal is over.</summary>
    public sealed record Combination(PlayerPosition Seat, AnnounceType Type, Card? Card, bool? IsScored)
    {
        public int Value => new Announce(this.Type, this.Card).Value;
    }
}
