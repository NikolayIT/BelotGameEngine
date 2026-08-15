namespace BelotArena
{
    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Round-by-round tallies, collected by wrapping one player. A win rate on its own does not
    /// say whether a side wins by bidding better, by playing better, or because the other side
    /// never bids at all -- these do.
    /// </summary>
    public sealed class StatsObserver
    {
        private readonly Dictionary<BidType, int> contracts = new();

        public int Rounds { get; private set; }

        public int SouthNorthContracts { get; private set; }

        public int EastWestContracts { get; private set; }

        public int Doubled { get; private set; }

        public void Observe(RoundResult r)
        {
            this.Rounds++;
            BidType type = r.Contract.Type;
            if (type.HasFlag(BidType.Double) || type.HasFlag(BidType.ReDouble))
            {
                this.Doubled++;
            }

            BidType clean = type & ~(BidType.Double | BidType.ReDouble);
            this.contracts.TryGetValue(clean, out int n);
            this.contracts[clean] = n + 1;

            if (r.Contract.Player == PlayerPosition.South || r.Contract.Player == PlayerPosition.North)
            {
                this.SouthNorthContracts++;
            }
            else
            {
                this.EastWestContracts++;
            }
        }

        /// <summary>
        /// The engine returns a passed-out round before it notifies the players, so those rounds
        /// are never observed here; the caller's own round count is what reveals them.
        /// </summary>
        public void Print(long totalRounds)
        {
            if (this.Rounds == 0)
            {
                return;
            }

            long passedOut = totalRounds - this.Rounds;
            Console.WriteLine($"  rounds: {totalRounds}, of which {passedOut} passed out " +
                              $"({(double)passedOut / totalRounds:P0}) - nobody bid");
            Console.WriteLine($"  contracts declared:  S+N {this.SouthNorthContracts}   E+W {this.EastWestContracts}" +
                              $"   (doubled {this.Doubled})");
            var parts = this.contracts.OrderByDescending(kv => kv.Value)
                .Select(kv => $"{kv.Key} {kv.Value}");
            Console.WriteLine($"  by contract: {string.Join(", ", parts)}");
        }
    }

    /// <summary>Passes every callback straight through, recording the round results.</summary>
    public sealed class ObservingPlayer : IPlayer
    {
        private readonly IPlayer inner;
        private readonly StatsObserver stats;

        public ObservingPlayer(IPlayer inner, StatsObserver stats)
        {
            this.inner = inner;
            this.stats = stats;
        }

        public BidType GetBid(PlayerGetBidContext context) => this.inner.GetBid(context);

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.inner.GetAnnounces(context);

        public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.inner.PlayCard(context);

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions) => this.inner.EndOfTrick(trickActions);

        public void EndOfRound(RoundResult roundResult)
        {
            this.stats.Observe(roundResult);
            this.inner.EndOfRound(roundResult);
        }

        public void EndOfGame(GameResult gameResult) => this.inner.EndOfGame(gameResult);
    }
}
