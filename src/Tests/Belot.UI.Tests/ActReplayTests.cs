namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.UI.Game;

    using Xunit;

    // ActReplay turns one Act into what happened at the table. Played straight on the engine (no
    // session, no pauses), every act of many matches must replay, deal after deal, into exactly the
    // match's record, with the actor's own move first and every other bid or card flagged as made
    // by the rules.
    public class ActReplayTests
    {
        [Fact]
        public void EveryActShouldReplayIntoTheRecord()
        {
            var seen = new Counts();
            for (var seed = 0; seed < 300; seed++)
            {
                var (events, record) = PlayAndReplay(seed, seen);
                RecordCheck.EventsMatchTheRecord(events, record);
            }

            // The matches went through the moments a single act makes many things happen.
            Assert.True(seen.AutoPasses > 100, $"auto passes: {seen.AutoPasses}");
            Assert.True(seen.AutoCards > 100, $"forced cards: {seen.AutoCards}");
            Assert.True(seen.DeclarationsThenForcedCard > 0, "a declaration followed by a forced card");
            Assert.True(seen.DealEndedByAnEarlierTrick > 0, "a deal ended by a card before the last trick");
            Assert.True(seen.PassedOut > 20, $"passed-out deals: {seen.PassedOut}");
            Assert.True(seen.Capots > 0, "a capot");
            Assert.True(seen.Hanging > 0, "a hanging deal");
            Assert.True(seen.Inside > 0, "a deal the declarers lost");
        }

        private static (List<object> Events, BelotMatchRecord Record) PlayAndReplay(int seed, Counts seen)
        {
            var random = new Random(seed);
            var match = new BelotMatch(new BelotMatchOptions { FirstToPlay = Seats.All[seed % 4], Random = new Random(seed) });

            // South decides at random (as a person might); the others are the computer levels.
            var players = new IPlayer[] { null!, new RandomPlayer(new Random(seed + 1)), new DummyPlayer(), new SmartPlayer() };
            var events = new List<object>();
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                var view = match.GetView(seat);
                var action = seat == Seats.Person ? RandomMoves.Choose(view, random) : players[seat.Index()].Decide(view);

                var before = match.GetView(Seats.Person);
                Assert.Equal(BelotActResult.Ok, match.Act(seat, action));
                var steps = ActReplay.Steps(before, match.GetView(Seats.Person), seat, action);
                CheckAct(steps, seat, action, seen);
                events.AddRange(steps);
            }

            return (events, match.GetRecord());
        }

        // One act: the actor's move comes first; the moves after it are the rules' (passes and
        // forced cards), and at most one deal ends.
        private static void CheckAct(IReadOnlyList<GameEvent> steps, PlayerPosition actor, BelotAction action, Counts seen)
        {
            Assert.NotEmpty(steps);
            var first = steps[0];
            switch (action.Type)
            {
                case BelotActionType.Bid:
                    var bid = Assert.IsType<BidInfo>(first);
                    Assert.Equal(actor, bid.Seat);
                    Assert.Equal(action.BidType, bid.Bid);
                    Assert.False(bid.IsAuto);
                    break;
                case BelotActionType.Announce:
                    var declared = Assert.IsType<AnnounceInfo>(first);
                    Assert.Equal(actor, declared.Seat);

                    // What the engine kept: a combination sharing a card with an earlier one is dropped.
                    var offered = action.Announces.Select(a => a.Type).ToList();
                    Assert.All(declared.Combinations, c => Assert.True(offered.Remove(c.Type), $"{c.Type} was not declared"));
                    break;
                default:
                    var card = Assert.IsType<CardInfo>(first);
                    Assert.Equal(actor, card.Seat);
                    Assert.Equal(action.Card, card.Card);
                    Assert.False(card.IsAuto);
                    break;
            }

            foreach (var step in steps.Skip(1))
            {
                switch (step)
                {
                    case BidInfo bid:
                        Assert.True(bid.IsAuto);
                        Assert.Equal(BidType.Pass, bid.Bid);
                        seen.AutoPasses++;
                        break;
                    case CardInfo card:
                        Assert.True(card.IsAuto);
                        seen.AutoCards++;
                        if (action.Type == BelotActionType.Announce)
                        {
                            seen.DeclarationsThenForcedCard++;
                        }

                        break;
                    case AnnounceInfo:
                        Assert.Fail("Only the actor declares in an act.");
                        break;
                }
            }

            var ends = steps.OfType<RoundEndInfo>().ToList();
            Assert.InRange(ends.Count, 0, 1);
            if (ends.Count == 1)
            {
                var end = ends[0];
                Assert.Same(end, steps[^1]);
                if (end.Outcome == RoundOutcome.PassedOut)
                {
                    seen.PassedOut++;
                }
                else if (steps.OfType<CardInfo>().FirstOrDefault() is { TrickNumber: < 8 })
                {
                    seen.DealEndedByAnEarlierTrick++;
                }

                seen.Capots += end.IsCapot ? 1 : 0;
                seen.Hanging += end.Outcome == RoundOutcome.Hanging ? 1 : 0;
                seen.Inside += end.Outcome == RoundOutcome.Inside ? 1 : 0;
            }
        }

        private sealed class Counts
        {
            public int AutoPasses { get; set; }

            public int AutoCards { get; set; }

            public int DeclarationsThenForcedCard { get; set; }

            public int DealEndedByAnEarlierTrick { get; set; }

            public int PassedOut { get; set; }

            public int Capots { get; set; }

            public int Hanging { get; set; }

            public int Inside { get; set; }
        }
    }
}
