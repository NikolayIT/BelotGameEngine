namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class MasterProfileTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void PromotedMasterMatchesTheFrozenConfigurationAndPublicView(int contractIndex)
        {
            var fromContext = ClaudePlayerProfiles.CreateMaster();
            var fromView = ClaudePlayerProfiles.CreateMaster();
            var reference = new ClaudePlayerNeural
            {
                CardSuitEnsemble = true,
                UseEndgameSearch = true,
                EndgameUseDeclarations = true,
                EndgameTricks = 5,
                EndgameThreeTrickWorldLimit = 1680,
                EndgameSampledWorlds = 128,
                EndgameNodeLimit = 250000,
                EndgameUseTranspositions = true,
                EndgameOwnershipModel = CardOwnershipModel.Embedded,
                EndgameOwnershipPower = 1,
                EndgameOwnershipUniformMix = 0.1,
            };

            // Fixed work makes parity independent of scheduling and JIT warmup. The factory's
            // exact eight-millisecond setting is checked separately in ClaudePlayerProfilesTests.
            fromContext.EndgameTimeLimitMilliseconds = 0;
            fromView.EndgameTimeLimitMilliseconds = 0;
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(17031 + contractIndex) });
            var smart = new SmartPlayer.SmartPlayer();
            var neuralFallback = false;
            var sampled = false;
            var exact = false;
            match.Start();
            while (!match.IsFinished && (!neuralFallback || !sampled || !exact))
            {
                var seat = match.ToMove;
                if (match.Decision == BelotDecision.Bid)
                {
                    var context = match.CreateBidContext();
                    var bid = context.CurrentContract.Type == BidType.Pass ? FeatureEncoder.BidOfIndex(contractIndex) : BidType.Pass;
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Bid(bid)));
                }
                else if (match.Decision == BelotDecision.Announce)
                {
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces)));
                }
                else
                {
                    var context = match.CreatePlayCardContext();
                    var early = context.CurrentTrickNumber < 4;
                    var fiveTricks = context.CurrentTrickNumber == 4;
                    var twoTricks = context.CurrentTrickNumber == 7;
                    if (context.AvailableCardsToPlay.Count > 1 && ((early && !neuralFallback) || (fiveTricks && !sampled) || (twoTricks && !exact)))
                    {
                        var view = match.GetView(seat);
                        fromContext.Rng = new Random(18031);
                        fromView.Rng = new Random(18031);
                        reference.Rng = new Random(18031);
                        var expected = reference.EvaluateCards(context);
                        var actual = fromContext.EvaluateCards(context);
                        var copied = fromView.EvaluateCards(view.CreatePlayCardContext());
                        Assert.Equal(expected.Select(value => value.Card), actual.Select(value => value.Card));
                        Assert.Equal(expected.Select(value => value.Value), actual.Select(value => value.Value));
                        Assert.Equal(actual.Select(value => value.Card), copied.Select(value => value.Card));
                        Assert.Equal(actual.Select(value => value.Value), copied.Select(value => value.Value));
                        Assert.Equal(reference.Rng.Next(), fromContext.Rng.Next());
                        Assert.Equal(fromContext.EndgameWorlds, fromView.EndgameWorlds);
                        fromContext.Rng = new Random(19031);
                        fromView.Rng = new Random(19031);
                        var action = fromContext.PlayCard(context);
                        var copiedAction = fromView.Decide(view);
                        Assert.Equal(action.Card, copiedAction.Card);
                        Assert.Equal(action.Belote, copiedAction.Belote);
                        if (early)
                        {
                            Assert.Equal(0, fromContext.EndgameDecisions);
                            Assert.Equal(0, fromView.EndgameDecisions);
                            Assert.Equal(0, reference.EndgameDecisions);
                        }

                        neuralFallback |= early;
                        sampled |= fiveTricks;
                        exact |= twoTricks;
                    }

                    var played = smart.PlayCard(context);
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(played.Card, played.Belote)));
                }
            }

            Assert.True(neuralFallback && sampled && exact);
            Assert.True(fromContext.EndgameDecisions > 0);
            Assert.True(fromContext.EndgameSampleAttempts > 0);
            Assert.Equal(0, fromContext.Fallbacks + fromView.Fallbacks + reference.Fallbacks);
        }
    }
}
