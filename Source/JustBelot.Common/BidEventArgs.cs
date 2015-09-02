namespace JustBelot.Common
{
    using System;

    public class BidEventArgs : EventArgs
    {
        public BidEventArgs(PlayerPosition position, BidType bid, Contract currentContract)
        {
            this.Position = position;
            this.Bid = bid;
            this.CurrentContract = currentContract;
        }

        public PlayerPosition Position { get; set; }

        public BidType Bid { get; set; }

        public Contract CurrentContract { get; set; }
    }
}
