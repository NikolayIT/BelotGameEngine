namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    /// <summary>
    /// A server keeps no bot between decisions: it builds one and asks it with the seat's view
    /// (<see cref="PlayerViewExtensions.Decide"/>). That is only right if the bots decide exactly
    /// as they do with the engine's own contexts, at every decision of whole matches.
    /// </summary>
    public class DecideFromViewTests
    {
        [Fact]
        public void TheBotsDecideTheSameFromAViewAsFromTheEngineContext()
        {
            var decisions = 0;
            var endgames = 0L;
            for (var seed = 0; seed < 2; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed) });
                var fromContext = Enumerable.Range(0, 4).Select(i => Claude(seed, i)).ToArray();
                var fromView = Enumerable.Range(0, 4).Select(i => Claude(seed, i)).ToArray();
                var smart = new SmartPlayer.SmartPlayer();
                var neural = new ClaudePlayerNeural(RandomModels.Create(seed));
                var endgame = new ClaudePlayerNeural(RandomModels.Create(seed)) { UseEndgameSearch = true };
                var declaredEndgame = new ClaudePlayerNeural(RandomModels.Create(seed)) { UseEndgameSearch = true, EndgameUseDeclarations = true, EndgameTricks = 3 };
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    var view = match.GetView(seat);
                    var expected = DecideFromContext(fromContext[seat.Index()], match);
                    AssertSame(expected, fromView[seat.Index()].Decide(view));
                    AssertSame(DecideFromContext(smart, match), smart.Decide(view));
                    AssertSame(DecideFromContext(neural, match), neural.Decide(view));
                    AssertSame(DecideFromContext(endgame, match), endgame.Decide(view));
                    AssertSame(DecideFromContext(declaredEndgame, match), declaredEndgame.Decide(view));
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, expected));
                    decisions++;
                }

                endgames += endgame.EndgameDecisions;
            }

            Assert.True(decisions > 300);
            Assert.True(endgames > 0);
        }

        private static ClaudePlayerIsmcts Claude(int seed, int seat) =>
            new ClaudePlayerIsmcts
            {
                Rng = new Random((seed * 4) + seat),
                MaxIterations = 150,
                TimeLimitMilliseconds = 600_000,
                BiddingDeals = 60,
            };

        private static BelotAction DecideFromContext(IPlayer player, BelotMatch match)
        {
            switch (match.Decision)
            {
                case BelotDecision.Bid:
                    return BelotAction.Bid(player.GetBid(match.CreateBidContext()));
                case BelotDecision.Announce:
                    return BelotAction.Declare(player.GetAnnounces(match.CreateAnnouncesContext()));
                default:
                    var action = player.PlayCard(match.CreatePlayCardContext());
                    return BelotAction.PlayCard(action.Card, action.Belote);
            }
        }

        private static void AssertSame(BelotAction expected, BelotAction actual)
        {
            Assert.Equal(expected.Type, actual.Type);
            Assert.Equal(expected.BidType, actual.BidType);
            Assert.Equal(expected.Card, actual.Card);
            Assert.Equal(expected.Belote, actual.Belote);
            Assert.Equal(expected.Announces?.Select(x => x.Type), actual.Announces?.Select(x => x.Type));
        }
    }
}
