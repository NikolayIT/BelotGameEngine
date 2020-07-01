namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    public class PlayerPlayCardContext : BasePlayerContext
    {
        public IEnumerable<Announce> Announces { get; set; }

        public IEnumerable<PlayCardAction> PreviousActions { get; set; }

        public IEnumerable<Card> AvailableCardsToPlay { get; set; }
    }
}
