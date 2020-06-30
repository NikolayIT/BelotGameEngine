namespace Belot.Engine.Game
{
    using Belot.Engine.Players;

    public class Bid
    {
        public Bid(PlayerPosition playerPosition, BidType bidType)
        {
            this.PlayerPosition = playerPosition;
            this.BidType = bidType;
        }

        public PlayerPosition PlayerPosition { get; }

        public BidType BidType { get; }
    }
}
