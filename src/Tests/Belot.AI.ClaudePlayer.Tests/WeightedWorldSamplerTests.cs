namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    using Xunit;

    public class WeightedWorldSamplerTests
    {
        [Fact]
        public void UnitWeightsGiveTheUniformPartitionAndIdenticalQuantileWorlds()
        {
            var sampler = new WeightedWorldSampler();
            var uniform = new UniformWorldSampler();
            var known = new uint[4];
            var excluded = new uint[4];
            var counts = new[] { 8, 8, 8, 8 };
            var weights = Enumerable.Repeat(1f, 96).ToArray();
            Assert.True(uniform.Configure(0, 255u, 0, known, excluded, counts));
            Assert.True(sampler.Configure(0, 255u, 0, known, excluded, counts, weights));
            Assert.Equal((double)uniform.WorldCount, sampler.TotalWeight);
            var random = new Random(7193);
            var first = default(SimState);
            first.Hands[0] = 255u;
            var second = first;
            for (var trial = 0; trial < 100; trial++)
            {
                var index = random.NextInt64((long)uniform.WorldCount);
                uniform.SampleAt(ref first, (ulong)index);
                sampler.SampleAt(ref second, (index + .5) / uniform.WorldCount);
                Assert.Equal(Hands(in first), Hands(in second));
            }
        }

        [Fact]
        public void EveryIntegerMassUnitHasTheIndependentEnumeratedAssignmentProbability()
        {
            var random = new Random(18203);
            var sampler = new WeightedWorldSampler();
            var nonempty = 0;
            var empty = 0;
            for (var trial = 0; trial < 80; trial++)
            {
                var me = trial % 4;
                const uint OwnHand = 1u << 31;
                const uint Pool = 63u;
                var known = new uint[4];
                var excluded = new uint[4];
                var counts = new int[4];
                counts[me] = 1;
                var occupied = OwnHand | Pool;
                var weights = new float[96];
                for (var relative = 0; relative < 3; relative++)
                {
                    var seat = (me + relative + 1) & 3;
                    known[seat] = (trial & (1 << relative)) != 0 ? 1u << (24 + relative) : 0;
                    counts[seat] = 2 + BitOperations.PopCount(known[seat]);
                    occupied |= known[seat];
                }

                for (var card = 0; card < 6; card++)
                {
                    var group = trial < 24 ? 7 : random.Next(1, 8);
                    for (var relative = 0; relative < 3; relative++)
                    {
                        weights[(card * 3) + relative] = trial < 24 ? random.Next(1, 3) : random.Next(3);
                        if ((group & (1 << relative)) == 0)
                        {
                            excluded[(me + relative + 1) & 3] |= 1u << card;
                        }
                    }
                }

                var expected = Enumerate(me, OwnHand, ~occupied, known, excluded, counts, weights);
                var total = expected.Values.Sum();
                Assert.Equal(total > 0, sampler.Configure(me, OwnHand, ~occupied, known, excluded, counts, weights));
                Assert.Equal(total, sampler.TotalWeight, 8);
                if (total == 0)
                {
                    empty++;
                    continue;
                }

                nonempty++;
                var observed = new Dictionary<(uint South, uint East, uint North, uint West), int>();
                var state = default(SimState);
                state.Hands[me] = OwnHand;
                state.Turn = me;
                state.TrickCards = 2;
                state.TrickPoints = 19;
                state.SouthNorthPoints = 63;
                for (var unit = 0; unit < (int)total; unit++)
                {
                    sampler.SampleAt(ref state, (unit + .5) / total);
                    var hands = Hands(in state);
                    observed[hands] = observed.GetValueOrDefault(hands) + 1;
                    Assert.Equal(OwnHand, state.Hands[me]);
                    Assert.Equal(me, state.Turn);
                    Assert.Equal(2, state.TrickCards);
                    Assert.Equal(19, state.TrickPoints);
                    Assert.Equal(63, state.SouthNorthPoints);
                }

                Assert.Equal(expected.Count, observed.Count);
                foreach (var world in expected)
                {
                    Assert.Equal(world.Value, observed[world.Key]);
                }
            }

            Assert.True(nonempty > 20, $"Only {nonempty} positive-weight cases were checked.");
            Assert.True(empty > 0);
        }

        [Fact]
        public void CardinalityConditionsTheProductInsteadOfSamplingIndependentOwners()
        {
            var sampler = new WeightedWorldSampler();
            var weights = Enumerable.Repeat(1f, 96).ToArray();
            weights[0] = 2;
            weights[4] = 3;
            Assert.True(sampler.Configure(0, 8u, ~15u, new uint[4], new uint[4], new[] { 1, 1, 1, 1 }, weights));

            // Six assignments have respective product masses 6, 2, 1, 1, 1, 3.
            Assert.Equal(14d, sampler.TotalWeight, 10);
            var state = default(SimState);
            state.Hands[0] = 8u;
            sampler.SampleAt(ref state, 0);
            Assert.Equal((8u, 1u, 2u, 4u), Hands(in state));
            sampler.SampleAt(ref state, Math.BitDecrement(1d));
            Assert.Equal((8u, 4u, 2u, 1u), Hands(in state));
        }

        [Fact]
        public void InvalidWeightsAndContradictionsInvalidatePreviousConfiguration()
        {
            var sampler = new WeightedWorldSampler();
            var known = new uint[4];
            var excluded = new uint[4];
            var counts = new[] { 1, 1, 1, 1 };
            var weights = Enumerable.Repeat(1f, 96).ToArray();
            Assert.True(sampler.Configure(0, 8u, ~15u, known, excluded, counts, weights));
            foreach (var bad in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                weights[95] = bad;
                Assert.False(sampler.Configure(0, 8u, ~15u, known, excluded, counts, weights));
                Assert.Equal(0d, sampler.TotalWeight);
                Assert.Equal(double.NegativeInfinity, sampler.LogTotalWeight);
                var state = default(SimState);
                Assert.Throws<InvalidOperationException>(() => sampler.Sample(ref state, new Random(1)));
            }

            Array.Fill(weights, 0f);
            Assert.False(sampler.Configure(0, 8u, ~15u, known, excluded, counts, weights));
            Array.Fill(weights, 1f);
            excluded[2] = excluded[3] = 7u;
            Assert.False(sampler.Configure(0, 8u, ~15u, known, excluded, counts, weights));
            Array.Clear(excluded);
            known[1] = known[2] = 1u;
            Assert.False(sampler.Configure(0, 8u, ~15u, known, excluded, counts, weights));
            Assert.False(sampler.Configure(0, 8u, ~15u, new uint[4], new uint[4], counts, new float[95]));
        }

        [Fact]
        public void ExtremePositiveWeightsStillSampleValidHandsWhenRawMassUnderflows()
        {
            var sampler = new WeightedWorldSampler();
            var weights = new float[96];
            for (var card = 8; card < 32; card++)
            {
                weights[card * 3] = float.MaxValue;
                weights[(card * 3) + 1] = float.Epsilon;
                weights[(card * 3) + 2] = float.Epsilon;
            }

            Assert.True(sampler.Configure(0, 255u, 0, new uint[4], new uint[4], new[] { 8, 8, 8, 8 }, weights));
            Assert.True(double.IsFinite(sampler.LogTotalWeight));
            Assert.Equal(0d, sampler.TotalWeight);
            var state = default(SimState);
            state.Hands[0] = 255u;
            foreach (var quantile in new[] { 0d, .01, .5, .99, Math.BitDecrement(1d) })
            {
                sampler.SampleAt(ref state, quantile);
                Assert.All(new[] { state.Hands[0], state.Hands[1], state.Hands[2], state.Hands[3] }, hand => Assert.Equal(8, BitOperations.PopCount(hand)));
                Assert.Equal(uint.MaxValue, state.Hands[0] | state.Hands[1] | state.Hands[2] | state.Hands[3]);
            }
        }

        [Fact]
        public void FullyKnownHandsNeedNoOwnershipWeightsAndRejectInvalidQuantiles()
        {
            var sampler = new WeightedWorldSampler();
            Assert.True(sampler.Configure(0, 8u, ~15u, new[] { 0u, 1u, 2u, 4u }, new uint[4], new[] { 1, 1, 1, 1 }, new float[96]));
            Assert.Equal(1d, sampler.TotalWeight);
            var state = default(SimState);
            state.Hands[0] = 8u;
            sampler.SampleAt(ref state, .5);
            Assert.Equal((8u, 1u, 2u, 4u), Hands(in state));
            foreach (var bad in new[] { -1d, 1d, double.NaN, double.PositiveInfinity })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => sampler.SampleAt(ref state, bad));
            }
        }

        [Fact]
        public void SeededWeightedSamplesRespectPublicEngineHistoryAcrossContracts()
        {
            var checkedPositions = 0;
            var contracts = new[] { BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps };
            for (var seed = 0; seed < 12; seed++)
            {
                var random = new Random(1049 + seed);
                var hands = Deal.RandomHands(random);
                var players = Enumerable.Range(0, 4).Select(_ => new TestPlayer(random)).ToArray();
                foreach (var player in players)
                {
                    player.OnDecision = context =>
                    {
                        var simulator = new BelotSimulator();
                        simulator.SetContract(context.CurrentContract.Type, context.CurrentContract.Player.Index(), 0);
                        var knowledge = new RoundKnowledge();
                        Assert.True(knowledge.Build(context, simulator, true));
                        var sampler = new WeightedWorldSampler();
                        var weights = Enumerable.Range(0, 96).Select(_ => (float)(.1 + random.NextDouble())).ToArray();
                        Assert.True(sampler.Configure(knowledge, weights));
                        var firstRandom = new Random(971);
                        var secondRandom = new Random(971);
                        for (var sample = 0; sample < 5; sample++)
                        {
                            var first = knowledge.Root;
                            var second = knowledge.Root;
                            sampler.Sample(ref first, firstRandom);
                            sampler.Sample(ref second, secondRandom);
                            Assert.Equal(Hands(in first), Hands(in second));
                            var all = 0u;
                            for (var seat = 0; seat < 4; seat++)
                            {
                                Assert.Equal(knowledge.HandCounts[seat], BitOperations.PopCount(first.Hands[seat]));
                                Assert.Equal(0u, all & first.Hands[seat]);
                                all |= first.Hands[seat];
                                Assert.Equal(0u, first.Hands[seat] & knowledge.Excluded[seat]);
                                Assert.Equal(knowledge.Known[seat], first.Hands[seat] & knowledge.Known[seat]);
                            }

                            Assert.Equal(knowledge.MyHand, first.Hands[knowledge.Me]);
                            Assert.Equal(~knowledge.Played, all);
                        }

                        checkedPositions++;
                    };
                }

                Deal.Play(hands, new Bid(Deal.Seats[random.Next(4)], contracts[seed % contracts.Length]), Deal.Seats[random.Next(4)], players);
            }

            Assert.True(checkedPositions > 100);
        }

        private static Dictionary<(uint South, uint East, uint North, uint West), double> Enumerate(
            int me, uint ownHand, uint played, uint[] known, uint[] excluded, int[] counts, float[] weights)
        {
            var occupied = ownHand | played;
            foreach (var cards in known)
            {
                occupied |= cards;
            }

            var pool = Enumerable.Range(0, 32).Where(card => (occupied & (1u << card)) == 0).ToArray();
            var result = new Dictionary<(uint South, uint East, uint North, uint West), double>();
            for (var assignment = 0; assignment < (int)Math.Pow(3, pool.Length); assignment++)
            {
                var state = default(SimState);
                for (var seat = 0; seat < 4; seat++)
                {
                    state.Hands[seat] = seat == me ? ownHand : known[seat];
                }

                var mass = 1d;
                var code = assignment;
                foreach (var card in pool)
                {
                    var relative = code % 3;
                    state.Hands[(me + relative + 1) & 3] |= 1u << card;
                    mass *= weights[(card * 3) + relative];
                    code /= 3;
                }

                var valid = mass > 0;
                for (var seat = 0; seat < 4; seat++)
                {
                    valid &= BitOperations.PopCount(state.Hands[seat]) == counts[seat] && (state.Hands[seat] & excluded[seat]) == 0;
                }

                if (valid)
                {
                    result.Add(Hands(in state), mass);
                }
            }

            return result;
        }

        private static (uint South, uint East, uint North, uint West) Hands(in SimState state) =>
            (state.Hands[0], state.Hands[1], state.Hands[2], state.Hands[3]);
    }
}
