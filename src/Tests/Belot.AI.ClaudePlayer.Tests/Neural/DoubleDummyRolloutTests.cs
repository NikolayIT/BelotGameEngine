namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;
    using System.Numerics;
    using System.Threading;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class DoubleDummyRolloutTests
    {
        [Theory]
        [InlineData(BidType.Clubs, 2)]
        [InlineData(BidType.Diamonds, 2)]
        [InlineData(BidType.Hearts, 2)]
        [InlineData(BidType.Spades, 2)]
        [InlineData(BidType.NoTrumps, 2)]
        [InlineData(BidType.AllTrumps, 2)]
        [InlineData(BidType.AllTrumps | BidType.Double, 2)]
        [InlineData(BidType.Clubs | BidType.ReDouble, 2)]
        [InlineData(BidType.Clubs, 3)]
        [InlineData(BidType.Diamonds, 3)]
        [InlineData(BidType.Hearts, 3)]
        [InlineData(BidType.Spades, 3)]
        [InlineData(BidType.NoTrumps, 3)]
        [InlineData(BidType.AllTrumps, 3)]
        [InlineData(BidType.AllTrumps | BidType.Double, 3)]
        [InlineData(BidType.Clubs | BidType.ReDouble, 3)]
        public void NeuralContinuationsThenExhaustiveLeavesMatchEveryRootValue(BidType contract, int finalTricks)
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(18439));
            var simulator = new BelotSimulator();
            var search = new NeuralSearch { DoubleDummyTricks = finalTricks };
            var leaves = 0;
            var latePositions = 0;
            var decisions = PlayDeal(contract, 8173, (context, viewContext) =>
            {
                // Both scoring paths must apply carried points once, including double/redouble.
                context.HangingPoints = viewContext.HangingPoints = 17;
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var random = new Random(2317);
                var referenceRandom = new Random(2317);
                var actual = Enumerable.Repeat(float.NaN, 32).ToArray();
                Assert.True(search.Evaluate(context, in deal, legal, 4, evaluator, simulator, random, actual));
                var expected = Reference(context, in deal, legal, 4, finalTricks, evaluator, simulator, referenceRandom, out var expectedLeaves);
                Assert.Equal(expected, actual);
                Assert.Equal(referenceRandom.Next(), random.Next());
                Assert.Equal(4, search.NeuralDealsCompleted);
                Assert.Equal(expectedLeaves, search.DoubleDummyLeaves);
                Assert.Equal(0, search.SkippedForcedLeaves);
                leaves += expectedLeaves;
                latePositions += deal.Play.TricksPlayed >= 8 - finalTricks ? 1 : 0;

                Assert.True(NeuralDeal.FromPlayContext(viewContext, simulator, out var viewDeal));
                var viewValues = Enumerable.Repeat(float.NaN, 32).ToArray();
                Assert.True(search.Evaluate(viewContext, in viewDeal, legal, 4, evaluator, simulator, new Random(2317), viewValues));
                Assert.Equal(actual, viewValues);
            });
            Assert.True(decisions > 8);
            Assert.True(leaves > 0);
            Assert.True(latePositions > 0);
        }

        [Fact]
        public void HiddenCallerHandsAndPendingCardsDoNotInfluenceRollouts()
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(19441));
            var simulator = new BelotSimulator();
            var search = new NeuralSearch { DoubleDummyTricks = 3 };
            PlayDeal(BidType.AllTrumps, 8273, (context, _) =>
            {
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var poisoned = deal;
                for (var seat = 0; seat < 4; seat++)
                {
                    if (seat != deal.Play.Turn)
                    {
                        poisoned.Play.Hands[seat] = uint.MaxValue;
                    }

                    poisoned.LastThree[seat] = uint.MaxValue;
                }

                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var expected = new float[32];
                var actual = new float[32];
                Assert.True(search.Evaluate(context, in deal, legal, 3, evaluator, simulator, new Random(2371), expected));
                Assert.True(search.Evaluate(context, in poisoned, legal, 3, evaluator, simulator, new Random(2371), actual));
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public void DeadlinePreservesEveryActionOfTheCompletedWorld()
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(19841));
            var simulator = new BelotSimulator();
            var timed = new NeuralSearch { DoubleDummyTricks = 3, TimeLimitMilliseconds = 1, MinimumDeals = 1 };
            var fixedCount = new NeuralSearch { DoubleDummyTricks = 3 };
            var checkedPosition = false;
            PlayDeal(BidType.AllTrumps, 8373, (context, _) =>
            {
                if (checkedPosition)
                {
                    return;
                }

                checkedPosition = true;
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var actual = new float[32];
                var expected = new float[32];
                var slowRandom = new DelayedRandom(2373);
                var random = new Random(2373);
                Assert.True(timed.Evaluate(context, in deal, legal, 9, evaluator, simulator, slowRandom, actual));
                Assert.True(fixedCount.Evaluate(context, in deal, legal, 1, evaluator, simulator, random, expected));
                Assert.Equal(1, timed.NeuralDealsCompleted);
                Assert.Equal(fixedCount.DoubleDummyLeaves, timed.DoubleDummyLeaves);
                Assert.Equal(BitOperations.PopCount(legal), timed.DoubleDummyLeaves);
                Assert.Equal(expected, actual);
                Assert.Equal(random.Next(), slowRandom.Next());
            });
            Assert.True(checkedPosition);
        }

        [Fact]
        public void RejectsInvalidHorizonsAndIncompatibleSearchModes()
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(19847));
            var simulator = new BelotSimulator();
            var deal = default(NeuralDeal);
            var values = new float[32];
            foreach (var tricks in new[] { -1, 1, 4 })
            {
                var search = new NeuralSearch { DoubleDummyTricks = tricks };
                Assert.Throws<ArgumentOutOfRangeException>(() => search.Evaluate(null, in deal, 3, 4, evaluator, simulator, new Random(1), values));
            }

            foreach (var search in new[]
            {
                new NeuralSearch { DoubleDummyTricks = 2, RolloutTricks = 1 },
                new NeuralSearch { DoubleDummyTricks = 2, ControlVariateDeals = 8 },
                new NeuralSearch { DoubleDummyTricks = 2, PruneMargin = 1 },
            })
            {
                Assert.Throws<InvalidOperationException>(() => search.Evaluate(null, in deal, 3, 4, evaluator, simulator, new Random(1), values));
            }
        }

        private static int PlayDeal(BidType contract, int seed, Action<PlayerPlayCardContext, PlayerPlayCardContext> check)
        {
            var smart = new SmartPlayer.SmartPlayer();
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed) });
            var decisions = 0;
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                if (match.Decision == BelotDecision.Bid)
                {
                    var context = match.CreateBidContext();
                    if (context.RoundNumber > 1)
                    {
                        break;
                    }

                    var current = context.CurrentContract.Type;
                    var bid = BidType.Pass;
                    if (current == BidType.Pass)
                    {
                        bid = contract & ~(BidType.Double | BidType.ReDouble);
                    }
                    else if ((contract & (BidType.Double | BidType.ReDouble)) != 0
                             && (current & (BidType.Double | BidType.ReDouble)) == 0
                             && !seat.IsInSameTeamWith(context.CurrentContract.Player))
                    {
                        bid = BidType.Double;
                    }
                    else if (contract.HasFlag(BidType.ReDouble) && current.HasFlag(BidType.Double)
                                                             && seat.IsInSameTeamWith(context.CurrentContract.Player))
                    {
                        bid = BidType.ReDouble;
                    }

                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Bid(bid)));
                }
                else if (match.Decision == BelotDecision.Announce)
                {
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces)));
                }
                else
                {
                    var context = match.CreatePlayCardContext();
                    Assert.Equal(contract, context.CurrentContract.Type);
                    check(context, match.GetView(seat).CreatePlayCardContext());
                    decisions++;
                    var action = smart.PlayCard(context);
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote)));
                }
            }

            return decisions;
        }

        private static float[] Reference(
            PlayerPlayCardContext context,
            in NeuralDeal deal,
            uint legal,
            int worlds,
            int finalTricks,
            NeuralEvaluator evaluator,
            BelotSimulator simulator,
            Random random,
            out int leaves)
        {
            var knowledge = new RoundKnowledge();
            var sampler = new WorldSampler();
            Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
            Assert.True(sampler.Configure(knowledge));
            var cards = Enumerable.Range(0, 32).Where(card => (legal & (1u << card)) != 0).ToArray();
            var sums = new double[32];
            var values = new float[32];
            var team = deal.Play.Turn & 1;
            leaves = 0;
            for (var worldIndex = 0; worldIndex < worlds; worldIndex++)
            {
                var state = knowledge.Root;
                sampler.Sample(ref state, random);
                var world = deal;
                for (var seat = 0; seat < 4; seat++)
                {
                    world.Play.Hands[seat] = state.Hands[seat];
                }

                var announced = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
                Array.Copy(knowledge.Announces, announced, knowledge.AnnounceCount);
                var count = knowledge.AnnounceCount;
                for (var seat = 0; seat < 4; seat++)
                {
                    if ((knowledge.SeatsToDeclare & (1 << seat)) == 0)
                    {
                        continue;
                    }

                    var before = count;
                    count = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat], seat, announced, count);
                    world.ToDeclare[seat] = 0;
                    foreach (var announce in announced.Skip(before).Take(count - before))
                    {
                        world.ToDeclare[seat] |= NeuralDeal.DeclarationBit(announce.Type);
                    }
                }

                AnnounceScorer.ResolveHiddenRanks(announced, count, random);
                AnnounceScorer.GetPoints(announced, count, out world.SouthNorthAnnounces, out world.EastWestAnnounces);
                foreach (var card in cards)
                {
                    var copy = world;
                    copy.PlayCard(simulator, card, legal);
                    while (copy.Play.TricksPlayed < 8 - finalTricks)
                    {
                        copy.DeclareIfFirstCard();
                        var moves = simulator.LegalMoves(in copy.Play);
                        var move = (moves & (moves - 1)) == 0
                            ? BitOperations.TrailingZeroCount(moves)
                            : evaluator.BestCard(in copy, moves, values);
                        copy.PlayCard(simulator, move, moves);
                    }

                    leaves += copy.IsFinished ? 0 : 1;
                    sums[card] += Exhaustive(in copy.Play, simulator, team, world.SouthNorthAnnounces, world.EastWestAnnounces);
                }
            }

            var result = Enumerable.Repeat(float.NaN, 32).ToArray();
            foreach (var card in cards)
            {
                result[card] = (float)(sums[card] / worlds);
            }

            return result;
        }

        private static int Exhaustive(in SimState state, BelotSimulator simulator, int team, int southNorth, int eastWest)
        {
            if (state.TricksPlayed == 8)
            {
                simulator.Score(in state, southNorth, eastWest, out var first, out var second, out _);
                return team == 0 ? first - second : second - first;
            }

            var maximizing = (state.Turn & 1) == team;
            var best = maximizing ? int.MinValue : int.MaxValue;
            var legal = simulator.LegalMoves(in state);
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var copy = state;
                simulator.Play(ref copy, BitOperations.TrailingZeroCount(rest), legal);
                var value = Exhaustive(in copy, simulator, team, southNorth, eastWest);
                best = maximizing ? Math.Max(best, value) : Math.Min(best, value);
            }

            return best;
        }

        private sealed class DelayedRandom : Random
        {
            private bool delayed;

            public DelayedRandom(int seed)
                : base(seed)
            {
            }

            public override int Next(int maxValue)
            {
                if (!this.delayed)
                {
                    Thread.Sleep(5);
                    this.delayed = true;
                }

                return base.Next(maxValue);
            }
        }
    }
}
