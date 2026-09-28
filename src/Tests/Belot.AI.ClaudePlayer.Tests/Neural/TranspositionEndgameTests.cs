namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class TranspositionEndgameTests
    {
        [Theory]
        [InlineData(1, 4, 0)]
        [InlineData(1, 5, 1)]
        [InlineData(2, 4, 1)]
        [InlineData(2, 5, 2)]
        [InlineData(3, 4, 2)]
        [InlineData(3, 5, 0)]
        [InlineData(4, 4, 0)]
        [InlineData(4, 5, 1)]
        [InlineData(5, 4, 1)]
        [InlineData(5, 5, 2)]
        [InlineData(6, 4, 2)]
        [InlineData(6, 5, 0)]
        public void FixedWorldValuesMatchWithoutTranspositionsAcrossContracts(int contract, int tricks, int doubling)
        {
            var cached = Search(tricks, true);
            var plain = Search(tricks, false);
            var compared = 0;
            foreach (var context in Positions(contract, tricks, doubling).Take(3))
            {
                context.HangingPoints = 17;
                var simulator = new BelotSimulator();
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var actual = Enumerable.Repeat(923f, 32).ToArray();
                var expected = Enumerable.Repeat(923f, 32).ToArray();
                var cachedRandom = new Random(137);
                var plainRandom = new Random(137);
                Assert.Equal(
                    plain.Evaluate(context, in deal, legal, simulator, expected, plainRandom),
                    cached.Evaluate(context, in deal, legal, simulator, actual, cachedRandom));
                Assert.Equal(expected, actual);
                Assert.Equal(plain.Worlds, cached.Worlds);
                Assert.Equal(plain.SampleAttempts, cached.SampleAttempts);
                Assert.Equal(plainRandom.NextInt64(), cachedRandom.NextInt64());
                Assert.InRange(cached.TranspositionHits, 0, cached.TranspositionProbes);
                Assert.InRange(cached.TranspositionCutoffs, 0, cached.TranspositionHits);
                Assert.Equal(0, plain.TranspositionProbes);
                if (cached.Worlds > 0)
                {
                    Assert.True(cached.TranspositionProbes > 0);
                    compared++;
                }
            }

            Assert.True(compared > 0);
        }

        [Fact]
        public void GenerationChangesPreserveFreshSearchResultsAndResetMetrics()
        {
            var reused = Search(5, true);
            var hits = 0;
            var cutoffs = 0;
            foreach (var context in Positions(5, 5, 0).Take(3))
            {
                foreach (var hanging in new[] { 0, 29 })
                {
                    context.HangingPoints = hanging;
                    var simulator = new BelotSimulator();
                    Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                    var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                    var fresh = Search(5, true);
                    var actual = new float[32];
                    var expected = new float[32];
                    Assert.True(reused.Evaluate(context, in deal, legal, simulator, actual, new Random(953)));
                    Assert.True(fresh.Evaluate(context, in deal, legal, simulator, expected, new Random(953)));
                    Assert.Equal(expected, actual);
                    Assert.Equal(fresh.Nodes, reused.Nodes);
                    Assert.Equal(fresh.TranspositionProbes, reused.TranspositionProbes);
                    Assert.Equal(fresh.TranspositionHits, reused.TranspositionHits);
                    Assert.Equal(fresh.TranspositionCutoffs, reused.TranspositionCutoffs);
                    hits += reused.TranspositionHits;
                    cutoffs += reused.TranspositionCutoffs;
                }
            }

            Assert.True(hits > 0);
            Assert.True(cutoffs > 0);
            var last = Positions(5, 5, 0).First();
            var lastSimulator = new BelotSimulator();
            Assert.True(NeuralDeal.FromPlayContext(last, lastSimulator, out var lastDeal));
            var lastLegal = NeuralDeal.ToMask(last.AvailableCardsToPlay);
            reused.UseTranspositions = false;
            Assert.True(reused.Evaluate(last, in lastDeal, lastLegal, lastSimulator, new float[32], new Random(953)));
            Assert.Equal(0, reused.TranspositionProbes);
            Assert.Equal(0, reused.TranspositionHits);
            Assert.Equal(0, reused.TranspositionCutoffs);
        }

        [Fact]
        public void InterruptedSearchLeavesValuesUntouchedAndCanBeReused()
        {
            var context = Positions(5, 5, 0).First();
            var simulator = new BelotSimulator();
            Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var search = Search(5, true);
            search.NodeLimit = 1;
            var values = Enumerable.Repeat(923f, 32).ToArray();
            Assert.False(search.Evaluate(context, in deal, legal, simulator, values, new Random(631)));
            Assert.Equal(0, search.Worlds);
            Assert.Equal(1, search.IncompleteWorlds);
            Assert.Equal(1, search.Nodes);
            Assert.All(values, value => Assert.Equal(923f, value));
            search.NodeLimit = 0;
            var fresh = Search(5, true);
            var expected = Enumerable.Repeat(923f, 32).ToArray();
            Assert.True(search.Evaluate(context, in deal, legal, simulator, values, new Random(631)));
            Assert.True(fresh.Evaluate(context, in deal, legal, simulator, expected, new Random(631)));
            Assert.Equal(expected, values);
            Assert.Equal(fresh.Nodes, search.Nodes);
            Assert.Equal(fresh.TranspositionHits, search.TranspositionHits);
        }

        [Fact]
        public void ExactKeysIncludeEveryScoringFieldAndIgnoreOnlyStaleTrickFields()
        {
            var state = Boundary();
            var original = new EndgameSearch.TranspositionKey(in state, 20, 50);
            for (var field = 0; field < 10; field++)
            {
                var changed = state;
                switch (field)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        changed.Hands[field] ^= 0x100u;
                        break;
                    case 4:
                        changed.Turn++;
                        break;
                    case 5:
                        changed.TricksPlayed++;
                        break;
                    case 6:
                        changed.SouthNorthPoints++;
                        break;
                    case 7:
                        changed.EastWestPoints++;
                        break;
                    case 8:
                        changed.SouthNorthTricks++;
                        break;
                    default:
                        changed.EastWestTricks++;
                        break;
                }

                var key = new EndgameSearch.TranspositionKey(in changed, 20, 50);
                Assert.False(original.Matches(in key));
            }

            var differentSouthNorth = new EndgameSearch.TranspositionKey(in state, 21, 50);
            var differentEastWest = new EndgameSearch.TranspositionKey(in state, 20, 51);
            Assert.False(original.Matches(in differentSouthNorth));
            Assert.False(original.Matches(in differentEastWest));
            state.LedSuit = 3;
            state.WinnerSeat = 3;
            state.WinnerCard = 31;
            state.TrickPoints = 50;
            state.LastTrickTeam = 1;
            var stale = new EndgameSearch.TranspositionKey(in state, 20, 50);
            Assert.True(original.Matches(in stale));
            Assert.Equal(original.Slot(), stale.Slot());
        }

        [Fact]
        public void HashCollisionDoesNotMakeDifferentPositionsMatch()
        {
            var bySlot = new Dictionary<int, EndgameSearch.TranspositionKey>();
            var state = Boundary();
            var collision = false;
            for (var points = 0; points <= 8192; points++)
            {
                state.SouthNorthPoints = points;
                var key = new EndgameSearch.TranspositionKey(in state, 20, 50);
                var slot = key.Slot();
                Assert.InRange(slot, 0, 8191);
                if (bySlot.TryGetValue(slot, out var previous))
                {
                    Assert.False(previous.Matches(in key));
                    collision = true;
                    break;
                }

                bySlot.Add(slot, key);
            }

            Assert.True(collision);
        }

        private static EndgameSearch Search(int tricks, bool useTranspositions) => new EndgameSearch
        {
            Tricks = tricks,
            UseDeclarations = true,
            SampledWorlds = 8,
            PruneEquivalentCards = true,
            UseTranspositions = useTranspositions,
        };

        private static SimState Boundary()
        {
            var state = default(SimState);
            state.Hands[0] = 3;
            state.Hands[1] = 12;
            state.Hands[2] = 48;
            state.Hands[3] = 192;
            state.TricksPlayed = 6;
            state.SouthNorthPoints = 62;
            state.EastWestPoints = 30;
            state.SouthNorthTricks = 4;
            state.EastWestTricks = 2;
            return state;
        }

        private static IEnumerable<PlayerPlayCardContext> Positions(int contract, int tricks, int doubling)
        {
            var smart = new SmartPlayer.SmartPlayer();
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(1209 + (contract * 11) + tricks) });
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                if (match.Decision == BelotDecision.Bid)
                {
                    var context = match.CreateBidContext();
                    var bid = BidType.Pass;
                    if (context.CurrentContract.Type == BidType.Pass)
                    {
                        bid = FeatureEncoder.BidOfIndex(contract);
                    }
                    else if (doubling > 0 && context.AvailableBids.HasFlag(BidType.Double))
                    {
                        bid = BidType.Double;
                    }
                    else if (doubling > 1 && context.AvailableBids.HasFlag(BidType.ReDouble))
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
                    if (context.RoundActions.Count() / 4 == 8 - tricks)
                    {
                        yield return context;
                    }

                    var action = smart.PlayCard(context);
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote)));
                }
            }
        }
    }
}
