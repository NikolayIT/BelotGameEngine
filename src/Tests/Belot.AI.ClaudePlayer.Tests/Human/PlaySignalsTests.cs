namespace Belot.AI.ClaudePlayer.Tests.Human
{
    using System;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Human;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class PlaySignalsTests
    {
        // Every card read as a signal is one its seat threw on another suit's trick without
        // trumping, split by who held the trick when it was played; nothing else is.
        [Fact]
        public void ReadsTheThrownCardsOfEverySeatByWhoHeldTheTrick()
        {
            var smart = new SmartPlayer.SmartPlayer();
            var discards = 0;
            var smears = 0;
            for (var seed = 0; seed < 20; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed + 7717) });
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
                    var kind = SimTables.ToKind(context.CurrentContract.Type);
                    var signals = PlaySignals.Read(context.RoundActions, kind);
                    var expectedDiscards = new uint[4];
                    var expectedSmears = new uint[4];
                    var actions = context.RoundActions.ToList();
                    for (var start = 0; start < actions.Count; start += 4)
                    {
                        var led = actions[start].Card.GetHashCode() >> 3;
                        var winner = actions[start];
                        for (var i = start + 1; i < Math.Min(start + 4, actions.Count); i++)
                        {
                            var card = actions[i].Card.GetHashCode();
                            var holder = winner.Player.Index();
                            if ((card >> 3) != led && !(kind < SimTables.NoTrumps && (card >> 3) == kind))
                            {
                                if (((holder ^ actions[i].Player.Index()) & 1) == 0)
                                {
                                    expectedSmears[actions[i].Player.Index()] |= 1u << card;
                                }
                                else
                                {
                                    expectedDiscards[actions[i].Player.Index()] |= 1u << card;
                                }
                            }

                            var row = ((kind * 4) + led) * 32;
                            if (SimTables.Strengths[row + card] > SimTables.Strengths[row + winner.Card.GetHashCode()])
                            {
                                winner = actions[i];
                            }
                        }
                    }

                    for (var s = 0; s < 4; s++)
                    {
                        Assert.Equal(expectedDiscards[s], signals.Discards[s]);
                        Assert.Equal(expectedSmears[s], signals.Smears[s]);
                        Assert.Equal(0u, signals.Discards[s] & signals.Smears[s]);
                        discards += System.Numerics.BitOperations.PopCount(signals.Discards[s]);
                        smears += System.Numerics.BitOperations.PopCount(signals.Smears[s]);
                    }

                    var action = smart.PlayCard(context);
                    match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote));
                }
            }

            Assert.True(discards > 0 && smears > 0);
        }
    }
}
