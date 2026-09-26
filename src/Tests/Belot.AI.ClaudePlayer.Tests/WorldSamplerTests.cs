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

    public class WorldSamplerTests
    {
        private static readonly BidType[] Contracts =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps,
        };

        [Fact]
        public void EveryDealRespectsTheKnowledge()
        {
            var checkedDeals = 0;
            for (var seed = 0; seed < 400; seed++)
            {
                var random = new Random(seed);
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
                        var sampler = new WorldSampler();
                        Assert.True(sampler.Configure(knowledge));
                        Assert.False(sampler.DroppedConstraints);
                        var me = knowledge.Me;
                        for (var i = 0; i < 20; i++)
                        {
                            var state = knowledge.Root;
                            sampler.Sample(ref state, random);
                            var all = 0u;
                            for (var seat = 0; seat < 4; seat++)
                            {
                                var hand = state.Hands[seat];
                                Assert.Equal(knowledge.HandCounts[seat], BitOperations.PopCount(hand));
                                Assert.Equal(0u, hand & all);
                                all |= hand;
                                if (seat != me)
                                {
                                    Assert.Equal(0u, hand & knowledge.Excluded[seat]);
                                    Assert.Equal(knowledge.Known[seat], hand & knowledge.Known[seat]);
                                }
                            }

                            Assert.Equal(knowledge.MyHand, state.Hands[me]);
                            Assert.Equal(~knowledge.Played, all);
                            checkedDeals++;
                        }
                    };
                }

                Deal.Play(hands, new Bid(Deal.Seats[random.Next(4)], Contracts[seed % 6]), Deal.Seats[random.Next(4)], players);
            }

            Assert.True(checkedDeals > 100_000);
        }

        [Fact]
        public void WithNothingKnownEveryCardGoesAnywhereAsOften()
        {
            // South leads first, nothing played: each of the 24 unseen cards should land with each
            // of the three others about a third of the time.
            var counts = new int[32, 4];
            const int Deals = 30_000;
            var random = new Random(5);
            var hands = new[]
            {
                Deal.Cards("7C 8C 9C 7D 8D 9D 7H 8H"),
                Deal.Cards("10C JC QC 10D JD QD 9H 10H"),
                Deal.Cards("KC AC KD AD JH QH KH AH"),
                Deal.Cards("7S 8S 9S 10S JS QS KS AS"),
            };
            var south = new TestPlayer(random)
            {
                OnDecision = context =>
                {
                    if (((IList<PlayCardAction>)context.RoundActions).Count != 0)
                    {
                        return;
                    }

                    var simulator = new BelotSimulator();
                    simulator.SetContract(BidType.NoTrumps, 0, 0);
                    var knowledge = new RoundKnowledge();
                    knowledge.Build(context, simulator, true);
                    var sampler = new WorldSampler();
                    sampler.Configure(knowledge);
                    for (var i = 0; i < Deals; i++)
                    {
                        var state = knowledge.Root;
                        sampler.Sample(ref state, random);
                        for (var seat = 1; seat < 4; seat++)
                        {
                            for (var rest = state.Hands[seat]; rest != 0; rest &= rest - 1)
                            {
                                counts[BitOperations.TrailingZeroCount(rest), seat]++;
                            }
                        }
                    }
                },
            };
            var players = new[] { south, new TestPlayer(random), new TestPlayer(random), new TestPlayer(random) };
            Deal.Play(hands, new Bid(PlayerPosition.South, BidType.NoTrumps), PlayerPosition.South, players);

            var unseen = ~Deal.Mask(Deal.Cards("7C 8C 9C 7D 8D 9D 7H 8H"));
            for (var card = 0; card < 32; card++)
            {
                if (((unseen >> card) & 1) == 0)
                {
                    continue;
                }

                for (var seat = 1; seat < 4; seat++)
                {
                    Assert.InRange(counts[card, seat], (Deals / 3) - 600, (Deals / 3) + 600);
                }
            }
        }
    }
}
