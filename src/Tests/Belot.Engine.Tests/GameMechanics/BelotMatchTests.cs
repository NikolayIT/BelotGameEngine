namespace Belot.Engine.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.Engine.Tests.FakeObjects;

    using Xunit;

    public class BelotMatchTests
    {
        private static readonly PlayerPosition[] Seats =
        {
            PlayerPosition.South, PlayerPosition.East, PlayerPosition.North, PlayerPosition.West,
        };

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DrivingTheMatchPlaysExactlyLikeBelotGame(bool fromViews)
        {
            // The same seeded deals and players, once through BelotGame and once through the
            // match (from its contexts, or from each seat's view): the same game, callback for
            // callback.
            for (var seed = 0; seed < 150; seed++)
            {
                var first = Seats[seed % 4];
                var gameLog = new StringBuilder();
                var gamePlayers = Players(seed).Select(x => (IPlayer)new CallbackLogger(x, gameLog)).ToArray();
                var expected = new BelotGame(gamePlayers[0], gamePlayers[1], gamePlayers[2], gamePlayers[3], new Random(seed)).PlayGame(first);

                var matchLog = new StringBuilder();
                var players = Players(seed);
                var observers = players.Select(x => new CallbackLogger(x, matchLog)).ToArray();
                var match = new BelotMatch(observers[0], observers[1], observers[2], observers[3], new BelotMatchOptions { Random = new Random(seed), FirstToPlay = first });
                match.Start();
                Drive(match, players, fromViews);

                Assert.Equal(gameLog.ToString(), matchLog.ToString());
                Assert.True(match.IsFinished);
                Assert.False(match.IsStopped);
                Assert.Equal(expected.Winner, match.Winner);
                Assert.Equal(expected.SouthNorthPoints, match.SouthNorthPoints);
                Assert.Equal(expected.EastWestPoints, match.EastWestPoints);
                Assert.Equal(expected.RoundsPlayed, match.RoundsPlayed);
                Assert.Equal(PlayerPosition.Unknown, match.ToMove);
                Assert.Equal(BelotDecision.None, match.Decision);
                Assert.All(players, x => Assert.Same(match.Result, x.LastGameResult));
            }
        }

        [Fact]
        public void IllegalDecisionsChangeNothing()
        {
            var players = Players(3);
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(3) });
            match.Start();
            var checkedCards = false;
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                var before = Describe(match.GetView(seat));
                var other = seat.Next();
                var action = players[seat.Index()].Decide(match.GetView(seat));
                var wrongKind = action.Type == BelotActionType.Bid ? BelotAction.PlayCard(Card.AllCards[0]) : BelotAction.Bid(BidType.Pass);
                var illegal = new List<BelotAction> { null, wrongKind };
                if (match.Decision == BelotDecision.Bid)
                {
                    var open = match.CreateBidContext().AvailableBids;
                    illegal.Add(BelotAction.Bid(BidType.Clubs | BidType.Hearts));
                    illegal.AddRange(new[] { BidType.Double, BidType.ReDouble, BidType.Clubs }.Where(x => !open.HasFlag(x)).Select(BelotAction.Bid));
                }

                if (match.Decision == BelotDecision.PlayCard)
                {
                    var view = match.GetView(seat);
                    illegal.AddRange(Card.AllCards.Where(x => !view.PlayableCards.Contains(x)).Select(x => BelotAction.PlayCard(x)));
                    illegal.Add(BelotAction.PlayCard(null));
                    checkedCards = true;
                }

                Assert.Equal(BelotActResult.NotYourTurn, match.Validate(other, action));
                Assert.Equal(BelotActResult.NotYourTurn, match.Act(other, action));
                foreach (var bad in illegal)
                {
                    Assert.Equal(BelotActResult.InvalidAction, match.Validate(seat, bad));
                    Assert.Equal(BelotActResult.InvalidAction, match.Act(seat, bad));
                }

                Assert.Equal(before, Describe(match.GetView(seat)));
                Assert.Equal(BelotActResult.Ok, match.Act(seat, action));
            }

            Assert.True(checkedCards);
            Assert.Equal(BelotActResult.MatchFinished, match.Act(PlayerPosition.South, BelotAction.Bid(BidType.Pass)));
        }

        [Fact]
        public void StopEndsTheMatchAndTheRecordKeepsTheUnfinishedDeal()
        {
            var players = Players(5);
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(5) });
            match.Start();

            // Play into the third trick of a played deal: two cards on the table.
            while (!(match.Decision == BelotDecision.PlayCard && match.GetView(match.ToMove) is var v && v.Tricks.Count == 2 && v.CurrentTrick.Count == 2))
            {
                Assert.Equal(BelotActResult.Ok, match.Act(match.ToMove, players[match.ToMove.Index()].Decide(match.GetView(match.ToMove))));
            }

            var seat = match.ToMove;
            match.Stop();
            match.Stop();

            Assert.True(match.IsFinished);
            Assert.True(match.IsStopped);
            Assert.Equal(PlayerPosition.Unknown, match.Winner);
            Assert.Equal(PlayerPosition.Unknown, match.ToMove);
            Assert.Null(match.Result);
            Assert.Equal(BelotActResult.MatchFinished, match.Act(seat, BelotAction.PlayCard(Card.AllCards[0])));

            var record = match.GetRecord();
            var last = record.Rounds[record.Rounds.Count - 1];
            Assert.Null(last.Result);
            Assert.Equal(3, last.Tricks.Count);
            Assert.Equal(2, last.Tricks[2].Cards.Count);
            Assert.Equal(PlayerPosition.Unknown, last.Tricks[2].Winner);
            Assert.Equal(match.RoundsPlayed + 1, record.Rounds.Count);
            Assert.Equal(PlayerPosition.Unknown, record.Winner);

            var final = match.GetFinalView();
            Assert.Equal(PlayerPosition.Unknown, final.Seat);
            Assert.Empty(final.Hand);
            Assert.True(final.IsMatchFinished);
            Assert.NotNull(final.Record);
        }

        [Fact]
        public void ViewsShowEachSeatOnlyWhatItMaySee()
        {
            var checkedHidden = 0;
            for (var seed = 0; seed < 40; seed++)
            {
                var players = Players(seed);
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed) });
                match.Start();
                while (!match.IsFinished)
                {
                    var toMove = match.ToMove;
                    foreach (var seat in Seats)
                    {
                        var view = match.GetView(seat);
                        Assert.Equal(seat, view.Seat);
                        Assert.Equal(toMove, view.ToMove);
                        Assert.Equal(view.CardCounts[seat.Index()], view.Hand.Count);
                        Assert.Equal(match.SouthNorthPoints, view.SouthNorthPoints);
                        Assert.Equal(match.HangingPoints, view.HangingPoints);
                        if (seat != toMove)
                        {
                            Assert.Equal(BidType.Pass, view.AvailableBids);
                            Assert.Empty(view.AvailableAnnounces);
                            Assert.Empty(view.PlayableCards);
                        }

                        foreach (var announce in view.Announces)
                        {
                            if (announce.Type == AnnounceType.Belot || announce.Player == seat)
                            {
                                Assert.NotNull(announce.Card);
                            }
                            else
                            {
                                Assert.Null(announce.Card);
                                Assert.Null(announce.IsScored);
                                checkedHidden++;
                            }
                        }

                        Assert.All(view.BeloteCards, x => Assert.Contains(x, view.PlayableCards));
                    }

                    var mover = match.GetView(toMove);
                    switch (match.Decision)
                    {
                        case BelotDecision.Bid:
                            Assert.Equal(match.CreateBidContext().AvailableBids, mover.AvailableBids);
                            break;
                        case BelotDecision.PlayCard:
                            Assert.Equal(match.CreatePlayCardContext().AvailableCardsToPlay.OrderBy(x => x.GetHashCode()), mover.PlayableCards.OrderBy(x => x.GetHashCode()));
                            break;
                    }

                    Assert.Equal(BelotActResult.Ok, match.Act(toMove, players[toMove.Index()].Decide(mover)));
                }

                // After the deal, every declaration shows its cards and whether it scored.
                foreach (var summary in match.GetView(PlayerPosition.South).PreviousRounds)
                {
                    Assert.All(summary.Announces, x => Assert.NotNull(x.Card));
                    Assert.All(summary.Announces, x => Assert.NotNull(x.IsScored));
                }
            }

            Assert.True(checkedHidden > 0);
        }

        [Fact]
        public void TheRecordHasEveryDealInFull()
        {
            for (var seed = 0; seed < 30; seed++)
            {
                var players = Players(seed);
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed) });
                match.Start();
                Drive(match, players, true);
                var record = match.GetRecord();
                var summaries = match.GetView(PlayerPosition.East).PreviousRounds;

                Assert.Equal(match.RoundsPlayed, record.Rounds.Count);
                Assert.Equal(match.Winner, record.Winner);
                Assert.Equal(match.SouthNorthPoints, record.SouthNorthPoints);
                for (var r = 0; r < record.Rounds.Count; r++)
                {
                    var round = record.Rounds[r];
                    Assert.Equal(r + 1, round.RoundNumber);
                    Assert.Equal(32, round.Deal.Count);
                    Assert.Equal(32, round.Deal.Distinct().Count());
                    Assert.Equal(summaries[r].SouthNorthPoints, round.Result.SouthNorthPoints);
                    Assert.Equal(round.Bids.Select(x => x.ToString()), summaries[r].Bids.Select(x => x.ToString()));
                    Assert.Equal(
                        round.Tricks.SelectMany(x => x.Cards).Select(x => $"{x.Player}{x.Card}{x.Belote}"),
                        summaries[r].Tricks.SelectMany(x => x.Cards).Select(x => $"{x.Player}{x.Card}{x.Belote}"));
                    Assert.Equal(round.Tricks.Select(x => x.Winner), summaries[r].Tricks.Select(x => x.Winner));
                    Assert.Equal(round.Contract.Type, round.Result.Contract.Type);
                    if (round.Contract.Type == BidType.Pass)
                    {
                        Assert.Empty(round.Tricks);
                        continue;
                    }

                    // Each seat played exactly the eight cards the deal gave it.
                    Assert.Equal(8, round.Tricks.Count);
                    for (var seat = 0; seat < 4; seat++)
                    {
                        var dealt = Enumerable.Range(0, 5).Select(i => round.Deal[(4 * i) + seat])
                            .Concat(Enumerable.Range(0, 3).Select(i => round.Deal[20 + (4 * i) + seat]));
                        var played = round.Tricks.SelectMany(x => x.Cards).Where(x => x.Player == Seats[seat]).Select(x => x.Card);
                        Assert.Equal(dealt.OrderBy(x => x.GetHashCode()), played.OrderBy(x => x.GetHashCode()));
                    }

                    // Each trick's winner led the next one.
                    for (var t = 0; t < 7; t++)
                    {
                        Assert.Equal(round.Tricks[t].Winner, round.Tricks[t + 1].Cards[0].Player);
                    }
                }
            }
        }

        [Fact]
        public void ContextsAreCopies()
        {
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(1) });
            match.Start();
            var first = match.CreateBidContext();
            first.MyCards.Clear();
            ((List<Bid>)first.Bids).Add(new Bid(PlayerPosition.North, BidType.AllTrumps));
            first.CurrentContract.Type = BidType.AllTrumps;

            var second = match.CreateBidContext();
            Assert.Equal(5, second.MyCards.Count);
            Assert.Empty(second.Bids);
            Assert.Equal(BidType.Pass, second.CurrentContract.Type);
            Assert.Throws<InvalidOperationException>(() => match.CreatePlayCardContext());
            Assert.Throws<InvalidOperationException>(() => match.CreateAnnouncesContext());
        }

        [Fact]
        public void TheHangingPointsReachEveryContext()
        {
            var seen = 0;
            for (var seed = 0; seed < 100; seed++)
            {
                var players = Players(seed);
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed) });
                match.Start();
                while (!match.IsFinished)
                {
                    BasePlayerContext context = match.Decision switch
                    {
                        BelotDecision.Bid => match.CreateBidContext(),
                        BelotDecision.Announce => match.CreateAnnouncesContext(),
                        _ => match.CreatePlayCardContext(),
                    };
                    Assert.Equal(match.HangingPoints, context.HangingPoints);
                    seen += context.HangingPoints > 0 ? 1 : 0;
                    match.Act(match.ToMove, players[match.ToMove.Index()].Decide(match.GetView(match.ToMove)));
                }
            }

            Assert.True(seen > 0);
        }

        [Fact]
        public void MisuseThrows()
        {
            var match = new BelotMatch();
            Assert.Throws<InvalidOperationException>(() => match.Act(PlayerPosition.South, BelotAction.Bid(BidType.Pass)));
            Assert.Throws<InvalidOperationException>(() => match.GetView(PlayerPosition.South));
            Assert.Throws<InvalidOperationException>(() => match.Stop());
            Assert.Equal(PlayerPosition.Unknown, match.ToMove);
            match.Start();
            Assert.Throws<InvalidOperationException>(() => match.Start());
            Assert.Throws<InvalidOperationException>(() => match.GetRecord());
            Assert.Throws<ArgumentOutOfRangeException>(() => match.GetView(PlayerPosition.SouthNorthTeam));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BelotMatch(new BelotMatchOptions { FirstToPlay = PlayerPosition.Unknown }));

            var withoutHistory = new BelotMatch(new BelotMatchOptions { RecordHistory = false });
            withoutHistory.Start();
            Assert.Throws<InvalidOperationException>(() => withoutHistory.GetView(PlayerPosition.South));
            Assert.Equal(BelotDecision.Bid, withoutHistory.Decision);
            Assert.NotNull(withoutHistory.CreateBidContext());
        }

        private static SeededRandomPlayer[] Players(int seed) =>
            Enumerable.Range(0, 4).Select(i => new SeededRandomPlayer((seed * 4) + i, bidRandomly: true)).ToArray();

        private static void Drive(BelotMatch match, IPlayer[] players, bool fromViews)
        {
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                var player = players[seat.Index()];
                BelotAction action;
                if (fromViews)
                {
                    action = player.Decide(match.GetView(seat));
                }
                else
                {
                    switch (match.Decision)
                    {
                        case BelotDecision.Bid:
                            action = BelotAction.Bid(player.GetBid(match.CreateBidContext()));
                            break;
                        case BelotDecision.Announce:
                            action = BelotAction.Declare(player.GetAnnounces(match.CreateAnnouncesContext()));
                            break;
                        default:
                            var played = player.PlayCard(match.CreatePlayCardContext());
                            action = BelotAction.PlayCard(played.Card, played.Belote);
                            break;
                    }
                }

                Assert.Equal(BelotActResult.Ok, match.Validate(seat, action));
                Assert.Equal(BelotActResult.Ok, match.Act(seat, action));
            }
        }

        private static string Describe(BelotSeatView view)
        {
            var text = new StringBuilder();
            text.Append($"{view.Seat}|{view.ToMove}|{view.Decision}|{view.RoundNumber}|{view.SouthNorthPoints}|{view.EastWestPoints}|{view.HangingPoints}|");
            text.Append($"{string.Join(",", view.Hand)}|{string.Join(",", view.Bids)}|{view.Contract}|{view.AvailableBids}|");
            text.Append($"{string.Join(",", view.PlayableCards)}|{string.Join(",", view.CardCounts)}|");
            text.Append(string.Join(",", view.Announces.Select(x => $"{x.Player}{x.Type}{x.Card}{x.IsScored}")));
            text.Append(string.Join(",", view.Tricks.SelectMany(x => x.Cards).Concat(view.CurrentTrick).Select(x => $"{x.Player}{x.Card}{x.Belote}")));
            return text.ToString();
        }

        // Writes every callback that is not a decision into a shared log.
        private sealed class CallbackLogger : IPlayer
        {
            private readonly IPlayer inner;
            private readonly StringBuilder log;

            public CallbackLogger(IPlayer inner, StringBuilder log)
            {
                this.inner = inner;
                this.log = log;
            }

            public BidType GetBid(PlayerGetBidContext context) => this.inner.GetBid(context);

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.inner.GetAnnounces(context);

            public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.inner.PlayCard(context);

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
            {
                this.log.AppendLine($"trick {string.Join(",", trickActions.Select(x => $"{x.Player}:{x.Card}{x.Belote}#{x.TrickNumber}"))}");
                this.inner.EndOfTrick(trickActions);
            }

            public void EndOfRound(RoundResult roundResult)
            {
                this.log.AppendLine($"round {roundResult.Contract} {roundResult.SouthNorthPoints}-{roundResult.EastWestPoints} {roundResult.HangingPoints}");
                this.inner.EndOfRound(roundResult);
            }

            public void EndOfGame(GameResult gameResult)
            {
                this.log.AppendLine($"game {gameResult.RoundsPlayed} {gameResult.SouthNorthPoints}-{gameResult.EastWestPoints}");
                this.inner.EndOfGame(gameResult);
            }
        }
    }
}
