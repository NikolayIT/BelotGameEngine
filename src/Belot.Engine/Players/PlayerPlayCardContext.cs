namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    public class PlayerPlayCardContext
    {
        public PlayerPosition FirstToPlay { get; set; }

        public PlayerPosition MyPosition { get; set; }

        public CardCollection MyCards { get; }

        public IList<Bid> Bids { get; set; }

        public BidType Contract { get; set; }

        public IList<Announce> Announces { get; set; }

        public IList<Card> AvailableCardsToPlay { get; set; }
    }
}
