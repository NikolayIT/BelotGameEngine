namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Linq;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.NeuralTrainer;
    using BelotLegacy;

    using Xunit;

    public class LegacyOpponentsTests
    {
        [Theory]
        [InlineData("sharpbelot")]
        [InlineData("belot206")]
        public void MirroredSelfMatchesAreExactlyEven(string kind)
        {
            var create = OpponentCatalog.Factory(kind);
            var result = Arena.Play(create, create, 20, 2, 69100000);
            Assert.Equal(.5, result.Match.Score);
            Assert.Equal(0, result.Match.Sigma);
            Assert.Equal(0, result.Match.PointsPerGame);
            Assert.All(result.PairScores, score => Assert.Equal(.5, score));
            Assert.All(result.PairPoints, points => Assert.Equal(0, points));
        }

        [Theory]
        [InlineData("sharpbelot")]
        [InlineData("belot206")]
        public void SeededMatchesDoNotDependOnParallelism(string kind)
        {
            var first = Arena.Play(OpponentCatalog.Factory("smart"), OpponentCatalog.Factory(kind), 20, 1, 69200000);
            var repeat = Arena.Play(OpponentCatalog.Factory("smart"), OpponentCatalog.Factory(kind), 20, 4, 69200000);
            Assert.Equal(first.PairScores, repeat.PairScores);
            Assert.Equal(first.PairPoints, repeat.PairPoints);
            Assert.Equal(first.TeamB.CardDecisions, repeat.TeamB.CardDecisions);
            Assert.Equal(first.TeamB.RejectedBids, repeat.TeamB.RejectedBids);
        }

        [Theory]
        [InlineData("sharpbelot")]
        [InlineData("belot206")]
        public void AdaptersAreLegalAndSeeTheSameStateThroughViews(string kind)
        {
            var factory = OpponentCatalog.Factory(kind);
            var contracts = 0;
            var decisions = 0;
            for (var seed = 0; seed < 24; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed + 69300000) });
                var contexts = Enumerable.Range(0, 4).Select(seat => factory(seed + seat)).ToArray();
                var views = Enumerable.Range(0, 4).Select(seat => factory(seed + seat)).ToArray();
                var random = new Random(seed);
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    var fromView = views[seat.Index()].Decide(match.GetView(seat));
                    BelotAction fromContext;
                    switch (match.Decision)
                    {
                        case BelotDecision.Bid:
                            var bid = match.CreateBidContext();
                            fromContext = BelotAction.Bid(contexts[seat.Index()].GetBid(bid));
                            break;
                        case BelotDecision.Announce:
                            fromContext = BelotAction.Declare(contexts[seat.Index()].GetAnnounces(match.CreateAnnouncesContext()));
                            break;
                        default:
                            var play = contexts[seat.Index()].PlayCard(match.CreatePlayCardContext());
                            fromContext = BelotAction.PlayCard(play.Card, play.Belote);
                            break;
                    }

                    Assert.Equal(BelotActResult.Ok, match.Validate(seat, fromContext));
                    Assert.Equal(fromContext.Type, fromView.Type);
                    Assert.Equal(fromContext.BidType, fromView.BidType);
                    Assert.Equal(fromContext.Card, fromView.Card);
                    Assert.Equal(fromContext.Belote, fromView.Belote);

                    // Deliberately expose the adapters to other styles and every contract,
                    // including doubles, instead of testing only their own reachable states.
                    if (match.Decision == BelotDecision.Bid)
                    {
                        var available = match.CreateBidContext().AvailableBids;
                        var options = Enum.GetValues<BidType>().Where(value => value == BidType.Pass || available.HasFlag(value)).ToArray();
                        fromContext = BelotAction.Bid(options[random.Next(options.Length)]);
                        contracts |= (int)fromContext.BidType;
                    }
                    else if (match.Decision == BelotDecision.PlayCard && random.Next(2) == 0)
                    {
                        var cards = match.CreatePlayCardContext().AvailableCardsToPlay.ToArray();
                        fromContext = BelotAction.PlayCard(cards[random.Next(cards.Length)], true);
                    }

                    Assert.Equal(BelotActResult.Ok, match.Act(seat, fromContext));
                    decisions++;
                }

                Assert.All(contexts, player => Assert.Equal(0, ((ILegacyDiagnostics)player).CardFallbacks));
            }

            Assert.Equal(255, contracts);
            Assert.True(decisions > 2000);
        }
    }
}
