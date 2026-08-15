namespace BelotV2
{
    /// <summary>A baseline opponent: always passes the auction and plays a random legal card.
    /// Used only to sanity-check that the reconstructed AI plays meaningfully better than chance.</summary>
    public sealed class RandomPlayer : IPlayer
    {
        private readonly DelphiRandom rng;

        public RandomPlayer(string name, DelphiRandom rng)
        {
            this.Name = name;
            this.rng = rng;
        }

        public string Name { get; }

        public BidType GetBid(BidContext context) => BidType.Pass;

        public Card PlayCard(PlayContext context) => context.Legal[this.rng.Next(context.Legal.Count)];
    }
}
