namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class RawTieBreakEndgameTests
    {
        // Raw card points only order results equal in game points: the values move by less than
        // the raw points possible (under 400 / 1024), and they do reorder some exact ties.
        [Fact]
        public void RawPointsOnlyBreakTiesInGamePoints()
        {
            var simulator = new Search.BelotSimulator();
            var plain = new EndgameSearch { UseDeclarations = true, Tricks = 3, ThreeTrickWorldLimit = 90 };
            var raw = new EndgameSearch { UseDeclarations = true, Tricks = 3, ThreeTrickWorldLimit = 90, RawTieBreak = true };
            var smart = new SmartPlayer.SmartPlayer();
            var checkedPositions = 0;
            var broken = 0;
            for (var seed = 0; seed < 30 && checkedPositions < 400; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed + 911) });
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
                        match.Act(seat, BelotAction.Declare(smart.GetAnnounces(match.CreateAnnouncesContext())));
                        continue;
                    }

                    var context = match.CreatePlayCardContext();
                    Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                    var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                    var game = new float[32];
                    var tie = new float[32];
                    if ((legal & (legal - 1)) != 0 && plain.Evaluate(context, in deal, legal, simulator, game)
                        && raw.Evaluate(context, in deal, legal, simulator, tie))
                    {
                        var bestGame = float.NegativeInfinity;
                        for (var rest = legal; rest != 0; rest &= rest - 1)
                        {
                            var card = BitOperations.TrailingZeroCount(rest);
                            Assert.InRange(tie[card] - game[card], -400f / EndgameSearch.RawScale, 400f / EndgameSearch.RawScale);
                            bestGame = Math.Max(bestGame, game[card]);
                        }

                        var distinct = false;
                        for (var rest = legal; rest != 0; rest &= rest - 1)
                        {
                            var card = BitOperations.TrailingZeroCount(rest);
                            distinct |= game[card] == bestGame && Math.Abs(tie[card] - game[card]) > 1e-6;
                        }

                        broken += distinct ? 1 : 0;
                        checkedPositions++;
                    }

                    var action = smart.PlayCard(context);
                    match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote));
                }
            }

            Assert.True(checkedPositions >= 100, $"Only {checkedPositions} endings checked.");
            Assert.True(broken > 0);
        }
    }
}
