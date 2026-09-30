namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class CandidateRolloutTests
    {
        // The rollouts play out only the network's best cards (by count and margin); the others
        // keep the network's value less a thousand points, so the player never chooses them.
        [Theory]
        [InlineData(2, 0, false)]
        [InlineData(3, 1.5, false)]
        [InlineData(2, 0, true)]
        public void PlaysOutOnlyTheNetworksBestCards(int count, double margin, bool ensemble)
        {
            var models = RandomModels.Create(40211);
            var plain = new ClaudePlayerNeural(models) { CardSuitEnsemble = ensemble };
            var searching = new ClaudePlayerNeural(models)
            {
                SearchDeals = 4,
                SearchDoubleDummyTricks = 5,
                SearchCandidateCards = count,
                SearchCandidateMargin = margin,
                SearchEnsembleCandidates = ensemble,
                SearchOwnershipModel = Belot.AI.ClaudePlayer.Neural.CardOwnershipModel.Embedded,
            };
            var smart = new SmartPlayer.SmartPlayer();
            var checkedPositions = 0;
            for (var seed = 0; seed < 4 && checkedPositions < 12; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(40311 + seed) });
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    if (match.Decision == BelotDecision.Bid)
                    {
                        match.Act(seat, BelotAction.Bid(smart.GetBid(match.CreateBidContext())));
                        continue;
                    }

                    if (match.Decision == BelotDecision.Announce)
                    {
                        match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces));
                        continue;
                    }

                    var context = match.CreatePlayCardContext();
                    if (context.CurrentTrickNumber <= 3 && context.AvailableCardsToPlay.Count > count)
                    {
                        var network = plain.EvaluateCards(context);
                        var searched = searching.EvaluateCards(context);
                        var expected = network.Where((value, index) => index < count && value.Value >= network[0].Value - (margin > 0 ? margin : double.PositiveInfinity))
                            .Select(value => value.Card).ToHashSet();
                        var kept = searched.Where(value => value.Value > network[0].Value - 500).Select(value => value.Card).ToHashSet();
                        Assert.Equal(expected, kept);
                        Assert.Contains(searching.PlayCard(context).Card, kept);
                        checkedPositions++;
                    }

                    var action = smart.PlayCard(context);
                    match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote));
                }
            }

            Assert.True(checkedPositions >= 12, $"Only {checkedPositions} positions checked.");
        }
    }
}
