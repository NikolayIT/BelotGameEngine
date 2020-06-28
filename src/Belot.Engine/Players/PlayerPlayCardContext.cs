namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    public class PlayerPlayCardContext : BasePlayerContext
    {
        public BidType Contract { get; set; }

        public IList<Announce> Announces { get; set; }

        public IList<Card> AvailableCardsToPlay { get; set; }
    }
}
