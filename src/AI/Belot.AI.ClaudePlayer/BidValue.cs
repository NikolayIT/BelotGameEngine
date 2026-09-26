namespace Belot.AI.ClaudePlayer
{
    using Belot.Engine.Game;

    /// <summary>
    /// A bid open to the player and what it is worth: the game points the player's team gets
    /// from the deal minus the other team's, if the player makes it (see <see cref="ClaudePlayerNeural.EvaluateBids"/>).
    /// </summary>
    public readonly struct BidValue
    {
        public BidValue(BidType bid, double value)
        {
            this.Bid = bid;
            this.Value = value;
        }

        public BidType Bid { get; }

        public double Value { get; }

        public override string ToString() => $"{this.Bid}: {this.Value:0.00}";
    }
}
