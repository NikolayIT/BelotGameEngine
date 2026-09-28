namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.NeuralTrainer;

    using Xunit;

    public class TrainingOpponentTests
    {
        [Theory]
        [InlineData("smart")]
        [InlineData("sharpbelot")]
        [InlineData("belot206")]
        [InlineData("neural")]
        public void PpoRecordsOnlyTheLearningTeamAndReplaysDeterministically(string name)
        {
            var opponents = new[] { OpponentCatalog.Factory(name) };
            var first = new PpoRecording.Worker(NeuralModels.Embedded, 81231, 1, opponents, 1);
            var copy = new PpoRecording.Worker(NeuralModels.Embedded, 81231, 1, opponents, 1);
            var masks = new HashSet<int>();
            for (var deal = 0; deal < 40; deal++)
            {
                var starts = first.Samples.Select(samples => samples.Count).ToArray();
                first.PlayDeal(deal);
                copy.PlayDeal(deal);
                masks.Add(first.LearnerMask);
                for (var kind = 0; kind < 3; kind++)
                {
                    foreach (var sample in first.Samples[kind].Skip(starts[kind]))
                    {
                        Assert.Equal(deal, sample.Deal);
                        Assert.NotEqual(0, first.LearnerMask & (1 << sample.Seat));
                        if (sample.Next >= 0)
                        {
                            Assert.Equal(sample.Seat, first.Samples[kind][sample.Next].Seat);
                        }
                    }
                }
            }

            Assert.Equal(new[] { 5, 10 }, masks.OrderBy(value => value));
            Assert.Equal(40, first.OpponentDeals[0]);
            Assert.True(first.Samples.Sum(samples => samples.Count) > 200);
            for (var kind = 0; kind < 3; kind++)
            {
                Assert.Equal(first.Samples[kind].Count, copy.Samples[kind].Count);
                for (var index = 0; index < first.Samples[kind].Count; index++)
                {
                    var a = first.Samples[kind][index];
                    var b = copy.Samples[kind][index];
                    Assert.Equal(a.Action, b.Action);
                    Assert.Equal(a.LogProbability, b.LogProbability);
                    Assert.Equal(a.Outcome, b.Outcome);
                    Assert.Equal(a.Owners, b.Owners);
                    Assert.Equal(a.Features, b.Features);
                }
            }
        }

        [Fact]
        public void ZeroOpponentChancePreservesSelfPlay()
        {
            var baseline = new PpoRecording.Worker(NeuralModels.Embedded, 231, 1);
            var zero = new PpoRecording.Worker(NeuralModels.Embedded, 231, 1, new[] { OpponentCatalog.Factory("belot206") }, 0);
            for (var deal = 0; deal < 30; deal++)
            {
                baseline.PlayDeal(deal);
                zero.PlayDeal(deal);
            }

            for (var kind = 0; kind < 3; kind++)
            {
                Assert.Equal(
                    baseline.Samples[kind].Select(sample => (sample.Action, sample.LogProbability, sample.Outcome)),
                    zero.Samples[kind].Select(sample => (sample.Action, sample.LogProbability, sample.Outcome)));
            }

            Assert.Equal(0, zero.OpponentDeals[0]);
        }

        [Fact]
        public void TrainingContextsMatchPublicEngineHistoriesAndFeatures()
        {
            for (var seed = 0; seed < 8; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed + 83421) });
                var random = new Random(seed);
                var bids = new Dictionary<(int, int), PlayerGetBidContext>();
                var cards = new Dictionary<(int, int), PlayerPlayCardContext>();
                match.Start();
                while (!match.IsFinished)
                {
                    BelotAction action;
                    if (match.Decision == BelotDecision.Bid)
                    {
                        var context = match.CreateBidContext();
                        bids.Add((context.RoundNumber, context.Bids.Count()), context);
                        var options = Enum.GetValues<BidType>().Where(bid => bid == BidType.Pass || context.AvailableBids.HasFlag(bid)).ToArray();
                        action = BelotAction.Bid(options[random.Next(options.Length)]);
                    }
                    else if (match.Decision == BelotDecision.Announce)
                    {
                        action = BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces);
                    }
                    else
                    {
                        var context = match.CreatePlayCardContext();
                        cards.Add((context.RoundNumber, context.RoundActions.Count()), context);
                        var options = context.AvailableCardsToPlay.ToArray();
                        action = BelotAction.PlayCard(options[random.Next(options.Length)], true);
                    }

                    Assert.Equal(BelotActResult.Ok, match.Act(match.ToMove, action));
                }

                var roundIndex = 0;
                var hanging = 0;
                var northSouth = 0;
                var eastWest = 0;
                var simulator = new BelotSimulator();
                foreach (var round in match.GetRecord().Rounds)
                {
                    roundIndex++;
                    var deal = NeuralDeal.Deal(round.Deal.Select(card => card.GetHashCode()).ToArray(), round.FirstToPlay.Index(), hanging);
                    var history = new TrainingDealContext(roundIndex, northSouth, eastWest);
                    foreach (var bid in round.Bids)
                    {
                        if (bids.TryGetValue((roundIndex, deal.BidCount), out var expected))
                        {
                            Assert.Equal(Base(expected), Base(history.BidContext(in deal)));
                        }

                        history.RecordBid(deal.ToBid, bid.Type);
                        deal.RecordBid(bid.Type);
                    }

                    if (deal.Contract != BidType.Pass)
                    {
                        deal.StartPlay(simulator, new DeclaredAnnounce[AnnounceScorer.MaxAnnounces]);
                        history.StartPlay(in deal);
                        var count = 0;
                        foreach (var action in round.Tricks.SelectMany(trick => trick.Cards))
                        {
                            deal.DeclareIfFirstCard();
                            var legal = simulator.LegalMoves(in deal.Play);
                            if (cards.TryGetValue((roundIndex, count), out var expected))
                            {
                                var actual = history.PlayContext(in deal, legal);
                                Assert.Equal(Base(expected), Base(actual));
                                Assert.Equal(Plays(expected), Plays(actual));
                                Assert.Equal(NeuralDeal.ToMask(expected.AvailableCardsToPlay), NeuralDeal.ToMask(actual.AvailableCardsToPlay));
                                Assert.True(NeuralDeal.FromPlayContext(actual, new BelotSimulator(), out var publicDeal));
                                Assert.Equal(Features(in deal, legal), Features(in publicDeal, legal));
                                Assert.Equal(
                                    expected.Announces.Select(announce => (announce.Player, announce.Type)).OrderBy(value => value),
                                    actual.Announces.Select(announce => (announce.Player, announce.Type)).OrderBy(value => value));
                            }

                            history.RecordCard(deal.Play.Turn, action.Card.GetHashCode(), action.Belote);
                            deal.PlayCard(simulator, action.Card.GetHashCode(), action.Belote);
                            count++;
                        }
                    }

                    hanging = round.Result.HangingPoints;
                    northSouth += round.Result.SouthNorthPoints;
                    eastWest += round.Result.EastWestPoints;
                }
            }
        }

        private static string Base(BasePlayerContext context) => JsonSerializer.Serialize(new
        {
            context.MyPosition,
            context.FirstToPlayInTheRound,
            context.RoundNumber,
            context.SouthNorthPoints,
            context.EastWestPoints,
            context.HangingPoints,
            Hand = NeuralDeal.ToMask(context.MyCards),
            context.CurrentContract,
            context.Bids,
        });

        private static string Plays(PlayerPlayCardContext context) => JsonSerializer.Serialize(new
        {
            context.CurrentTrickNumber,
            Round = context.RoundActions.Select(action => new { action.Player, Card = action.Card.GetHashCode(), action.Belote, action.TrickNumber }),
            Trick = context.CurrentTrickActions.Select(action => new { action.Player, Card = action.Card.GetHashCode(), action.Belote }),
        });

        private static float[] Features(in NeuralDeal deal, uint legal)
        {
            var indices = new int[FeatureEncoder.MaxActive];
            var values = new float[FeatureEncoder.MaxActive];
            var count = FeatureEncoder.EncodeCard(in deal, legal, indices, values);
            var result = new float[FeatureEncoder.CardInputs];
            for (var index = 0; index < count; index++)
            {
                result[indices[index]] = values[index];
            }

            return result;
        }
    }
}
