namespace JustBelot.Common
{
    using System;

    public class BidEventArgs : EventArgs
    {
        public BidEventArgs(PlayerPosition position, BidType bid)
        {
            this.Position = position;
            this.Bid = bid;
        }

        public PlayerPosition Position { get; set; }

        public BidType Bid { get; set; }
    }
}
