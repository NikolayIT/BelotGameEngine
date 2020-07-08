namespace Belot.Engine.Game
{
    using Belot.Engine.Players;

    public class Bid
    {
        public Bid(PlayerPosition player, BidType type)
        {
            this.Player = player;
            this.Type = type;
        }

        public PlayerPosition Player { get; internal set; }

        public BidType Type { get; internal set; }

        public BidType CleanBidType
        {
            get
            {
                var cleanContract = this.Type;
                cleanContract &= ~BidType.Double;
                cleanContract &= ~BidType.ReDouble;
                return cleanContract;
            }
        }

        public override string ToString() => $"{this.Type} ({this.Player})";
    }
}
