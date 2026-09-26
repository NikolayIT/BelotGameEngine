namespace Belot.UI.Game
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    /// <summary>A trick is taken off the table.</summary>
    public sealed record TrickInfo(int TrickNumber, IReadOnlyList<PlayedCard> Cards, PlayerPosition Winner, bool IsLastOfDeal) : GameEvent
    {
        public Card? CardOf(PlayerPosition seat) => this.Cards.FirstOrDefault(c => c.Seat == seat)?.Card;
    }
}
