namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
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

    public class ControlVariateSearchTests
    {
        [Theory]
        [InlineData(BidType.Clubs)]
        [InlineData(BidType.Diamonds)]
        [InlineData(BidType.Hearts)]
        [InlineData(BidType.Spades)]
        [InlineData(BidType.NoTrumps)]
        [InlineData(BidType.AllTrumps)]
        [InlineData(BidType.AllTrumps | BidType.Double)]
        [InlineData(BidType.Clubs | BidType.ReDouble)]
        public void EqualWorldCountsReproduceOrdinarySearchAndRandomStream(BidType contract)
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(639));
            var simulator = new BelotSimulator();
            var ordinary = new NeuralSearch();
            var corrected = new NeuralSearch { ControlVariateDeals = 5 };
            var decisions = PlayDeal(contract, 1643, (context, viewContext) =>
            {
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var expected = Enumerable.Repeat(float.NaN, 32).ToArray();
                var actual = Enumerable.Repeat(float.NaN, 32).ToArray();
                var firstRandom = new Random(641);
                var secondRandom = new Random(641);
                Assert.True(ordinary.Evaluate(context, in deal, legal, 5, evaluator, simulator, firstRandom, expected));
                Assert.True(corrected.Evaluate(viewContext, in deal, legal, 5, evaluator, simulator, secondRandom, actual));
                Assert.Equal(expected, actual);
                Assert.Equal(firstRandom.Next(), secondRandom.Next());
                Assert.Equal(5, ordinary.NeuralDealsCompleted);
                Assert.Equal(0, ordinary.ControlVariateDealsCompleted);
                Assert.Equal(5, corrected.NeuralDealsCompleted);
                Assert.Equal(5, corrected.ControlVariateDealsCompleted);
            });
            Assert.True(decisions > 8);
        }

        [Theory]
        [InlineData(BidType.Clubs)]
        [InlineData(BidType.NoTrumps)]
        [InlineData(BidType.AllTrumps)]
        [InlineData(BidType.Hearts | BidType.Double)]
        [InlineData(BidType.AllTrumps | BidType.ReDouble)]
        public void MoreCheapWorldsMatchIndependentRolloutAndSampleVariances(BidType contract)
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(645));
            var simulator = new BelotSimulator();
            var search = new NeuralSearch { ControlVariateDeals = 13 };
            var sawDifferentPolicies = false;
            var decisions = PlayDeal(contract, 2674, (context, viewContext) =>
            {
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var random = new Random(647);
                var referenceRandom = new Random(647);
                var actual = Enumerable.Repeat(float.NaN, 32).ToArray();
                Assert.True(search.Evaluate(context, in deal, legal, 4, evaluator, simulator, random, actual));
                var expected = Reference(
                    context, in deal, legal, 4, 13, evaluator, simulator, referenceRandom, out var neuralVariance, out var residualVariance);
                Assert.Equal(expected, actual);
                Assert.Equal(referenceRandom.Next(), random.Next());
                Assert.Equal(4, search.NeuralDealsCompleted);
                Assert.Equal(13, search.ControlVariateDealsCompleted);
                Assert.Equal(neuralVariance, search.NeuralDifferenceVariance, 9);
                Assert.Equal(residualVariance, search.ResidualDifferenceVariance, 9);
                sawDifferentPolicies |= residualVariance > 0;

                Assert.True(NeuralDeal.FromPlayContext(viewContext, simulator, out var viewDeal));
                var viewValues = Enumerable.Repeat(float.NaN, 32).ToArray();
                Assert.True(search.Evaluate(viewContext, in viewDeal, legal, 4, evaluator, simulator, new Random(647), viewValues));
                Assert.Equal(actual, viewValues);
            });
            Assert.True(decisions > 8);
            Assert.True(sawDifferentPolicies);
        }

        [Fact]
        public void DeadlineStopsAfterCompleteWorldAndUsesActualCounts()
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(650));
            var simulator = new BelotSimulator();
            var timed = new NeuralSearch { ControlVariateDeals = 17, TimeLimitMilliseconds = 1, MinimumDeals = 1 };
            var fixedCount = new NeuralSearch { ControlVariateDeals = 1 };
            var checkedPosition = false;
            PlayDeal(BidType.AllTrumps, 2650, (context, _) =>
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
                var slowRandom = new DelayedRandom(651);
                var ordinaryRandom = new Random(651);
                Assert.True(timed.Evaluate(context, in deal, legal, 9, evaluator, simulator, slowRandom, actual));
                Assert.True(fixedCount.Evaluate(context, in deal, legal, 1, evaluator, simulator, ordinaryRandom, expected));
                Assert.Equal(1, timed.NeuralDealsCompleted);
                Assert.Equal(1, timed.ControlVariateDealsCompleted);
                Assert.Equal(0, timed.NeuralDifferenceVariance);
                Assert.Equal(0, timed.ResidualDifferenceVariance);
                Assert.Equal(expected, actual);
                Assert.Equal(ordinaryRandom.Next(), slowRandom.Next());
            });
            Assert.True(checkedPosition);
        }

        [Fact]
        public void RejectsUnsupportedCountsAndBiasedCombinations()
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(655));
            var simulator = new BelotSimulator();
            var values = new float[32];
            var deal = default(NeuralDeal);
            foreach (var (cheap, neural) in new[] { (-1, 1), (2, 3), (1, 0) })
            {
                var search = new NeuralSearch { ControlVariateDeals = cheap };
                Assert.Throws<ArgumentOutOfRangeException>(() => search.Evaluate(null, in deal, 3, neural, evaluator, simulator, new Random(1), values));
            }

            foreach (var search in new[]
            {
                new NeuralSearch { ControlVariateDeals = 3, PriorDeals = 1 },
                new NeuralSearch { ControlVariateDeals = 3, PruneMargin = 1 },
            })
            {
                Assert.Throws<InvalidOperationException>(() => search.Evaluate(null, in deal, 3, 2, evaluator, simulator, new Random(1), values));
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
            int neuralWorlds,
            int cheapWorlds,
            NeuralEvaluator evaluator,
            BelotSimulator simulator,
            Random random,
            out double neuralVariance,
            out double residualVariance)
        {
            var knowledge = new RoundKnowledge();
            var sampler = new WorldSampler();
            Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
            Assert.True(sampler.Configure(knowledge));
            var cards = Enumerable.Range(0, 32).Where(card => (legal & (1u << card)) != 0).ToArray();
            var neural = new List<int[]>();
            var cheap = new List<int[]>();
            var values = new float[32];
            var team = deal.Play.Turn & 1;
            for (var i = 0; i < cheapWorlds; i++)
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
                var cheapReturns = new int[32];
                var neuralReturns = new int[32];
                foreach (var card in cards)
                {
                    // The cheap reference uses only the mask simulator, independently of
                    // NeuralDeal's declaration/knowledge updates in the implementation.
                    var greedy = world.Play;
                    simulator.Play(ref greedy, card, legal);
                    while (greedy.TricksPlayed < 8)
                    {
                        var moves = simulator.LegalMoves(in greedy);
                        simulator.Play(ref greedy, simulator.ChooseRolloutMove(in greedy, moves), moves);
                    }

                    simulator.Score(in greedy, world.SouthNorthAnnounces, world.EastWestAnnounces, out var first, out var second, out _);
                    cheapReturns[card] = team == 0 ? first - second : second - first;
                    if (i >= neuralWorlds)
                    {
                        continue;
                    }

                    var copy = world;
                    copy.PlayCard(simulator, card, legal);
                    while (!copy.IsFinished)
                    {
                        copy.DeclareIfFirstCard();
                        var moves = simulator.LegalMoves(in copy.Play);
                        var move = (moves & (moves - 1)) == 0
                            ? BitOperations.TrailingZeroCount(moves)
                            : evaluator.BestCard(in copy, moves, values);
                        copy.PlayCard(simulator, move, moves);
                    }

                    copy.Score(simulator, out first, out second, out _);
                    neuralReturns[card] = team == 0 ? first - second : second - first;
                }

                cheap.Add(cheapReturns);
                if (i < neuralWorlds)
                {
                    neural.Add(neuralReturns);
                }
            }

            var result = Enumerable.Repeat(float.NaN, 32).ToArray();
            foreach (var card in cards)
            {
                var correction = cheap.Average(row => row[card]) - cheap.Take(neuralWorlds).Average(row => row[card]);
                result[card] = (float)(neural.Average(row => row[card]) + correction);
            }

            var pivot = cards[0];
            neuralVariance = cards.Skip(1).Average(card => SampleVariance(neural.Select(row => (double)(row[card] - row[pivot])).ToArray()));
            residualVariance = cards.Skip(1).Average(card => SampleVariance(neural.Select((row, index) =>
                (double)(row[card] - row[pivot] - cheap[index][card] + cheap[index][pivot])).ToArray()));
            return result;
        }

        private static double SampleVariance(double[] values)
        {
            var mean = values.Average();
            return values.Sum(value => (value - mean) * (value - mean)) / (values.Length - 1);
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
