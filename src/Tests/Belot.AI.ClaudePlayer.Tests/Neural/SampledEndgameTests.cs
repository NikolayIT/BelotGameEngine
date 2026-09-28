namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class SampledEndgameTests
    {
        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(5, false)]
        [InlineData(6, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        [InlineData(5, true)]
        [InlineData(6, true)]
        public void FourTrickSamplesMatchExhaustivePlayAndPublicViews(int contractIndex, bool pruneEquivalentCards)
        {
            var simulator = new BelotSimulator();
            var checkedPositions = 0;
            using var views = Positions(contractIndex, 4, fromView: true).GetEnumerator();
            foreach (var context in Positions(contractIndex, 4).Take(3))
            {
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var search = new EndgameSearch { Tricks = 4, UseDeclarations = true, SampledWorlds = 8, PruneEquivalentCards = pruneEquivalentCards };
                var values = new float[32];
                Assert.True(search.Evaluate(context, in deal, legal, simulator, values, new Random(147)));
                Assert.Equal(8, search.Worlds);
                var expected = ReferenceSamples(context, simulator, legal, 8, 147, out _);
                Assert.Equal(expected, values);
                Assert.True(views.MoveNext());
                Assert.True(NeuralDeal.FromPlayContext(views.Current, simulator, out var viewDeal));
                var viewValues = new float[32];
                Assert.True(search.Evaluate(views.Current, in viewDeal, legal, simulator, viewValues, new Random(147)));
                Assert.Equal(values, viewValues);

                for (var seat = 0; seat < 4; seat++)
                {
                    if (seat != context.MyPosition.Index())
                    {
                        deal.Play.Hands[seat] = uint.MaxValue;
                    }
                }

                var altered = new float[32];
                Assert.True(search.Evaluate(context, in deal, legal, simulator, altered, new Random(147)));
                Assert.Equal(values, altered);
                checkedPositions++;
            }

            Assert.Equal(3, checkedPositions);
        }

        [Fact]
        public void DeclarationRejectionMatchesIndependentMeldFiltering()
        {
            var simulator = new BelotSimulator();
            var rejected = 0;
            var emptySamples = 0;
            foreach (var context in Positions(6, 4).Take(25))
            {
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var search = new EndgameSearch { Tricks = 4, UseDeclarations = true, SampledWorlds = 4 };
                var values = Enumerable.Repeat(923f, 32).ToArray();
                var expected = ReferenceSamples(context, simulator, legal, 4, 177, out var positionRejected);
                var evaluated = search.Evaluate(context, in deal, legal, simulator, values, new Random(177));
                if (expected == null)
                {
                    Assert.False(evaluated);
                    Assert.Equal(256, positionRejected);
                    Assert.Equal(0, search.Worlds);
                    Assert.Equal(0, search.Nodes);
                    Assert.All(values, value => Assert.Equal(923f, value));
                    emptySamples++;
                }
                else
                {
                    Assert.True(evaluated);
                    for (var rest = legal; rest != 0; rest &= rest - 1)
                    {
                        var card = BitOperations.TrailingZeroCount(rest);
                        Assert.Equal(expected[card], values[card]);
                    }
                }

                rejected += positionRejected;
            }

            Assert.True(rejected > 0, "The test must exercise rejection of worlds inconsistent with declarations.");
            Assert.True(emptySamples > 0, "The test must exercise a bounded rejection batch with no matching world.");
        }

        [Fact]
        public void NodeCutoffCommitsOnlyCompleteSharedWorlds()
        {
            var context = Positions(5, 4).First();
            var simulator = new BelotSimulator();
            Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var search = new EndgameSearch { Tricks = 4, UseDeclarations = true, SampledWorlds = 1 };
            var firstWorld = new float[32];
            Assert.True(search.Evaluate(context, in deal, legal, simulator, firstWorld, new Random(819)));
            var firstNodes = search.Nodes;
            Assert.True(firstNodes > 1);
            search.SampledWorlds = 10;
            search.NodeLimit = firstNodes + 1;
            var cutoff = new float[32];
            Assert.True(search.Evaluate(context, in deal, legal, simulator, cutoff, new Random(819)));
            Assert.Equal(1, search.Worlds);
            Assert.Equal(firstNodes + 1, search.Nodes);
            Assert.Equal(firstWorld, cutoff);

            search.NodeLimit = 1;
            var untouched = Enumerable.Repeat(923f, 32).ToArray();
            Assert.False(search.Evaluate(context, in deal, legal, simulator, untouched, new Random(819)));
            Assert.Equal(0, search.Worlds);
            Assert.Equal(1, search.Nodes);
            Assert.All(untouched, value => Assert.Equal(923f, value));
        }

        [Fact]
        public void ExactWorldsAreNotPartiallyAveragedAfterNodeCutoff()
        {
            var context = Positions(5, 3).First();
            var simulator = new BelotSimulator();
            Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var search = new EndgameSearch { Tricks = 3, ThreeTrickWorldLimit = 1680, UseDeclarations = true };
            var complete = new float[32];
            Assert.True(search.Evaluate(context, in deal, legal, simulator, complete));
            Assert.True(search.Nodes > 1);
            search.NodeLimit = search.Nodes - 1;
            var untouched = Enumerable.Repeat(923f, 32).ToArray();
            Assert.False(search.Evaluate(context, in deal, legal, simulator, untouched));
            Assert.Equal(0, search.Worlds);
            Assert.All(untouched, value => Assert.Equal(923f, value));
        }

        [Fact]
        public void ThreeTrickOverflowSamplesWithoutUsingEnumerationPrefix()
        {
            var simulator = new BelotSimulator();
            var searched = false;
            foreach (var context in Positions(5, 3).Take(20))
            {
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var search = new EndgameSearch { Tricks = 3, ThreeTrickWorldLimit = 1, UseDeclarations = true };
                var untouched = Enumerable.Repeat(923f, 32).ToArray();
                if (search.Evaluate(context, in deal, legal, simulator, untouched))
                {
                    continue;
                }

                Assert.All(untouched, value => Assert.Equal(923f, value));
                search.SampledWorlds = 8;
                var values = new float[32];
                Assert.True(search.Evaluate(context, in deal, legal, simulator, values, new Random(213)));
                Assert.Equal(8, search.Worlds);
                Assert.Equal(ReferenceSamples(context, simulator, legal, 8, 213, out _), values);
                searched = true;
                break;
            }

            Assert.True(searched);
        }

        [Fact]
        public void FourTricksWithoutSamplingLeaveValuesUnchanged()
        {
            var context = Positions(5, 4).First();
            var simulator = new BelotSimulator();
            Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var search = new EndgameSearch { Tricks = 4, UseDeclarations = true };
            var untouched = Enumerable.Repeat(923f, 32).ToArray();
            Assert.False(search.Evaluate(context, in deal, legal, simulator, untouched, new Random(213)));
            Assert.All(untouched, value => Assert.Equal(923f, value));
            Assert.Equal(0, search.Nodes);
        }

        [Fact]
        public void FiveTrickTimeBudgetStopsBeforeAllRequestedWorlds()
        {
            var context = Positions(5, 5).First();
            var simulator = new BelotSimulator();
            Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var search = new EndgameSearch { Tricks = 5, UseDeclarations = true, SampledWorlds = 10000, TimeLimitMilliseconds = 1 };
            var values = Enumerable.Repeat(923f, 32).ToArray();
            var watch = Stopwatch.StartNew();
            var evaluated = search.Evaluate(context, in deal, legal, simulator, values, new Random(147));
            Assert.InRange(search.Worlds, 0, 9999);
            Assert.True(watch.ElapsedMilliseconds < 5000, "A one-millisecond search must return without finishing an unbounded ending.");
            if (!evaluated)
            {
                Assert.Equal(0, search.Worlds);
                Assert.All(values, value => Assert.Equal(923f, value));
            }
            else
            {
                Assert.True(search.Worlds > 0);
                Assert.All(values, value => Assert.True(float.IsFinite(value)));
            }
        }

        private static IEnumerable<PlayerPlayCardContext> Positions(int contractIndex, int tricksRemaining, bool fromView = false)
        {
            var smart = new SmartPlayer.SmartPlayer();
            for (var seed = 0; seed < 3; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(1521 + seed + (contractIndex * 17)) });
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    if (match.Decision == BelotDecision.Bid)
                    {
                        var context = match.CreateBidContext();
                        var bid = context.CurrentContract.Type == BidType.Pass ? FeatureEncoder.BidOfIndex(contractIndex) : BidType.Pass;
                        match.Act(seat, BelotAction.Bid(bid));
                    }
                    else if (match.Decision == BelotDecision.Announce)
                    {
                        match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces));
                    }
                    else
                    {
                        var context = match.CreatePlayCardContext();
                        if (context.RoundActions.Count() / 4 == 8 - tricksRemaining)
                        {
                            yield return fromView ? match.GetView(seat).CreatePlayCardContext() : context;
                        }

                        var action = smart.PlayCard(context);
                        match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote));
                    }
                }
            }
        }

        private static float[] ReferenceSamples(PlayerPlayCardContext context, BelotSimulator simulator, uint legal, int sampleCount, int seed, out int rejected)
        {
            var knowledge = new RoundKnowledge();
            Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
            var sampler = new UniformWorldSampler();
            Assert.True(sampler.Configure(knowledge));
            var random = new Random(seed);
            var sums = new double[32];
            var observed = knowledge.Announces.Take(knowledge.AnnounceCount).ToArray();
            var observedTypes = observed.Select(x => (x.Seat, x.Type)).OrderBy(x => x.Seat).ThenBy(x => x.Type).ToArray();
            var accepted = 0;
            rejected = 0;
            while (accepted < sampleCount && accepted + rejected < Math.Max(64, sampleCount * 64))
            {
                var state = knowledge.Root;
                sampler.Sample(ref state, random);
                var announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
                var count = 0;
                if (simulator.Kind != SimTables.NoTrumps)
                {
                    for (var seat = 0; seat < 4; seat++)
                    {
                        count = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat] | knowledge.PlayedBy[seat], seat, announces, count);
                    }
                }

                var actual = announces.Take(count).ToArray();
                var types = actual.Select(x => (x.Seat, x.Type)).OrderBy(x => x.Seat).ThenBy(x => x.Type);
                if (!observedTypes.SequenceEqual(types)
                    || observed.Any(x => x.Rank >= 0 && !actual.Any(y => y.Seat == x.Seat && y.Type == x.Type && y.Rank == x.Rank)))
                {
                    rejected++;
                    continue;
                }

                AnnounceScorer.GetPoints(announces, count, out var southNorth, out var eastWest);
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var copy = state;
                    simulator.Play(ref copy, card, legal);
                    sums[card] += Exhaustive(in copy, simulator, knowledge.Me & 1, southNorth, eastWest);
                }

                accepted++;
            }

            if (accepted == 0)
            {
                return null;
            }

            var values = new float[32];
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                values[card] = (float)(sums[card] / accepted);
            }

            return values;
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
    }
}
