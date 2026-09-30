namespace Belot.AI.ClaudePlayer.Tests.Human
{
    using System;
    using System.Linq;

    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class HumanMasterTests
    {
        // With its time budgets off (fixed work) the Master values and plays every card the
        // same from the engine's context and from a seat's view, in the early rollouts, the
        // sampled endgames and the exact endings alike.
        [Fact]
        public void DecidesTheSameFromTheContextAndFromTheView()
        {
            var fromContext = ClaudePlayerProfiles.CreateMaster();
            var fromView = ClaudePlayerProfiles.CreateMaster();
            foreach (var player in new[] { fromContext, fromView })
            {
                player.SearchTimeLimitMilliseconds = 0;
                player.EndgameTimeLimitMilliseconds = 0;
            }

            var smart = new SmartPlayer.SmartPlayer();
            var checkedByTrick = new int[9];
            for (var seed = 0; seed < 6 && checkedByTrick.Skip(1).Count(x => x > 0) < 7; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(29011 + seed) });
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
                    var trick = context.CurrentTrickNumber;
                    if (context.AvailableCardsToPlay.Count > 1 && checkedByTrick[trick] < 2)
                    {
                        fromContext.Rng = new Random(30011 + trick);
                        fromView.Rng = new Random(30011 + trick);
                        var expected = fromContext.EvaluateCards(context);
                        var actual = fromView.EvaluateCards(match.GetView(seat).CreatePlayCardContext());
                        Assert.Equal(expected.Select(x => x.Card), actual.Select(x => x.Card));
                        Assert.Equal(expected.Select(x => x.Value), actual.Select(x => x.Value));
                        fromContext.Rng = new Random(30011 + trick);
                        fromView.Rng = new Random(30011 + trick);
                        Assert.Equal(fromContext.PlayCard(context).Card, fromView.Decide(match.GetView(seat)).Card);
                        checkedByTrick[trick]++;
                    }

                    var action = smart.PlayCard(context);
                    match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote));
                }
            }

            Assert.All(Enumerable.Range(1, 7), trick => Assert.True(checkedByTrick[trick] > 0, $"No decision checked in trick {trick}."));
            Assert.Equal(0, fromContext.Fallbacks);
        }
    }
}
