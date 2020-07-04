namespace Belot.Engine.Players
{
    using Belot.Engine.Game;

    public class PlayerGetBidContext : BasePlayerContext
    {
        public BidType AvailableBids { get; set; }
    }
}
