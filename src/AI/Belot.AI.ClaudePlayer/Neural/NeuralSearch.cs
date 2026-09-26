namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
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
            for (var i = 0; i < deals; i++)
            {
                var state = this.knowledge.Root;
                this.sampler.Sample(ref state, random);
                var world = deal;
                for (var seat = 0; seat < 4; seat++)
                {
                    world.Play.Hands[seat] = state.Hands[seat];
                }

                this.Declarations(ref world, in state, random);
                for (var rest = legal; rest != 0; rest &= rest - 1)
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
                }
            }

            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                cardValues[card] = (float)(this.sums[card] / deals);
            }

            return true;
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
