namespace JustBelot.Common
{
    using System;

    public class BidEventArgs : EventArgs
    {
        public PlayerPosition Position { get; set; }

        public BidType Bid { get; set; }

        public BidEventArgs(PlayerPosition position, BidType bid)
        {
            this.Position = position;
            this.Bid = bid;
        }
    }
}
