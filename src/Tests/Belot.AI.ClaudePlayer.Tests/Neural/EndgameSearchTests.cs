namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class EndgameSearchTests
    {
        [Fact]
        public void AlphaBetaMatchesFullEnumerationAcrossContractsAndTeams()
        {
            var random = new Random(971);
            var simulator = new BelotSimulator();
            for (var round = 0; round < 120; round++)
            {
                var deal = NeuralDeal.Deal(Enumerable.Range(0, 32).OrderBy(_ => random.Next()).ToArray(), round % 4, round % 20);
                deal.Bid(FeatureEncoder.BidOfIndex((round % 6) + 1));
                if ((round / 6) % 3 > 0)
                {
                    Assert.True(deal.AvailableBids().HasFlag(BidType.Double));
                    deal.Bid(BidType.Double);
                    if ((round / 6) % 3 == 2)
                    {
                        Assert.True(deal.AvailableBids().HasFlag(BidType.ReDouble));
                        deal.Bid(BidType.ReDouble);
                    }
                }

                while (!deal.AuctionFinished)
                {
                    deal.Bid(FeatureEncoder.BidOfIndex(0));
                }

                deal.StartPlay(simulator, new DeclaredAnnounce[AnnounceScorer.MaxAnnounces]);
                var cards = 20 + (round % 11);
                for (var play = 0; play < cards; play++)
                {
                    deal.DeclareIfFirstCard();
                    var legal = simulator.LegalMoves(in deal.Play);
                    var candidates = Enumerable.Range(0, 32).Where(card => (legal & (1u << card)) != 0).ToArray();
                    deal.PlayCard(simulator, candidates[random.Next(candidates.Length)], legal);
                }

                for (var team = 0; team < 2; team++)
                {
                    var expected = Reference(in deal.Play, simulator, team, deal.SouthNorthAnnounces, deal.EastWestAnnounces);
                    Assert.Equal(expected, EndgameSearch.Solve(in deal.Play, simulator, team, deal.SouthNorthAnnounces, deal.EastWestAnnounces));
                }
            }
        }

        [Theory]
        [InlineData(2, false, 8)]
        [InlineData(2, true, 8)]
        [InlineData(3, true, 8)]
        [InlineData(3, true, 32)]
        [InlineData(3, true, 90)]
        public void PublicWorldsAndValuesMatchIndependentAssignmentsAndIgnoreSecretHands(int tricks, bool declarations, int worldLimit)
        {
            var checkedPositions = 0;
            var multipleWorlds = 0;
            var earlyPositions = 0;
            var checkedOverflow = false;
            var simulator = new BelotSimulator();
            var search = new EndgameSearch { UseDeclarations = declarations, Tricks = tricks, ThreeTrickWorldLimit = worldLimit };
            var smart = new SmartPlayer.SmartPlayer();
            for (var seed = 0; seed < 8; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed + 157) });
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    if (match.Decision == BelotDecision.Bid)
                    {
                        match.Act(seat, BelotAction.Bid(smart.GetBid(match.CreateBidContext())));
                    }
                    else if (match.Decision == BelotDecision.Announce)
                    {
                        match.Act(seat, BelotAction.Declare(smart.GetAnnounces(match.CreateAnnouncesContext())));
                    }
                    else
                    {
                        var context = match.CreatePlayCardContext();
                        Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                        var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                        var actual = new float[32];
                        if (checkedPositions < 40 && search.Evaluate(context, in deal, legal, simulator, actual))
                        {
                            var knowledge = new RoundKnowledge();
                            Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
                            var expected = EnumerateAssignments(knowledge, simulator, legal, declarations, out var worlds);
                            Assert.Equal(worlds, search.Worlds);
                            Assert.InRange(worlds, 1, deal.Play.TricksPlayed < 6 ? worldLimit : 90);
                            Assert.Equal(expected, actual);
                            multipleWorlds += worlds > 1 ? 1 : 0;
                            earlyPositions += deal.Play.TricksPlayed == 5 ? 1 : 0;
                            for (var other = 0; other < 4; other++)
                            {
                                if (other != knowledge.Me)
                                {
                                    deal.Play.Hands[other] = uint.MaxValue;
                                }
                            }

                            var altered = new float[32];
                            Assert.True(search.Evaluate(context, in deal, legal, simulator, altered));
                            Assert.Equal(actual, altered);
                            checkedPositions++;
                        }
                        else if (tricks == 3 && deal.Play.TricksPlayed == 5 && !checkedOverflow)
                        {
                            var knowledge = new RoundKnowledge();
                            Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
                            EnumerateAssignments(knowledge, simulator, legal, declarations, out var worlds, onlyCount: true);
                            if (worlds > worldLimit)
                            {
                                var untouched = Enumerable.Repeat(9f, 32).ToArray();
                                Assert.False(search.Evaluate(context, in deal, legal, simulator, untouched));
                                Assert.All(untouched, value => Assert.Equal(9f, value));
                                checkedOverflow = true;
                            }
                        }

                        var action = smart.PlayCard(context);
                        match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote));
                    }
                }
            }

            Assert.True(checkedPositions >= 20, $"Only {checkedPositions} endings checked.");
            Assert.True(multipleWorlds > 0);
            if (tricks == 3)
            {
                Assert.True(earlyPositions > 0);
                Assert.True(checkedOverflow);
            }
        }

        [Fact]
        public void AWithheldOwnDeclarationFallsBackWithoutValuingAnUndeclaredMeld()
        {
            var checkedPositions = 0;
            var simulator = new BelotSimulator();
            var search = new EndgameSearch { UseDeclarations = true, Tricks = 3, ThreeTrickWorldLimit = 90 };
            var smart = new SmartPlayer.SmartPlayer();
            for (var seed = 0; seed < 4; seed++)
            {
                var withheldRound = -1;
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed + 421) });
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    if (match.Decision == BelotDecision.Bid)
                    {
                        match.Act(seat, BelotAction.Bid(smart.GetBid(match.CreateBidContext())));
                    }
                    else if (match.Decision == BelotDecision.Announce)
                    {
                        var context = match.CreateAnnouncesContext();
                        if (seat.Index() == 0 && context.AvailableAnnounces.Count > 0)
                        {
                            withheldRound = context.RoundNumber;
                            match.Act(seat, BelotAction.Declare(Array.Empty<Announce>()));
                        }
                        else
                        {
                            match.Act(seat, BelotAction.Declare(smart.GetAnnounces(context)));
                        }
                    }
                    else
                    {
                        var context = match.CreatePlayCardContext();
                        Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                        if (seat.Index() == 0 && context.RoundNumber == withheldRound && deal.Play.TricksPlayed >= 5)
                        {
                            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                            var values = Enumerable.Repeat(9f, 32).ToArray();
                            Assert.False(search.Evaluate(context, in deal, legal, simulator, values));
                            Assert.All(values, value => Assert.Equal(9f, value));
                            checkedPositions++;
                        }

                        var action = smart.PlayCard(context);
                        match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote));
                    }
                }
            }

            Assert.True(checkedPositions > 0);
        }

        [Fact]
        public void PublicEndgameHorizonIsBounded()
        {
            var player = new ClaudePlayerNeural();
            Assert.Equal(2, player.EndgameTricks);
            Assert.Equal(8, player.EndgameThreeTrickWorldLimit);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgameThreeTrickWorldLimit = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgameThreeTrickWorldLimit = 1681);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgameTricks = 6);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgameTricks = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgameSampledWorlds = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgameNodeLimit = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgameTimeLimitMilliseconds = -1);
        }

        // Independent ternary assignment of each unseen card, followed by explicit filtering.
        private static float[] EnumerateAssignments(RoundKnowledge knowledge, BelotSimulator simulator, uint legal, bool declarations, out int worlds, bool onlyCount = false)
        {
            var unseen = ~(knowledge.MyHand | knowledge.Played);
            var cards = Enumerable.Range(0, 32).Where(card => (unseen & (1u << card)) != 0).ToArray();
            var possibilities = (int)Math.Pow(3, cards.Length);
            var sums = new double[32];
            worlds = 0;
            var observed = knowledge.Announces.Take(knowledge.AnnounceCount).ToArray();
            var observedTypes = observed.Select(x => (x.Seat, x.Type)).OrderBy(x => x.Seat).ThenBy(x => x.Type).ToArray();
            var northSouth = 0;
            var eastWest = 0;
            if (!declarations)
            {
                AnnounceScorer.GetPoints(knowledge.Announces, knowledge.AnnounceCount, out northSouth, out eastWest);
            }

            for (var assignment = 0; assignment < possibilities; assignment++)
            {
                var world = knowledge.Root;
                var code = assignment;
                for (var i = 0; i < cards.Length; i++)
                {
                    world.Hands[(knowledge.Me + 1 + (code % 3)) & 3] |= 1u << cards[i];
                    code /= 3;
                }

                var valid = true;
                for (var seat = 0; seat < 4; seat++)
                {
                    valid &= BitOperations.PopCount(world.Hands[seat]) == knowledge.HandCounts[seat]
                        && (world.Hands[seat] & knowledge.Excluded[seat]) == 0
                        && (world.Hands[seat] & knowledge.Known[seat]) == knowledge.Known[seat];
                }

                if (!valid)
                {
                    continue;
                }

                if (declarations)
                {
                    var announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
                    var count = 0;
                    if (simulator.Kind != SimTables.NoTrumps)
                    {
                        for (var seat = 0; seat < 4; seat++)
                        {
                            count = AnnounceScorer.AddDeclaredCombinations(world.Hands[seat] | knowledge.PlayedBy[seat], seat, announces, count);
                        }
                    }

                    var actual = announces.Take(count).ToArray();
                    var types = actual.Select(x => (x.Seat, x.Type)).OrderBy(x => x.Seat).ThenBy(x => x.Type);
                    if (!observedTypes.SequenceEqual(types)
                        || observed.Any(x => x.Rank >= 0 && !actual.Any(y => y.Seat == x.Seat && y.Type == x.Type && y.Rank == x.Rank)))
                    {
                        continue;
                    }

                    AnnounceScorer.GetPoints(announces, count, out northSouth, out eastWest);
                }

                worlds++;
                if (onlyCount)
                {
                    continue;
                }

                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var copy = world;
                    simulator.Play(ref copy, card, legal);
                    sums[card] += Reference(in copy, simulator, knowledge.Me & 1, northSouth, eastWest);
                }
            }

            var result = new float[32];
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                result[card] = (float)(sums[card] / worlds);
            }

            return result;
        }

        private static int Reference(in SimState state, BelotSimulator simulator, int team, int northSouth, int eastWest)
        {
            if (state.TricksPlayed == 8)
            {
                simulator.Score(in state, northSouth, eastWest, out var first, out var second, out _);
                return team == 0 ? first - second : second - first;
            }

            var best = (state.Turn & 1) == team ? int.MinValue : int.MaxValue;
            var legal = simulator.LegalMoves(in state);
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var copy = state;
                simulator.Play(ref copy, BitOperations.TrailingZeroCount(rest), legal);
                var value = Reference(in copy, simulator, team, northSouth, eastWest);
                best = (state.Turn & 1) == team ? Math.Max(best, value) : Math.Min(best, value);
            }

            return best;
        }
    }
}
