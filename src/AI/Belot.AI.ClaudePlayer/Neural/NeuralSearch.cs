namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Diagnostics;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Players;

    /// <summary>
    /// Values the legal cards by playing them out: it deals the unseen cards many times,
    /// consistently with what the play has shown (<see cref="RoundKnowledge"/>,
    /// <see cref="WorldSampler"/>, as ClaudePlayerIsmcts deals them), and in each deal plays
    /// every legal card and the rest of the deal with the networks deciding for every seat from
    /// what that seat can see. A card's value is its team's game points minus the other team's,
    /// averaged over the deals; every card is tried on the same deals, so the luck of the deals
    /// cancels out of their differences. Unlike ClaudePlayerIsmcts's greedy rollouts, nobody in
    /// the playouts sees the others' cards.
    /// </summary>
    internal sealed class NeuralSearch
    {
        private readonly RoundKnowledge knowledge = new RoundKnowledge();
        private readonly WorldSampler sampler = new WorldSampler();
        private readonly DeclaredAnnounce[] announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
        private readonly float[] values = new float[FeatureEncoder.CardOutputs];
        private readonly double[] sums = new double[FeatureEncoder.CardOutputs];
        private readonly int[] counts = new int[FeatureEncoder.CardOutputs];
        private readonly float[] prior = new float[FeatureEncoder.CardOutputs];

        /// <summary>
        /// Gets or sets how many deals the card network's own value counts as: it is averaged in
        /// with the playouts, which steadies a small number of deals.
        /// </summary>
        public double PriorDeals { get; set; }

        /// <summary>
        /// Gets or sets how far below the best, in game points, a card may fall after half the
        /// deals and still be played out in the rest (the others keep their first-half average);
        /// 0 plays every card in every deal.
        /// </summary>
        public double PruneMargin { get; set; }

        /// <summary>
        /// Gets or sets a time budget in milliseconds (0 = none): no new deal is started once it
        /// is spent, so a slow device plays fewer deals (at least <see cref="MinimumDeals"/>).
        /// </summary>
        public int TimeLimitMilliseconds { get; set; }

        public int MinimumDeals { get; set; } = 8;

        /// <summary>
        /// Fills the value of every legal card (indexed by card) from the given number of deals.
        /// </summary>
        /// <returns>False when the context cannot be modelled.</returns>
        public bool Evaluate(
            PlayerPlayCardContext context,
            in NeuralDeal deal,
            uint legal,
            int deals,
            NeuralEvaluator evaluator,
            BelotSimulator simulator,
            Random random,
            float[] cardValues)
        {
            // The deal gave the simulator its contract; the knowledge replays the play with it.
            if (!this.knowledge.Build(context, simulator, usePlayInference: true) || !this.sampler.Configure(this.knowledge))
            {
                return false;
            }

            var team = deal.Play.Turn & 1;
            Array.Clear(this.sums);
            Array.Clear(this.counts);
            if (this.PriorDeals > 0)
            {
                evaluator.EvaluateCards(in deal, legal, this.prior);
            }

            var playing = legal;
            var start = Stopwatch.GetTimestamp();
            var limit = (long)this.TimeLimitMilliseconds * Stopwatch.Frequency / 1000L;
            for (var i = 0; i < deals; i++)
            {
                if (this.TimeLimitMilliseconds > 0 && i >= this.MinimumDeals && Stopwatch.GetTimestamp() - start > limit)
                {
                    break;
                }

                if (i == deals / 2 && this.PruneMargin > 0)
                {
                    playing = this.Contenders(legal, cardValues);
                }

                var state = this.knowledge.Root;
                this.sampler.Sample(ref state, random);
                var world = deal;
                for (var seat = 0; seat < 4; seat++)
                {
                    world.Play.Hands[seat] = state.Hands[seat];
                }

                this.Declarations(ref world, in state, random);
                for (var rest = playing; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var copy = world;
                    copy.PlayCard(simulator, card, legal);
                    while (!copy.IsFinished)
                    {
                        copy.DeclareIfFirstCard();
                        var moves = simulator.LegalMoves(in copy.Play);
                        var move = (moves & (moves - 1)) == 0
                            ? BitOperations.TrailingZeroCount(moves)
                            : evaluator.BestCard(in copy, moves, this.values);
                        copy.PlayCard(simulator, move, moves);
                    }

                    copy.Score(simulator, out var southNorth, out var eastWest, out _);
                    this.sums[card] += team == 0 ? southNorth - eastWest : eastWest - southNorth;
                    this.counts[card]++;
                }
            }

            this.Averages(legal, cardValues);
            return true;
        }

        // Each card's average so far, the network's value counted as PriorDeals deals.
        private void Averages(uint legal, float[] cardValues)
        {
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var weight = this.PriorDeals > 0 ? this.PriorDeals : 0;
                var total = this.sums[card] + (weight * this.prior[card]);
                cardValues[card] = (float)(total / Math.Max(1e-9, this.counts[card] + weight));
            }
        }

        // The cards within PruneMargin of the best so far.
        private uint Contenders(uint legal, float[] cardValues)
        {
            this.Averages(legal, cardValues);
            var best = float.NegativeInfinity;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                best = Math.Max(best, cardValues[BitOperations.TrailingZeroCount(rest)]);
            }

            var contenders = 0u;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                if (cardValues[card] >= best - this.PruneMargin)
                {
                    contenders |= 1u << card;
                }
            }

            return contenders;
        }

        // The combinations of this deal: those declared (hidden ranks drawn at random), and in
        // the first trick those the seats still to declare hold in it, which they declare when
        // they play.
        private void Declarations(ref NeuralDeal world, in SimState state, Random random)
        {
            var count = this.knowledge.AnnounceCount;
            Array.Copy(this.knowledge.Announces, this.announces, count);
            for (var seats = this.knowledge.SeatsToDeclare; seats != 0; seats &= seats - 1)
            {
                var seat = BitOperations.TrailingZeroCount(seats);
                var start = count;
                count = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat], seat, this.announces, count);
                world.ToDeclare[seat] = 0;
                for (var i = start; i < count; i++)
                {
                    world.ToDeclare[seat] |= NeuralDeal.DeclarationBit(this.announces[i].Type);
                }
            }

            AnnounceScorer.ResolveHiddenRanks(this.announces, count, random);
            AnnounceScorer.GetPoints(this.announces, count, out world.SouthNorthAnnounces, out world.EastWestAnnounces);
        }
    }
}
