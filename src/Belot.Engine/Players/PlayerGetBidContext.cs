namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    public abstract class PlayerGetBidContext
    {
        public PlayerPosition FirstToPlay { get; set; }

        public PlayerPosition MyPosition { get; set; }

        public CardCollection MyCards { get; }

        public IList<Bid> PreviousBids { get; set; }

        public BidType CurrentContract { get; set; }

        public IList<BidType> AvailableBids { get; set; }
    }
}
