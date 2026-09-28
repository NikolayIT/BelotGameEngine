namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class TruncatedSearchTests
    {
        [Theory]
        [InlineData(BidType.Clubs, 1, false)]
        [InlineData(BidType.Clubs, 1, true)]
        [InlineData(BidType.Clubs, 2, false)]
        [InlineData(BidType.Clubs, 2, true)]
        [InlineData(BidType.Diamonds, 1, false)]
        [InlineData(BidType.Diamonds, 1, true)]
        [InlineData(BidType.Diamonds, 2, false)]
        [InlineData(BidType.Diamonds, 2, true)]
        [InlineData(BidType.Hearts, 1, false)]
        [InlineData(BidType.Hearts, 1, true)]
        [InlineData(BidType.Hearts, 2, false)]
        [InlineData(BidType.Hearts, 2, true)]
        [InlineData(BidType.Spades, 1, false)]
        [InlineData(BidType.Spades, 1, true)]
        [InlineData(BidType.Spades, 2, false)]
        [InlineData(BidType.Spades, 2, true)]
        [InlineData(BidType.NoTrumps, 1, false)]
        [InlineData(BidType.NoTrumps, 1, true)]
        [InlineData(BidType.NoTrumps, 2, false)]
        [InlineData(BidType.NoTrumps, 2, true)]
        [InlineData(BidType.AllTrumps, 1, false)]
        [InlineData(BidType.AllTrumps, 1, true)]
        [InlineData(BidType.AllTrumps, 2, false)]
        [InlineData(BidType.AllTrumps, 2, true)]
        [InlineData(BidType.AllTrumps | BidType.Double, 1, false)]
        [InlineData(BidType.AllTrumps | BidType.Double, 1, true)]
        [InlineData(BidType.Clubs | BidType.ReDouble, 2, false)]
        [InlineData(BidType.Clubs | BidType.ReDouble, 2, true)]
        public void MatchesIndependentFixedCardCountReferenceAndPublicView(BidType contract, int tricks, bool rootLeaf)
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(853));
            var simulator = new BelotSimulator();
            var search = new NeuralSearch { RolloutTricks = tricks, RolloutRootLeaf = rootLeaf };
            var sameTeamLeaves = 0;
            var oppositeTeamLeaves = 0;
            var decisions = PlayDeal(contract, 2857, (context, viewContext) =>
            {
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var random = new Random(859);
                var referenceRandom = new Random(859);
                var skippedForcedLeaves = 0;
                var actual = Enumerable.Repeat(float.NaN, 32).ToArray();
                Assert.True(search.Evaluate(context, in deal, legal, 5, evaluator, simulator, random, actual));
                var expected = Reference(
                    context, in deal, legal, 5, tricks, rootLeaf, evaluator, simulator, referenceRandom, ref sameTeamLeaves, ref oppositeTeamLeaves, ref skippedForcedLeaves);
                Assert.Equal(expected, actual);
                Assert.Equal(referenceRandom.Next(), random.Next());
                Assert.Equal(5, search.NeuralDealsCompleted);
                Assert.Equal(skippedForcedLeaves, search.SkippedForcedLeaves);

                Assert.True(NeuralDeal.FromPlayContext(viewContext, simulator, out var viewDeal));
                var viewValues = Enumerable.Repeat(float.NaN, 32).ToArray();
                Assert.True(search.Evaluate(viewContext, in viewDeal, legal, 5, evaluator, simulator, new Random(859), viewValues));
                Assert.Equal(actual, viewValues);
            });
            Assert.True(decisions > 8);
            Assert.True(sameTeamLeaves > 0);
            if (rootLeaf)
            {
                Assert.Equal(0, oppositeTeamLeaves);
            }
            else
            {
                Assert.True(oppositeTeamLeaves > 0);
            }
        }

        [Theory]
        [InlineData(BidType.Clubs)]
        [InlineData(BidType.NoTrumps)]
        [InlineData(BidType.AllTrumps)]
        public void FullHorizonAndTerminalLeavesMatchLegacyExactly(BidType contract)
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(863));
            var simulator = new BelotSimulator();
            var legacy = new NeuralSearch();
            var fullHorizon = new NeuralSearch { RolloutTricks = 8 };
            var truncated = new NeuralSearch { RolloutTricks = 1 };
            var terminalPositions = 0;
            var skippedForcedLeaves = 0;
            PlayDeal(contract, 2867, (context, _) =>
            {
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var expected = new float[32];
                var actual = new float[32];
                var firstRandom = new Random(869);
                var secondRandom = new Random(869);
                Assert.True(legacy.Evaluate(context, in deal, legal, 4, evaluator, simulator, firstRandom, expected));
                Assert.True(fullHorizon.Evaluate(context, in deal, legal, 4, evaluator, simulator, secondRandom, actual));
                Assert.Equal(expected, actual);
                Assert.Equal(firstRandom.Next(), secondRandom.Next());
                if (deal.Play.TricksPlayed >= 6)
                {
                    // A one-trick horizon leaves only the final forced card at the root.
                    // It must continue to exact terminal scoring, without querying that Q.
                    Assert.True(truncated.Evaluate(context, in deal, legal, 4, evaluator, simulator, new Random(869), actual));
                    Assert.Equal(expected, actual);
                    skippedForcedLeaves += truncated.SkippedForcedLeaves;
                    terminalPositions++;
                }
            });
            Assert.True(terminalPositions > 0);
            Assert.True(skippedForcedLeaves > 0);
        }

        [Fact]
        public void HiddenHandsInCallerStateDoNotInfluenceSearch()
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(877));
            var simulator = new BelotSimulator();
            var search = new NeuralSearch { RolloutTricks = 1 };
            var decisions = PlayDeal(BidType.AllTrumps, 2881, (context, _) =>
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
                Assert.True(search.Evaluate(context, in deal, legal, 4, evaluator, simulator, new Random(883), expected));
                Assert.True(search.Evaluate(context, in poisoned, legal, 4, evaluator, simulator, new Random(883), actual));
                Assert.Equal(expected, actual);
            });
            Assert.True(decisions > 8);
        }

        [Fact]
        public void RejectsInvalidHorizonsAndUntestedControlVariateCombination()
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(887));
            var simulator = new BelotSimulator();
            var values = new float[32];
            var deal = default(NeuralDeal);
            Assert.Equal(0, new NeuralSearch().RolloutTricks);
            Assert.True(new NeuralSearch().RolloutRootLeaf);
            foreach (var tricks in new[] { -1, 9 })
            {
                var search = new NeuralSearch { RolloutTricks = tricks };
                Assert.Throws<ArgumentOutOfRangeException>(() => search.Evaluate(null, in deal, 3, 2, evaluator, simulator, new Random(1), values));
            }

            var combined = new NeuralSearch { RolloutTricks = 1, ControlVariateDeals = 4 };
            Assert.Throws<InvalidOperationException>(() => combined.Evaluate(null, in deal, 3, 2, evaluator, simulator, new Random(1), values));
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
            int tricks,
            bool rootLeaf,
            NeuralEvaluator evaluator,
            BelotSimulator simulator,
            Random random,
            ref int sameTeamLeaves,
            ref int oppositeTeamLeaves,
            ref int skippedForcedLeaves)
        {
            var knowledge = new RoundKnowledge();
            var sampler = new WorldSampler();
            Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
            Assert.True(sampler.Configure(knowledge));
            var cards = Enumerable.Range(0, 32).Where(card => (legal & (1u << card)) != 0).ToArray();
            var sums = new double[32];
            var values = new float[32];
            var rootTeam = deal.Play.Turn & 1;

            // Count remaining individual cards independently of the implementation's
            // completed-trick stop. The root action is already one card of this horizon.
            var alreadyPlayed = (deal.Play.TricksPlayed * 4) + deal.Play.TrickCards;
            var followingCards = Math.Min(31 - alreadyPlayed, (tricks * 4) - deal.Play.TrickCards - 1);
            for (var i = 0; i < worlds; i++)
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
                    var trajectory = new List<NeuralDeal> { copy };
                    while (!copy.IsFinished)
                    {
                        Advance(ref copy, evaluator, simulator, values);
                        trajectory.Add(copy);
                    }

                    // Build the whole policy continuation first, then choose a leaf from
                    // that trajectory independently of the search's stopping loop.
                    var rootSeat = deal.Play.Turn;
                    var leaf = trajectory.FindIndex(followingCards, state => state.IsFinished
                        || ((!rootLeaf || state.Play.Turn == rootSeat) && BitOperations.PopCount(simulator.LegalMoves(in state.Play)) >= 2));
                    Assert.True(leaf >= followingCards);
                    for (var index = followingCards; index < leaf; index++)
                    {
                        var stateBeforeLeaf = trajectory[index];
                        if (!rootLeaf || stateBeforeLeaf.Play.Turn == rootSeat)
                        {
                            Assert.Equal(1, BitOperations.PopCount(simulator.LegalMoves(in stateBeforeLeaf.Play)));
                            skippedForcedLeaves++;
                        }
                    }

                    copy = trajectory[leaf];
                    if (copy.IsFinished)
                    {
                        copy.Score(simulator, out var first, out var second, out _);
                        sums[card] += rootTeam == 0 ? first - second : second - first;
                    }
                    else
                    {
                        copy.DeclareIfFirstCard();
                        var moves = simulator.LegalMoves(in copy.Play);
                        Assert.True(BitOperations.PopCount(moves) >= 2);
                        Assert.True(!rootLeaf || copy.Play.Turn == rootSeat);
                        evaluator.EvaluateCards(in copy, moves, values);
                        var highest = Enumerable.Range(0, 32).Where(move => (moves & (1u << move)) != 0).Max(move => values[move]);
                        var ourMover = (copy.Play.Turn & 1) == rootTeam;
                        sums[card] += ourMover ? highest : -highest;
                        sameTeamLeaves += ourMover ? 1 : 0;
                        oppositeTeamLeaves += ourMover ? 0 : 1;
                    }
                }
            }

            var result = Enumerable.Repeat(float.NaN, 32).ToArray();
            foreach (var card in cards)
            {
                result[card] = (float)(sums[card] / worlds);
            }

            return result;
        }

        private static void Advance(ref NeuralDeal deal, NeuralEvaluator evaluator, BelotSimulator simulator, float[] values)
        {
            deal.DeclareIfFirstCard();
            var moves = simulator.LegalMoves(in deal.Play);
            var chosen = BitOperations.TrailingZeroCount(moves);
            if (BitOperations.PopCount(moves) > 1)
            {
                evaluator.EvaluateCards(in deal, moves, values);
                chosen = Enumerable.Range(0, 32).Where(move => (moves & (1u << move)) != 0)
                    .OrderByDescending(move => values[move]).ThenBy(move => move).First();
            }

            deal.PlayCard(simulator, chosen, moves);
        }
    }
}
