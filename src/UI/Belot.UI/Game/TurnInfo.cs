namespace Belot.UI.Game
{
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>A seat has to decide something.</summary>
    public sealed record TurnInfo(PlayerPosition Seat, BelotDecision Decision, bool IsHuman);
}
