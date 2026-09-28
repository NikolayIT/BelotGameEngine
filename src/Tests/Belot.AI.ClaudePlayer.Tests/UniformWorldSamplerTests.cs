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

    public class UniformWorldSamplerTests
    {
        [Fact]
        public void CompleteEightCardHandsHaveTheMultinomialWorldCount()
        {
            var sampler = new UniformWorldSampler();
            var known = new uint[4];
            var excluded = new uint[4];
            var handCounts = new[] { 8, 8, 8, 8 };
            Assert.True(sampler.Configure(0, 255u, 0, known, excluded, handCounts));
            Assert.Equal(9_465_511_770UL, sampler.WorldCount);

            var first = default(SimState);
            first.Hands[0] = 255u;
            var second = first;
            var random1 = new Random(1281);
            var random2 = new Random(1281);
            for (var sample = 0; sample < 100; sample++)
            {
                sampler.Sample(ref first, random1);
                sampler.Sample(ref second, random2);
                var all = 0u;
                for (var seat = 0; seat < 4; seat++)
                {
                    Assert.Equal(first.Hands[seat], second.Hands[seat]);
                    Assert.Equal(8, BitOperations.PopCount(first.Hands[seat]));
                    Assert.Equal(0u, all & first.Hands[seat]);
                    all |= first.Hands[seat];
                }

                Assert.Equal(uint.MaxValue, all);
            }
        }

        [Fact]
        public void UnevenAllowedSeatsStillGiveEveryWorldOneIntegerOutcome()
        {
            // A/B/C need 2/1/1 cards. The four cards allow AB, AC, BC and ABC.
            // Room-weighted placement with all shuffled orders gives 25/108, 25/108,
            // 29/108 and 29/108; counting completions gives the required quarters.
            var sampler = new UniformWorldSampler();
            var known = new uint[4];
            var excluded = new[] { 0u, 4u, 2u, 1u };
            Assert.True(sampler.Configure(0, 16u, ~31u, known, excluded, new[] { 1, 2, 1, 1 }));
            Assert.Equal(4UL, sampler.WorldCount);
            var expected = new HashSet<(uint South, uint East, uint North, uint West)>
            {
                (16u, 3u, 4u, 8u),
                (16u, 3u, 8u, 4u),
                (16u, 9u, 4u, 2u),
                (16u, 10u, 1u, 4u),
            };
            var actual = new HashSet<(uint South, uint East, uint North, uint West)>();
            var state = default(SimState);
            state.Hands[0] = 16u;
            for (ulong index = 0; index < sampler.WorldCount; index++)
            {
                sampler.SampleAt(ref state, index);
                Assert.True(actual.Add(Hands(in state)));
            }

            Assert.True(expected.SetEquals(actual));
        }

        [Fact]
        public void EveryIntegerSelectsExactlyOneIndependentEnumeratedWorld()
        {
            // Exhausting uniformly selected integer outcomes tests exact probabilities without
            // a statistical tolerance: every legal assignment must appear exactly once.
            var random = new Random(18731);
            var nonempty = 0;
            var empty = 0;
            var sampler = new UniformWorldSampler();
            for (var trial = 0; trial < 160; trial++)
            {
                var me = trial % 4;
                const uint MyHand = 1u << 31;
                const uint Pool = 63u;
                var known = new uint[4];
                var excluded = new uint[4];
                var handCounts = new int[4];
                handCounts[me] = 1;
                var occupied = MyHand | Pool;
                for (var other = 0; other < 3; other++)
                {
                    var seat = (me + other + 1) & 3;
                    known[seat] = (trial & (1 << other)) != 0 ? 1u << (24 + other) : 0;
                    handCounts[seat] = 2 + BitOperations.PopCount(known[seat]);
                    occupied |= known[seat];
                }

                for (var card = 0; card < 6; card++)
                {
                    var group = trial < 4 ? 7 : random.Next(1, 8);
                    for (var other = 0; other < 3; other++)
                    {
                        if ((group & (1 << other)) == 0)
                        {
                            excluded[(me + other + 1) & 3] |= 1u << card;
                        }
                    }
                }

                var expected = Enumerate(me, MyHand, ~occupied, known, excluded, handCounts);
                Assert.Equal(expected.Count > 0, sampler.Configure(me, MyHand, ~occupied, known, excluded, handCounts));
                Assert.Equal((ulong)expected.Count, sampler.WorldCount);
                if (expected.Count == 0)
                {
                    empty++;
                    continue;
                }

                nonempty++;
                var actual = new HashSet<(uint South, uint East, uint North, uint West)>();
                var state = default(SimState);
                state.Hands[me] = MyHand;
                state.Turn = me;
                state.TrickCards = 2;
                state.TrickPoints = 19;
                state.SouthNorthPoints = 63;
                for (ulong index = 0; index < sampler.WorldCount; index++)
                {
                    sampler.SampleAt(ref state, index);
                    Assert.True(actual.Add(Hands(in state)));
                    Assert.Equal(MyHand, state.Hands[me]);
                    Assert.Equal(me, state.Turn);
                    Assert.Equal(2, state.TrickCards);
                    Assert.Equal(19, state.TrickPoints);
                    Assert.Equal(63, state.SouthNorthPoints);
                }

                Assert.True(expected.SetEquals(actual));
                Assert.Throws<ArgumentOutOfRangeException>(() => sampler.SampleAt(ref state, sampler.WorldCount));
            }

            Assert.True(nonempty > 50);
            Assert.True(empty > 0);
        }

        [Fact]
        public void ContradictionsReturnNoWorldsAndInvalidateThePreviousConfiguration()
        {
            var sampler = new UniformWorldSampler();
            var known = new uint[4];
            var excluded = new uint[4];
            var counts = new[] { 1, 1, 1, 1 };
            const uint MyHand = 8u;
            const uint Played = ~15u;
            Assert.True(sampler.Configure(0, MyHand, Played, known, excluded, counts));
            Assert.Equal(6UL, sampler.WorldCount);

            excluded[2] = excluded[3] = 7u;
            Assert.False(sampler.Configure(0, MyHand, Played, known, excluded, counts));
            Assert.Equal(0UL, sampler.WorldCount);
            var state = default(SimState);
            Assert.Throws<InvalidOperationException>(() => sampler.Sample(ref state, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => sampler.SampleAt(ref state, 0));

            Array.Clear(excluded);
            known[1] = known[2] = 1u;
            Assert.False(sampler.Configure(0, MyHand, Played, known, excluded, counts));
            known[2] = 0;
            excluded[1] = 1u;
            Assert.False(sampler.Configure(0, MyHand, Played, known, excluded, counts));
            excluded[1] = 0;
            known[1] = 8u;
            Assert.False(sampler.Configure(0, MyHand, Played, known, excluded, counts));
            known[1] = 16u;
            Assert.False(sampler.Configure(0, MyHand, Played, known, excluded, counts));
            known[1] = 0;
            counts[1] = 2;
            Assert.False(sampler.Configure(0, MyHand, Played, known, excluded, counts));
            Assert.Equal(0UL, sampler.WorldCount);
        }

        [Fact]
        public void FullyKnownHandsProduceOneWorldWithoutUnknownCards()
        {
            var sampler = new UniformWorldSampler();
            var known = new[] { 0u, 1u, 2u, 4u };
            Assert.True(sampler.Configure(0, 8u, ~15u, known, new uint[4], new[] { 1, 1, 1, 1 }));
            Assert.Equal(1UL, sampler.WorldCount);
            var state = default(SimState);
            state.Hands[0] = 8u;
            sampler.SampleAt(ref state, 0);
            Assert.Equal((8u, 1u, 2u, 4u), Hands(in state));
        }

        [Fact]
        public void SampledHandsRespectRealEngineHistoryAcrossAllContracts()
        {
            var contracts = new[] { BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps };
            var checkedPositions = 0;
            var constrainedPositions = 0;
            var sampler = new UniformWorldSampler();
            for (var seed = 0; seed < 60; seed++)
            {
                var random = new Random(7741 + seed);
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
                        Assert.True(sampler.Configure(knowledge));
                        Assert.True(sampler.WorldCount > 0);
                        for (var sample = 0; sample < 10; sample++)
                        {
                            var state = knowledge.Root;
                            sampler.Sample(ref state, random);
                            var all = 0u;
                            for (var seat = 0; seat < 4; seat++)
                            {
                                Assert.Equal(knowledge.HandCounts[seat], BitOperations.PopCount(state.Hands[seat]));
                                Assert.Equal(0u, all & state.Hands[seat]);
                                all |= state.Hands[seat];
                                Assert.Equal(0u, state.Hands[seat] & knowledge.Excluded[seat]);
                                Assert.Equal(knowledge.Known[seat], state.Hands[seat] & knowledge.Known[seat]);
                            }

                            Assert.Equal(knowledge.MyHand, state.Hands[knowledge.Me]);
                            Assert.Equal(~knowledge.Played, all);
                        }

                        checkedPositions++;
                        constrainedPositions += knowledge.Excluded.Any(mask => (mask & ~(knowledge.Played | knowledge.MyHand)) != 0) ? 1 : 0;
                    };
                }

                Deal.Play(hands, new Bid(Deal.Seats[random.Next(4)], contracts[seed % contracts.Length]), Deal.Seats[random.Next(4)], players);
            }

            Assert.True(checkedPositions > 500);
            Assert.True(constrainedPositions > 0);
        }

        private static HashSet<(uint South, uint East, uint North, uint West)> Enumerate(int me, uint ownHand, uint played, uint[] known, uint[] excluded, int[] counts)
        {
            var occupied = ownHand | played;
            foreach (var cards in known)
            {
                occupied |= cards;
            }

            var pool = Enumerable.Range(0, 32).Where(card => (occupied & (1u << card)) == 0).ToArray();
            var result = new HashSet<(uint South, uint East, uint North, uint West)>();
            var possibilities = (int)Math.Pow(3, pool.Length);
            for (var assignment = 0; assignment < possibilities; assignment++)
            {
                var state = default(SimState);
                for (var seat = 0; seat < 4; seat++)
                {
                    state.Hands[seat] = seat == me ? ownHand : known[seat];
                }

                var code = assignment;
                foreach (var card in pool)
                {
                    state.Hands[(me + 1 + (code % 3)) & 3] |= 1u << card;
                    code /= 3;
                }

                var valid = true;
                for (var seat = 0; seat < 4; seat++)
                {
                    valid &= BitOperations.PopCount(state.Hands[seat]) == counts[seat] && (state.Hands[seat] & excluded[seat]) == 0;
                }

                if (valid)
                {
                    result.Add(Hands(in state));
                }
            }

            return result;
        }

        private static (uint South, uint East, uint North, uint West) Hands(in SimState state) =>
            (state.Hands[0], state.Hands[1], state.Hands[2], state.Hands[3]);
    }
}
