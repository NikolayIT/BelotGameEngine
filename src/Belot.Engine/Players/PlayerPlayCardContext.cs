namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    public class PlayerPlayCardContext : BasePlayerContext
    {
        public IEnumerable<Announce> Announces { get; set; }

        public IEnumerable<PlayCardAction> CurrentTrickActions { get; set; }

        public IEnumerable<PlayCardAction> RoundActions { get; set; }

        public CardCollection AvailableCardsToPlay { get; set; }
    }
}
