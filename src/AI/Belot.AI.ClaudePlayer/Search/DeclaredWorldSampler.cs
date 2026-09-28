namespace Belot.AI.ClaudePlayer.Search
{
    using System;

    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// Rejects uniform hard-constrained worlds whose original hands disagree with the public
    /// combination types. Assumes every seat declared every available combination. Hidden ranks
    /// and the resulting points come from each accepted hand, rather than independent draws.
    /// </summary>
    internal sealed class DeclaredWorldSampler
    {
        private readonly UniformWorldSampler sampler = new UniformWorldSampler();
        private readonly uint[] declaredCounts = new uint[4];
        private readonly DeclaredAnnounce[] announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
        private RoundKnowledge knowledge;
        private int kind;

        /// <summary>Configures a position after all four seats have played and declared.</summary>
        public bool Configure(PlayerPlayCardContext context, RoundKnowledge knowledge, int kind)
        {
            this.knowledge = null;
            if (knowledge.Root.TricksPlayed == 0)
            {
                return false;
            }

            Array.Clear(this.declaredCounts);
            for (var i = 0; i < knowledge.AnnounceCount; i++)
            {
                var announce = knowledge.Announces[i];
                this.declaredCounts[announce.Seat] += 1u << (2 * (int)announce.Type);
            }

            // Knowledge reconstructs our melds under the declare-all policy. A human may
            // withhold one, so reject that context instead of scoring an undeclared meld.
            var observedOwn = 0u;
            foreach (var announce in context.Announces)
            {
                if (announce.Player.Index() == knowledge.Me && announce.Type != AnnounceType.Belot)
                {
                    observedOwn += 1u << (2 * (int)announce.Type);
                }
            }

            if (observedOwn != this.declaredCounts[knowledge.Me] || !this.sampler.Configure(knowledge))
            {
                return false;
            }

            this.knowledge = knowledge;
            this.kind = kind;
            return true;
        }

        /// <summary>Tries one proposal. The caller bounds retries and counts accepted worlds.</summary>
        public bool TrySample(ref SimState state, Random random, out int southNorth, out int eastWest)
        {
            if (this.knowledge == null)
            {
                southNorth = 0;
                eastWest = 0;
                return false;
            }

            this.sampler.Sample(ref state, random);
            return this.TryScore(in state, out southNorth, out eastWest);
        }

        /// <summary>Checks combination types and known ranks, and scores the reconstructed hands.</summary>
        internal bool TryScore(in SimState state, out int southNorth, out int eastWest)
        {
            southNorth = 0;
            eastWest = 0;
            if (this.knowledge == null)
            {
                return false;
            }

            var count = 0;
            for (var seat = 0; seat < 4; seat++)
            {
                var start = count;
                if (this.kind != SimTables.NoTrumps)
                {
                    count = AnnounceScorer.AddDeclaredCombinations(
                        state.Hands[seat] | this.knowledge.PlayedBy[seat], seat, this.announces, count);
                }

                var actualCounts = 0u;
                for (var i = start; i < count; i++)
                {
                    actualCounts += 1u << (2 * (int)this.announces[i].Type);
                }

                if (actualCounts != this.declaredCounts[seat])
                {
                    return false;
                }
            }

            for (var i = 0; i < this.knowledge.AnnounceCount; i++)
            {
                var declared = this.knowledge.Announces[i];
                if (declared.Rank < 0)
                {
                    continue;
                }

                var found = false;
                for (var j = 0; j < count; j++)
                {
                    var actual = this.announces[j];
                    found |= actual.Seat == declared.Seat && actual.Type == declared.Type && actual.Rank == declared.Rank;
                }

                if (!found)
                {
                    return false;
                }
            }

            AnnounceScorer.GetPoints(this.announces, count, out southNorth, out eastWest);
            return true;
        }
    }
}
