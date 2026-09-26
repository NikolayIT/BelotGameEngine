namespace Belot.UI.Game
{
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    /// <summary>A card was played; <see cref="IsAuto"/> when it was the seat's only legal card.</summary>
    public sealed record CardInfo(PlayerPosition Seat, Card Card, bool Belote, int TrickNumber, int IndexInTrick, bool IsAuto) : GameEvent;
}
