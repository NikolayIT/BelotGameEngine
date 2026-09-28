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

    public class DeclaredRolloutTests
    {
        [Fact]
        public void HiddenSequenceRankComesFromItsHandAndWithheldOwnMeldsAreRejected()
        {
            var hands = new[]
            {
                Deal.Cards("10C JC QC 7D 9D 7H 9H AS"),
                Deal.Cards("JD QD KD 7C 9C 8H 10H 7S"),
                Deal.Cards("8C KC 8D AD JH KH 9S QS"),
                Deal.Cards("AC 10D QH AH 8S 10S JS KS"),
            };
            var scripts = new[] { "JC", "7C", "8C", "AC" };
            var players = scripts.Select(script => new TestPlayer(new Random(391), script)).ToArray();
            var checkedPosition = false;
            players[0].OnDecision = context =>
            {
                if (context.RoundActions.Count() != 4)
                {
                    return;
                }

                checkedPosition = true;
                var simulator = new BelotSimulator();
                simulator.SetContract(context.CurrentContract.Type, context.CurrentContract.Player.Index(), 0);
                var knowledge = new RoundKnowledge();
                Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
                var sampler = new DeclaredWorldSampler();
                Assert.True(sampler.Configure(context, knowledge, simulator.Kind));
                var state = knowledge.Root;
                for (var seat = 0; seat < 4; seat++)
                {
                    state.Hands[seat] = Deal.Mask(hands[seat]);
                }

                Assert.True(sampler.TryScore(in state, out var southNorth, out var eastWest));
                Assert.Equal(0, southNorth);
                Assert.Equal(20, eastWest);

                // The old independent rank draw assigns our Q-high tierce a win for three
                // ranks, a tie for one, and a loss for two: +20/6 instead of the actual -20.
                var oldDifferenceSum = 0;
                for (var rank = 2; rank < 8; rank++)
                {
                    var announced = knowledge.Announces.Take(knowledge.AnnounceCount).ToArray();
                    for (var i = 0; i < announced.Length; i++)
                    {
                        if (announced[i].Rank < 0)
                        {
                            announced[i].Rank = rank;
                        }
                    }

                    AnnounceScorer.GetPoints(announced, announced.Length, out southNorth, out eastWest);
                    oldDifferenceSum += southNorth - eastWest;
                }

                Assert.Equal(20, oldDifferenceSum);
                var queen = 1u << Deal.Card("QD").GetHashCode();
                var ace = 1u << Deal.Card("AD").GetHashCode();
                state.Hands[1] ^= queen | ace;
                state.Hands[2] ^= queen | ace;
                Assert.False(sampler.TryScore(in state, out _, out _));

                var originalAnnounces = context.Announces;
                context.Announces = originalAnnounces.Where(x => x.Player != context.MyPosition).ToArray();
                Assert.False(sampler.Configure(context, knowledge, simulator.Kind));
                Assert.False(sampler.TrySample(ref state, new Random(4), out _, out _));
                context.Announces = originalAnnounces;
            };
            Deal.Play(hands, new Bid(PlayerPosition.South, BidType.AllTrumps), PlayerPosition.South, players);
            Assert.True(checkedPosition);
        }

        [Theory]
        [InlineData(BidType.Clubs, 0)]
        [InlineData(BidType.Diamonds, 0)]
        [InlineData(BidType.Hearts, 0)]
        [InlineData(BidType.Spades, 0)]
        [InlineData(BidType.NoTrumps, 0)]
        [InlineData(BidType.AllTrumps, 0)]
        [InlineData(BidType.AllTrumps, 2)]
        [InlineData(BidType.Clubs, 3)]
        public void ConditionedValuesMatchIndependentFilteringAndPublicViews(BidType contract, int finalTricks)
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(24397));
            var simulator = new BelotSimulator();
            var search = new NeuralSearch { UseDeclarations = true, DoubleDummyTricks = finalTricks };
            var positions = 0;
            var rejections = 0;
            PlayDeal(contract, 12971, (context, viewContext) =>
            {
                if (context.RoundActions.Count() < 4)
                {
                    return;
                }

                context.HangingPoints = viewContext.HangingPoints = 13;
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var random = new Random(4163);
                var referenceRandom = new Random(4163);
                var actual = Enumerable.Repeat(923f, 32).ToArray();
                var expected = Reference(context, in deal, legal, 3, finalTricks, evaluator, simulator, referenceRandom, out var accepted, out var attempts);
                Assert.Equal(accepted > 0, search.Evaluate(context, in deal, legal, 3, evaluator, simulator, random, actual));
                Assert.Equal(expected, actual);
                Assert.Equal(referenceRandom.Next(), random.Next());
                Assert.Equal(accepted, search.NeuralDealsCompleted);
                Assert.Equal(attempts, search.DeclarationSampleAttempts);
                Assert.Equal(attempts - accepted, search.DeclarationRejectedWorlds);
                rejections += search.DeclarationRejectedWorlds;

                Assert.True(NeuralDeal.FromPlayContext(viewContext, simulator, out var publicDeal));
                for (var seat = 0; seat < 4; seat++)
                {
                    if (seat != publicDeal.Play.Turn)
                    {
                        publicDeal.Play.Hands[seat] = uint.MaxValue;
                    }

                    publicDeal.LastThree[seat] = uint.MaxValue;
                }

                var viewValues = Enumerable.Repeat(923f, 32).ToArray();
                Assert.Equal(accepted > 0, search.Evaluate(viewContext, in publicDeal, legal, 3, evaluator, simulator, new Random(4163), viewValues));
                Assert.Equal(actual, viewValues);
                positions++;
            });
            Assert.True(positions > 4);
            if (contract != BidType.NoTrumps)
            {
                Assert.True(rejections > 0);
            }
        }

        [Fact]
        public void FirstTrickLeavesLegacyValuesAndRandomStreamUnchanged()
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(24763));
            var simulator = new BelotSimulator();
            var oldSearch = new NeuralSearch { DoubleDummyTricks = 2 };
            var conditioned = new NeuralSearch { DoubleDummyTricks = 2, UseDeclarations = true };
            var positions = 0;
            PlayDeal(BidType.AllTrumps, 13411, (context, _) =>
            {
                if (context.RoundActions.Count() >= 4)
                {
                    return;
                }

                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var firstRandom = new Random(4493);
                var secondRandom = new Random(4493);
                var expected = new float[32];
                var actual = new float[32];
                Assert.True(oldSearch.Evaluate(context, in deal, legal, 3, evaluator, simulator, firstRandom, expected));
                Assert.True(conditioned.Evaluate(context, in deal, legal, 3, evaluator, simulator, secondRandom, actual));
                Assert.Equal(expected, actual);
                Assert.Equal(firstRandom.Next(), secondRandom.Next());
                Assert.Equal(0, conditioned.DeclarationSampleAttempts);
                Assert.Equal(0, conditioned.DeclarationRejectedWorlds);
                positions++;
            });
            Assert.InRange(positions, 1, 4);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RejectionBudgetPreservesOutputsOrAveragesOnlyAcceptedWorlds(bool firstAccepted)
        {
            var evaluator = new NeuralEvaluator(RandomModels.Create(24781));
            var simulator = new BelotSimulator();
            var checkedPosition = false;
            PlayDeal(BidType.AllTrumps, 13829, (context, _) =>
            {
                if (checkedPosition || context.RoundActions.Count() < 4)
                {
                    return;
                }

                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                if (!FindWorldIndices(context, simulator, out var acceptedIndex, out var rejectedIndex))
                {
                    return;
                }

                checkedPosition = true;
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var search = new NeuralSearch { UseDeclarations = true, DoubleDummyTricks = 2 };
                var values = Enumerable.Repeat(923f, 32).ToArray();
                var random = new IndexedRandom(firstAccepted ? acceptedIndex : rejectedIndex, rejectedIndex);
                Assert.Equal(firstAccepted, search.Evaluate(context, in deal, legal, 3, evaluator, simulator, random, values));
                Assert.Equal(192, search.DeclarationSampleAttempts);
                Assert.Equal(firstAccepted ? 1 : 0, search.NeuralDealsCompleted);
                Assert.Equal(firstAccepted ? 191 : 192, search.DeclarationRejectedWorlds);
                if (firstAccepted)
                {
                    var oneWorld = new NeuralSearch { UseDeclarations = true, DoubleDummyTricks = 2 };
                    var expected = Enumerable.Repeat(923f, 32).ToArray();
                    Assert.True(oneWorld.Evaluate(context, in deal, legal, 1, evaluator, simulator, new IndexedRandom(acceptedIndex, acceptedIndex), expected));
                    Assert.Equal(expected, values);
                }
                else
                {
                    Assert.All(values, value => Assert.Equal(923f, value));
                    search.TimeLimitMilliseconds = 1;
                    search.MinimumDeals = 8;
                    Assert.False(search.Evaluate(context, in deal, legal, 10000, evaluator, simulator, new IndexedRandom(rejectedIndex, rejectedIndex, delay: true), values));
                    Assert.Equal(1, search.DeclarationSampleAttempts);
                    Assert.All(values, value => Assert.Equal(923f, value));
                }
            });
            Assert.True(checkedPosition);
        }

        [Fact]
        public void DeclarationConditioningRejectsControlVariates()
        {
            var search = new NeuralSearch { UseDeclarations = true, ControlVariateDeals = 4 };
            var deal = default(NeuralDeal);
            Assert.Throws<InvalidOperationException>(() => search.Evaluate(null, in deal, 3, 2, null, null, new Random(1), new float[32]));
        }

        private static void PlayDeal(BidType contract, int seed, Action<PlayerPlayCardContext, PlayerPlayCardContext> check)
        {
            var smart = new SmartPlayer.SmartPlayer();
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed) });
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

                    var bid = context.CurrentContract.Type == BidType.Pass ? contract : BidType.Pass;
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Bid(bid)));
                }
                else if (match.Decision == BelotDecision.Announce)
                {
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces)));
                }
                else
                {
                    var context = match.CreatePlayCardContext();
                    check(context, match.GetView(seat).CreatePlayCardContext());
                    var action = smart.PlayCard(context);
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote)));
                }
            }
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
            out int accepted,
            out int attempts)
        {
            var knowledge = new RoundKnowledge();
            Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
            var sampler = new UniformWorldSampler();
            Assert.True(sampler.Configure(knowledge));
            var sums = new double[32];
            var values = new float[32];
            var team = deal.Play.Turn & 1;
            accepted = 0;
            attempts = 0;
            while (accepted < worlds && attempts < 64 * worlds)
            {
                attempts++;
                var state = knowledge.Root;
                sampler.Sample(ref state, random);
                if (!TryPoints(knowledge, in state, simulator.Kind, out var southNorth, out var eastWest))
                {
                    continue;
                }

                var world = deal;
                world.Play = state;
                world.SouthNorthAnnounces = southNorth;
                world.EastWestAnnounces = eastWest;
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var copy = world;
                    copy.PlayCard(simulator, card, legal);
                    while (!copy.IsFinished && (finalTricks == 0 || copy.Play.TricksPlayed < 8 - finalTricks))
                    {
                        var moves = simulator.LegalMoves(in copy.Play);
                        var move = (moves & (moves - 1)) == 0
                            ? BitOperations.TrailingZeroCount(moves)
                            : evaluator.BestCard(in copy, moves, values);
                        copy.PlayCard(simulator, move, moves);
                    }

                    sums[card] += EndgameSearch.Solve(in copy.Play, simulator, team, southNorth, eastWest);
                }

                accepted++;
            }

            var result = Enumerable.Repeat(923f, 32).ToArray();
            if (accepted > 0)
            {
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    result[card] = (float)(sums[card] / accepted);
                }
            }

            return result;
        }

        private static bool TryPoints(RoundKnowledge knowledge, in SimState state, int kind, out int southNorth, out int eastWest)
        {
            var actual = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
            var count = 0;
            if (kind != SimTables.NoTrumps)
            {
                for (var seat = 0; seat < 4; seat++)
                {
                    count = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat] | knowledge.PlayedBy[seat], seat, actual, count);
                }
            }

            var expected = knowledge.Announces.Take(knowledge.AnnounceCount).ToArray();
            var expectedTypes = expected.Select(x => (x.Seat, x.Type)).OrderBy(x => x.Seat).ThenBy(x => x.Type);
            var actualTypes = actual.Take(count).Select(x => (x.Seat, x.Type)).OrderBy(x => x.Seat).ThenBy(x => x.Type);
            var match = expectedTypes.SequenceEqual(actualTypes)
                && expected.Where(x => x.Rank >= 0).All(x => actual.Take(count).Any(y => x.Seat == y.Seat && x.Type == y.Type && x.Rank == y.Rank));
            AnnounceScorer.GetPoints(actual, count, out southNorth, out eastWest);
            return match;
        }

        private static bool FindWorldIndices(PlayerPlayCardContext context, BelotSimulator simulator, out long accepted, out long rejected)
        {
            var knowledge = new RoundKnowledge();
            Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
            var sampler = new UniformWorldSampler();
            Assert.True(sampler.Configure(knowledge));
            var random = new Random(4931);
            accepted = rejected = -1;
            for (var i = 0; i < 1024 && (accepted < 0 || rejected < 0); i++)
            {
                var index = random.NextInt64((long)sampler.WorldCount);
                var state = knowledge.Root;
                sampler.SampleAt(ref state, (ulong)index);
                if (TryPoints(knowledge, in state, simulator.Kind, out _, out _))
                {
                    accepted = index;
                }
                else
                {
                    rejected = index;
                }
            }

            return accepted >= 0 && rejected >= 0;
        }

        private sealed class IndexedRandom : Random
        {
            private readonly long first;
            private readonly long following;
            private readonly bool delay;
            private bool sampled;

            public IndexedRandom(long first, long following, bool delay = false)
            {
                this.first = first;
                this.following = following;
                this.delay = delay;
            }

            public override long NextInt64(long maxValue)
            {
                var index = this.sampled ? this.following : this.first;
                if (!this.sampled && this.delay)
                {
                    Thread.Sleep(5);
                }

                this.sampled = true;
                Assert.InRange(index, 0, maxValue - 1);
                return index;
            }
        }
    }
}
