namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Game;

    public abstract class PlayerGetBidContext : BasePlayerContext
    {
        public BidType Contract { get; set; }

        public IList<BidType> AvailableBids { get; set; }
    }
}
