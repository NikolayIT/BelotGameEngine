namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    public class PlayerPlayCardContext : BasePlayerContext
    {
        // TODO: Don't disclose the exact type of announce
        public IEnumerable<Announce> Announces { get; set; }

        public IEnumerable<PlayCardAction> CurrentTrickActions { get; set; }

        public IEnumerable<PlayCardAction> RoundActions { get; set; }

        public CardCollection AvailableCardsToPlay { get; set; }

        public int CurrentTrickNumber { get; set; }
    }
}
