namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Players;

    /// <summary>
    /// Enumerates remaining deals in the final two tricks, or three within a bounded world count, and solves
    /// each by partnership minimax. Equal-weight perfect-information results approximate
    /// the seat's values (PIMC); future decisions inside a world have perfect information.
    /// Declines inconsistent constraints. Unresolved announcement ranks require the optional
    /// model in which every seat declares all available combinations, as the bots do.
    /// </summary>
    internal sealed class EndgameSearch
    {
        private readonly RoundKnowledge knowledge = new RoundKnowledge();
        private readonly int[] pool = new int[9];
        private readonly int[] needs = new int[4];
        private readonly double[] sums = new double[32];
        private readonly uint[] declaredCounts = new uint[4];
        private readonly uint[] worldCounts = new uint[4];
        private readonly DeclaredAnnounce[] worldAnnounces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
        private readonly SimState[] worlds = new SimState[90];
        private readonly int[] worldSouthNorthAnnounces = new int[90];
        private readonly int[] worldEastWestAnnounces = new int[90];
        private BelotSimulator simulator;
        private int poolCount;
        private int team;
        private int southNorthAnnounces;
        private int eastWestAnnounces;
        private int worldLimit;
        private bool overflow;

        public int Worlds { get; private set; }

        /// <summary>Gets or sets whether to condition on the bots' policy of declaring every available combination.</summary>
        public bool UseDeclarations { get; set; }

        public int Tricks { get; set; } = 2;

        public int ThreeTrickWorldLimit { get; set; } = 8;

        public bool Evaluate(PlayerPlayCardContext context, in NeuralDeal deal, uint legal, BelotSimulator simulator, float[] values)
        {
            this.Worlds = 0;
            if (deal.Play.TricksPlayed < 8 - this.Tricks || !this.knowledge.Build(context, simulator, usePlayInference: true))
            {
                return false;
            }

            Array.Clear(this.declaredCounts);
            for (var i = 0; i < this.knowledge.AnnounceCount; i++)
            {
                var announce = this.knowledge.Announces[i];
                this.declaredCounts[announce.Seat] += 1u << (2 * (int)announce.Type);
                if (!this.UseDeclarations && announce.Rank < 0)
                {
                    return false;
                }
            }

            if (!this.UseDeclarations)
            {
                AnnounceScorer.GetPoints(this.knowledge.Announces, this.knowledge.AnnounceCount, out this.southNorthAnnounces, out this.eastWestAnnounces);
            }

            this.simulator = simulator;
            this.team = this.knowledge.Me & 1;
            this.worldLimit = deal.Play.TricksPlayed < 6 ? this.ThreeTrickWorldLimit : 90;
            this.overflow = false;
            Array.Clear(this.sums);
            Array.Clear(this.needs);
            var state = this.knowledge.Root;
            var unseen = ~(this.knowledge.Played | this.knowledge.MyHand);
            var needed = 0;
            for (var seat = 0; seat < 4; seat++)
            {
                if (seat == this.knowledge.Me)
                {
                    continue;
                }

                state.Hands[seat] = this.knowledge.Known[seat];
                unseen &= ~state.Hands[seat];
                this.needs[seat] = this.knowledge.HandCounts[seat] - BitOperations.PopCount(state.Hands[seat]);
                if (this.needs[seat] < 0)
                {
                    return false;
                }

                needed += this.needs[seat];
            }

            this.poolCount = BitOperations.PopCount(unseen);
            if (this.poolCount > this.pool.Length || this.poolCount != needed)
            {
                return false;
            }

            var index = 0;
            for (var rest = unseen; rest != 0; rest &= rest - 1)
            {
                this.pool[index++] = BitOperations.TrailingZeroCount(rest);
            }

            this.Enumerate(0, in state);
            if (this.Worlds == 0 || this.overflow)
            {
                this.Worlds = 0;
                return false;
            }

            for (var world = 0; world < this.Worlds; world++)
            {
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var copy = this.worlds[world];
                    simulator.Play(ref copy, card, legal);
                    this.sums[card] += Solve(in copy, simulator, this.team, this.worldSouthNorthAnnounces[world], this.worldEastWestAnnounces[world]);
                }
            }

            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                values[card] = (float)(this.sums[card] / this.Worlds);
            }

            return true;
        }

        internal static int Solve(in SimState state, BelotSimulator simulator, int team, int southNorthAnnounces, int eastWestAnnounces) =>
            Solve(in state, simulator, team, southNorthAnnounces, eastWestAnnounces, int.MinValue, int.MaxValue);

        private static int Solve(in SimState state, BelotSimulator simulator, int team, int southNorthAnnounces, int eastWestAnnounces, int alpha, int beta)
        {
            if (state.TricksPlayed == 8)
            {
                simulator.Score(in state, southNorthAnnounces, eastWestAnnounces, out var southNorth, out var eastWest, out _);
                return team == 0 ? southNorth - eastWest : eastWest - southNorth;
            }

            var maximizing = (state.Turn & 1) == team;
            var best = maximizing ? int.MinValue : int.MaxValue;
            var legal = simulator.LegalMoves(in state);
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var copy = state;
                simulator.Play(ref copy, BitOperations.TrailingZeroCount(rest), legal);
                var value = Solve(in copy, simulator, team, southNorthAnnounces, eastWestAnnounces, alpha, beta);
                if (maximizing)
                {
                    best = Math.Max(best, value);
                    alpha = Math.Max(alpha, best);
                }
                else
                {
                    best = Math.Min(best, value);
                    beta = Math.Min(beta, best);
                }

                if (alpha >= beta)
                {
                    break;
                }
            }

            return best;
        }

        private void Enumerate(int index, in SimState state)
        {
            if (this.overflow)
            {
                return;
            }

            if (index == this.poolCount)
            {
                if (this.UseDeclarations && !this.ResolveDeclarations(in state))
                {
                    return;
                }

                if (this.Worlds == this.worldLimit)
                {
                    this.overflow = true;
                    return;
                }

                this.worlds[this.Worlds] = state;
                this.worldSouthNorthAnnounces[this.Worlds] = this.southNorthAnnounces;
                this.worldEastWestAnnounces[this.Worlds] = this.eastWestAnnounces;
                this.Worlds++;

                return;
            }

            var bit = 1u << this.pool[index];
            for (var seat = 0; seat < 4; seat++)
            {
                if (this.needs[seat] == 0 || (this.knowledge.Excluded[seat] & bit) != 0)
                {
                    continue;
                }

                var copy = state;
                copy.Hands[seat] |= bit;
                this.needs[seat]--;
                this.Enumerate(index + 1, in copy);
                this.needs[seat]++;
            }
        }

        private bool ResolveDeclarations(in SimState state)
        {
            Array.Clear(this.worldCounts);
            var count = 0;
            if (this.simulator.Kind != SimTables.NoTrumps)
            {
                for (var seat = 0; seat < 4; seat++)
                {
                    count = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat] | this.knowledge.PlayedBy[seat], seat, this.worldAnnounces, count);
                }
            }

            for (var i = 0; i < count; i++)
            {
                var announce = this.worldAnnounces[i];
                this.worldCounts[announce.Seat] += 1u << (2 * (int)announce.Type);
            }

            for (var seat = 0; seat < 4; seat++)
            {
                if (this.worldCounts[seat] != this.declaredCounts[seat])
                {
                    return false;
                }
            }

            for (var i = 0; i < this.knowledge.AnnounceCount; i++)
            {
                var known = this.knowledge.Announces[i];
                if (known.Rank < 0)
                {
                    continue;
                }

                var found = false;
                for (var j = 0; j < count; j++)
                {
                    var candidate = this.worldAnnounces[j];
                    found |= candidate.Seat == known.Seat && candidate.Type == known.Type && candidate.Rank == known.Rank;
                }

                if (!found)
                {
                    return false;
                }
            }

            AnnounceScorer.GetPoints(this.worldAnnounces, count, out this.southNorthAnnounces, out this.eastWestAnnounces);
            return true;
        }
    }
}
