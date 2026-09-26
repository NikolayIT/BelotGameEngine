namespace Belot.UI.Game
{
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public sealed record PlayedCard(PlayerPosition Seat, Card Card, bool Belote);
}
