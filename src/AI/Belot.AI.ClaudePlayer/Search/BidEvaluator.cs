namespace Belot.AI.ClaudePlayer.Search
{
    using System;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// Values the contracts a bidder could end up in by Monte Carlo: it deals the unseen cards
    /// (the bidder's last three included) many times, plays each deal out in every candidate
    /// contract with the simulator's greedy policy, and averages the bidder's team's game
    /// points minus the other team's. Every candidate sees the same deals, so their differences
    /// are measured far more precisely than their levels. The other players' first five cards
    /// are dealt to fit their bids so far (<see cref="BidModel"/>).
    /// </summary>
    internal sealed class BidEvaluator
    {
        private const int MaxAuctionAttempts = 50;

        private readonly BelotSimulator simulator = new BelotSimulator();
        private readonly BidModel bidModel = new BidModel();
        private readonly int[] pool = new int[32];
        private readonly DeclaredAnnounce[] announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];

        /// <summary>
        /// Averages, over the given number of deals, each candidate contract's result for the
        /// bidder's team.
        /// </summary>
        /// <param name="context">The bidder's context.</param>
        /// <param name="contracts">The candidate contracts (doubling flags included).</param>
        /// <param name="declarers">Each candidate's declarer seat.</param>
        /// <param name="count">How many candidates there are.</param>
        /// <param name="deals">How many deals to play.</param>
        /// <param name="hangingPoints">The points hanging from earlier deals.</param>
        /// <param name="random">The random source.</param>
        /// <param name="values">Receives each candidate's average game points difference.</param>
        public void Evaluate(
            PlayerGetBidContext context,
            BidType[] contracts,
            int[] declarers,
            int count,
            int deals,
            int hangingPoints,
            Random random,
            double[] values)
        {
            var me = context.MyPosition.Index();
            var bidderTeam = me & 1;
            var bidderCards = 0u;
            foreach (var card in context.MyCards)
            {
                bidderCards |= 1u << card.GetHashCode();
            }

            var poolCount = 0;
            for (var card = 0; card < 32; card++)
            {
                if ((bidderCards & (1u << card)) == 0)
                {
                    this.pool[poolCount++] = card;
                }
            }

            Array.Clear(values, 0, count);
            this.bidModel.Read(context.Bids, context.FirstToPlayInTheRound);
            var biddingSeats = this.bidModel.InformativeSeats & ~(1 << me);
            var first = context.FirstToPlayInTheRound.Index();
            var state = default(SimState);
            for (var deal = 0; deal < deals; deal++)
            {
                for (var i = poolCount - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (this.pool[i], this.pool[j]) = (this.pool[j], this.pool[i]);
                }

                // The others' first five cards (fitting their bids), then three more for everybody.
                var next = 0;
                for (var seat = 0; seat < 4; seat++)
                {
                    state.Hands[seat] = seat == me ? bidderCards : this.DealFive(seat, ((biddingSeats >> seat) & 1) != 0, ref next, poolCount, random);
                }

                for (var seat = 0; seat < 4; seat++)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        state.Hands[seat] |= 1u << this.pool[next++];
                    }
                }

                var announceCount = 0;
                for (var seat = 0; seat < 4; seat++)
                {
                    announceCount = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat], seat, this.announces, announceCount);
                }

                AnnounceScorer.GetPoints(this.announces, announceCount, out var southNorthAnnounces, out var eastWestAnnounces);
                for (var candidate = 0; candidate < count; candidate++)
                {
                    var contract = contracts[candidate];
                    var noTrumps = contract.HasFlag(BidType.NoTrumps);
                    this.simulator.SetContract(contract, declarers[candidate], hangingPoints);
                    var play = state;
                    play.Turn = first;
                    while (play.TricksPlayed < 8)
                    {
                        var legal = this.simulator.LegalMoves(in play);
                        this.simulator.Play(ref play, this.simulator.ChooseRolloutMove(in play, legal), legal);
                    }

                    this.simulator.Score(
                        in play,
                        noTrumps ? 0 : southNorthAnnounces,
                        noTrumps ? 0 : eastWestAnnounces,
                        out var southNorth,
                        out var eastWest,
                        out _);
                    values[candidate] += bidderTeam == 0 ? southNorth - eastWest : eastWest - southNorth;
                }
            }

            for (var candidate = 0; candidate < count; candidate++)
            {
                values[candidate] /= deals;
            }
        }

        // Takes five cards from the pool's rest (from next on), retrying for five that fit the
        // seat's bids; the pool is shuffled, so the rest stays random.
        private uint DealFive(int seat, bool bids, ref int next, int poolCount, Random random)
        {
            var five = 0u;
            for (var attempt = 0; attempt < (bids ? MaxAuctionAttempts : 1); attempt++)
            {
                for (var i = 0; i < 5; i++)
                {
                    var j = next + i + random.Next(poolCount - next - i);
                    (this.pool[next + i], this.pool[j]) = (this.pool[j], this.pool[next + i]);
                }

                five = 0u;
                for (var i = 0; i < 5; i++)
                {
                    five |= 1u << this.pool[next + i];
                }

                if (!bids || this.bidModel.Fits(seat, five))
                {
                    break;
                }
            }

            next += 5;
            return five;
        }
    }
}
