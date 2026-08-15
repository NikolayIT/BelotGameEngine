namespace BelotV2
{
    /// <summary>
    /// The computer player: an <see cref="IPlayer"/> that delegates to the reverse-engineered
    /// <see cref="BiddingAi"/> (bidding, ported from choosegame @0x4778CC) and
    /// <see cref="OriginalPlayAi"/> (card play, transcribed from player2BeforePlay @0x46F5C0).
    /// </summary>
    public sealed class AiPlayer : IPlayer
    {
        private readonly DelphiRandom rng;

        public AiPlayer(string name, DelphiRandom rng)
        {
            this.Name = name;
            this.rng = rng;
        }

        public string Name { get; }

        public BidType GetBid(BidContext context) => BiddingAi.ChooseBid(context, this.rng);

        public Card PlayCard(PlayContext context) => OriginalPlayAdapter.Play(context, this.rng);
    }
}
