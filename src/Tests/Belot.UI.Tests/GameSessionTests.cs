namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.UI.Game;

    using Xunit;

    // The app's game flow: whole games through GameSession on a UI-like thread, the person
    // deciding at random through the Try methods. What the table is told must be the game the
    // engine played, in the table's order, on the UI thread, with the pauses of the pace, and
    // stopping or restarting at any point must leave nothing behind.
    [Collection(AppState.Name)]
    public class GameSessionTests
    {
        private static readonly string[] Names = { "Ann", "East", "Partner", "West" };

        private static readonly BidType[] AllBids =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades,
            BidType.NoTrumps, BidType.AllTrumps, BidType.Double, BidType.ReDouble,
        };

        public GameSessionTests() => AppState.Reset();

        [Theory]
        [InlineData("random", "random", "random", 3)]
        [InlineData("dummy", "smart", "random", 3)]
        [InlineData("smart", "dummy", "smart", 3)]
        [InlineData("claude", "smart", "dummy", 3)]
        [InlineData("expert", "claude", "expert", 3)]
        public void GamesAtEveryLevelShouldBeWhatTheEnginePlayed(string partner, string west, string east, int games) => UiThread.Run(async () =>
        {
            var lineup = new Lineup(AiLevels.ById(partner), AiLevels.ById(west), AiLevels.ById(east));
            for (var seed = 0; seed < games; seed++)
            {
                var session = new GameSession(Names, lineup.CreatePlayer, GamePace.Instant, () => new SmartPlayer(), new Random(seed));
                var driver = new TableDriver(session, seed);
                await driver.PlayToTheEndAsync();

                var record = session.GetRecord()!;
                RecordCheck.EventsMatchTheRecord(driver.Events, record);
                CheckTheTablesOrder(driver.Events);
                Assert.Equal(new[] { UiThread.Id }, driver.EventThreads);
                Assert.Equal(record.Winner, driver.Over!.WinnerTeam);
                Assert.False(session.IsRunning);
                Assert.Equal(BelotDecision.None, session.AwaitedDecision);
            }
        });

        // Each computer seat is played by its own player, built for it, which sees only its seat.
        [Fact]
        public void EachComputerSeatShouldBePlayedByItsOwnPlayer() => UiThread.Run(async () =>
        {
            var spies = new List<SeatSpy>();
            var session = new GameSession(
                Names,
                seat =>
                {
                    var spy = new SeatSpy(seat);
                    spies.Add(spy);
                    return spy;
                },
                GamePace.Instant,
                () => new SmartPlayer(),
                new Random(3));
            await new TableDriver(session, 3).PlayToTheEndAsync();

            Assert.Equal(new[] { PlayerPosition.East, PlayerPosition.North, PlayerPosition.West }, spies.Select(s => s.Seat));
            Assert.All(spies, spy =>
            {
                Assert.Equal(new[] { spy.Seat }, spy.SeatsAsked);
                Assert.True(spy.Decisions > 20);
            });
        });

        [Fact]
        public void ThePersonsIllegalDecisionsShouldBeRefused() => UiThread.Run(async () =>
        {
            var session = NewSession(5);
            var driver = new TableDriver(session, 5) { AutoPlay = false };
            var checkedDecisions = new HashSet<BelotDecision>();
            driver.Turn += turn =>
            {
                if (!turn.IsHuman)
                {
                    // Nothing is asked of the person while the others decide.
                    Assert.Equal(BelotDecision.None, session.AwaitedDecision);
                    Assert.False(session.TryBid(BidType.Pass));
                    Assert.False(session.TryDeclare(Array.Empty<BelotAnnounce>()));
                    Assert.False(session.TryPlay(Card.AllCards[0]));
                    return;
                }

                var view = session.GetView(Seats.Person)!;
                Assert.Equal(turn.Decision, session.AwaitedDecision);
                Assert.False(session.TryPlay(null!));
                Assert.False(session.TryDeclare(null!));
                if (turn.Decision != BelotDecision.Bid)
                {
                    Assert.False(session.TryBid(BidType.Pass));
                }

                if (turn.Decision != BelotDecision.Announce)
                {
                    Assert.False(session.TryDeclare(Array.Empty<BelotAnnounce>()));
                }

                if (turn.Decision != BelotDecision.PlayCard)
                {
                    Assert.False(session.TryPlay(view.Hand[0]));
                }

                switch (turn.Decision)
                {
                    case BelotDecision.Bid:
                        foreach (var bid in AllBids.Where(b => !view.AvailableBids.HasFlag(b)))
                        {
                            Assert.False(session.TryBid(bid), $"{bid} is not available");
                        }

                        break;
                    case BelotDecision.Announce:
                        var notOffered = new BelotAnnounce { Player = Seats.Person, Type = AnnounceType.FourJacks, Card = Card.GetCard(CardSuit.Club, CardType.Jack) };
                        if (!view.AvailableAnnounces.Any(a => a.Type == notOffered.Type))
                        {
                            Assert.False(session.TryDeclare(new[] { notOffered }));
                        }

                        break;
                    default:
                        foreach (var card in Card.AllCards.Where(c => !view.PlayableCards.Contains(c)))
                        {
                            Assert.False(session.TryPlay(card), $"{card} may not be played");
                        }

                        break;
                }

                // Nothing refused changed anything: the same decision is still awaited.
                Assert.Equal(turn.Decision, session.AwaitedDecision);
                checkedDecisions.Add(turn.Decision);
                driver.Decide(turn.Decision);
                Assert.Equal(BelotDecision.None, session.AwaitedDecision);
            };
            session.RoundFinished += _ => Assert.False(session.TryBid(BidType.Pass));

            await driver.PlayToTheEndAsync();
            RecordCheck.EventsMatchTheRecord(driver.Events, session.GetRecord()!);
            Assert.Contains(BelotDecision.Bid, checkedDecisions);
            Assert.Contains(BelotDecision.PlayCard, checkedDecisions);
        });

        // Stopped from a handler of any event (the game page leaves from anywhere), the game
        // raises nothing more, waits for nothing, and its run ends.
        [Theory]
        [InlineData("deal")]
        [InlineData("computer's turn")]
        [InlineData("person's turn")]
        [InlineData("bid")]
        [InlineData("contract")]
        [InlineData("card")]
        [InlineData("trick")]
        [InlineData("result")]
        public void StoppingFromAHandlerShouldRaiseNothingMore(string when) => UiThread.Run(async () =>
        {
            var session = NewSession(7, pace: new GamePace(3, 3, 3));
            var driver = new TableDriver(session, 7);
            var stoppedAt = -1;
            void StopOnce()
            {
                if (stoppedAt < 0)
                {
                    session.Stop();
                    stoppedAt = driver.Events.Count;
                }
            }

            switch (when)
            {
                case "deal":
                    session.RoundStarted += deal => StopOnce();
                    break;
                case "computer's turn":
                    session.TurnStarted += turn =>
                    {
                        if (!turn.IsHuman)
                        {
                            StopOnce();
                        }
                    };
                    break;
                case "person's turn":
                    session.TurnStarted += turn =>
                    {
                        if (turn.IsHuman)
                        {
                            StopOnce();
                        }
                    };
                    break;
                case "bid":
                    session.BidMade += bid => StopOnce();
                    break;
                case "contract":
                    session.ContractSettled += contract => StopOnce();
                    break;
                case "card":
                    session.CardPlayed += card =>
                    {
                        if (card.TrickNumber == 3)
                        {
                            StopOnce();
                        }
                    };
                    break;
                case "trick":
                    session.TrickCollected += trick => StopOnce();
                    break;
                default:
                    session.RoundFinished += round => StopOnce();
                    break;
            }

            session.Start();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(60));
            await Task.Delay(100);

            Assert.True(stoppedAt > 0, "the game was never stopped");
            Assert.Equal(stoppedAt, driver.Events.Count);
            Assert.Empty(driver.Errors);
            Assert.Null(driver.Over);
            Assert.False(session.IsRunning);
            Assert.Equal(BelotDecision.None, session.AwaitedDecision);
            Assert.Null(session.GetRecord());
            Assert.Null(await session.GetHintAsync());
        });

        // Restarted from a handler (Play again, or a new game from the menu), only the new game
        // goes on: the old one raises nothing more.
        [Theory]
        [InlineData("deal")]
        [InlineData("person's turn")]
        [InlineData("result")]
        public void RestartingFromAHandlerShouldPlayOnlyTheNewGame(string when) => UiThread.Run(async () =>
        {
            var session = NewSession(9);
            var driver = new TableDriver(session, 9);
            var newGameFrom = -1;
            void RestartOnce()
            {
                if (newGameFrom < 0)
                {
                    newGameFrom = driver.Events.Count;
                    session.Restart();
                }
            }

            switch (when)
            {
                case "deal":
                    session.RoundStarted += deal => RestartOnce();
                    break;
                case "person's turn":
                    session.TurnStarted += turn =>
                    {
                        if (turn.IsHuman)
                        {
                            RestartOnce();
                        }
                    };
                    break;
                default:
                    session.RoundFinished += round => RestartOnce();
                    break;
            }

            await driver.PlayToTheEndAsync();

            Assert.True(newGameFrom > 0);
            var newGame = driver.Events.Skip(newGameFrom).ToList();
            var deal = Assert.IsType<DealInfo>(newGame[0]);
            Assert.Equal(1, deal.RoundNumber);
            Assert.Equal(0, deal.SouthNorthPoints + deal.EastWestPoints);
            RecordCheck.EventsMatchTheRecord(newGame, session.GetRecord()!);
            CheckTheTablesOrder(newGame);
        });

        // A hint is what the hint player would decide in the person's place, from the person's
        // own view; asked for a turn that is over, it is dropped.
        [Fact]
        public void HintsShouldBeWhatTheHintPlayerWouldDoInThePersonsPlace() => UiThread.Run(async () =>
        {
            var session = NewSession(11, hint: () => new SmartPlayer());
            var driver = new TableDriver(session, 11) { AutoPlay = false };
            var nextTurn = new TaskCompletionSource<TurnInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
            driver.Turn += turn =>
            {
                if (turn.IsHuman)
                {
                    nextTurn.TrySetResult(turn);
                }
            };

            Assert.Null(await session.GetHintAsync());
            session.Start();
            var hints = new HashSet<BelotActionType>();
            var dropped = 0;
            for (var i = 0; i < 60; i++)
            {
                var asked = nextTurn.Task;
                if (await Task.WhenAny(asked, session.Completion).WaitAsync(TimeSpan.FromSeconds(30)) != asked)
                {
                    break;
                }

                var turn = asked.Result;
                nextTurn = new TaskCompletionSource<TurnInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
                var view = session.GetView(Seats.Person)!;
                if (i % 5 == 4)
                {
                    // Decided before the hint came: the hint is for a turn that is over.
                    var late = session.GetHintAsync();
                    driver.Decide(turn.Decision);
                    Assert.Null(await late);
                    dropped++;
                    continue;
                }

                var expected = new SmartPlayer().Decide(view);
                var hint = (await session.GetHintAsync())!;
                Assert.Equal(expected.Type, hint.Type);
                Assert.Equal(expected.BidType, hint.BidType);
                Assert.Equal(expected.Card, hint.Card);
                Assert.Equal(expected.Announces?.Select(a => a.ToString()), hint.Announces?.Select(a => a.ToString()));
                Assert.Equal(turn.Decision, session.AwaitedDecision);
                hints.Add(hint.Type);
                driver.Decide(turn.Decision);
            }

            session.Stop();
            Assert.Contains(BelotActionType.Bid, hints);
            Assert.Contains(BelotActionType.PlayCard, hints);
            Assert.True(dropped > 0);
            Assert.Empty(driver.Errors);
        });

        // The computer thinks for the pace's pause, a finished trick stays on the table for its
        // pause, and a pass or card the rules made waits for the auto-move pause.
        [Fact]
        public void TheGameShouldPauseWhereThePaceSays() => UiThread.Run(async () =>
        {
            var pace = new GamePace(ThinkDelayMs: 80, TrickSettleMs: 120, AutoMoveDelayMs: 60);
            var session = NewSession(13, pace: pace);
            var driver = new TableDriver(session, 13);
            var played = 0;
            session.RoundFinished += round =>
            {
                if (round.Outcome != RoundOutcome.PassedOut && ++played == 2)
                {
                    session.Stop();
                }
            };
            session.Start();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(120));
            Assert.Empty(driver.Errors);

            int thinking = 0, settling = 0, auto = 0;
            for (var i = 1; i < driver.Events.Count; i++)
            {
                var gap = driver.Times[i] - driver.Times[i - 1];
                var minimum = driver.Events[i] switch
                {
                    TrickInfo => pace.TrickSettleMs,
                    BidInfo { IsAuto: true } or CardInfo { IsAuto: true } => pace.AutoMoveDelayMs,
                    BidInfo or AnnounceInfo or CardInfo when driver.Events[i - 1] is TurnInfo { IsHuman: false } => pace.ThinkDelayMs,
                    _ => 0,
                };
                switch (driver.Events[i])
                {
                    case TrickInfo:
                        settling++;
                        break;
                    case BidInfo { IsAuto: true } or CardInfo { IsAuto: true }:
                        auto++;
                        break;
                    default:
                        thinking += minimum > 0 ? 1 : 0;
                        break;
                }

                // Timers count in system ticks (about 16 ms on Windows), so one may end a tick early.
                Assert.True(gap >= minimum - 16, $"{driver.Events[i]} came {gap} ms after {driver.Events[i - 1]}; expected {minimum}");
            }

            Assert.True(thinking > 10);
            Assert.True(settling >= 16);
            Assert.True(auto > 0);
        });

        [Fact]
        public void TheLineupShouldBuildEachSeatsLevel()
        {
            var lineup = new Lineup(AiLevels.ById("dummy"), AiLevels.ById("random"), AiLevels.ById("smart"));
            Assert.IsType<DummyPlayer>(lineup.CreatePlayer(PlayerPosition.North));
            Assert.IsType<RandomPlayer>(lineup.CreatePlayer(PlayerPosition.West));
            Assert.IsType<SmartPlayer>(lineup.CreatePlayer(PlayerPosition.East));
            Assert.Throws<ArgumentOutOfRangeException>(() => lineup.CreatePlayer(PlayerPosition.South));

            // The rivals are rated as a pair; a level on both sides is one level.
            Assert.Equal((int)Math.Round((AiLevels.ById("random").Elo + AiLevels.ById("smart").Elo) / 2.0, MidpointRounding.AwayFromZero), lineup.RivalsElo);
            Assert.Equal(new[] { "random", "smart" }, lineup.RivalLevels.Select(l => l.Id));
            Assert.Single(new Lineup(AiLevels.ById("smart"), AiLevels.ById("claude"), AiLevels.ById("claude")).RivalLevels);
        }

        internal static GameSession NewSession(int seed, GamePace? pace = null, Func<IPlayer>? hint = null) =>
            new(Names, seat => new DummyPlayer(), pace ?? GamePace.Instant, hint ?? (() => new SmartPlayer()), new Random(seed));

        // The order of the table: every decision is announced by a turn for the seat that makes it,
        // right before it; deals follow one another with the score and the hanging points of the
        // deal before; the game ends with a deal.
        internal static void CheckTheTablesOrder(IReadOnlyList<object> events)
        {
            var first = Assert.IsType<DealInfo>(events[0]);
            Assert.Equal(1, first.RoundNumber);
            RoundEndInfo? previous = null;
            for (var i = 0; i < events.Count; i++)
            {
                switch (events[i])
                {
                    case TurnInfo turn:
                        Assert.Equal(turn.Seat == Seats.Person, turn.IsHuman);
                        switch (turn.Decision)
                        {
                            case BelotDecision.Bid:
                                var bid = Assert.IsType<BidInfo>(events[i + 1]);
                                Assert.Equal(turn.Seat, bid.Seat);
                                Assert.False(bid.IsAuto);
                                break;
                            case BelotDecision.Announce:
                                Assert.Equal(turn.Seat, Assert.IsType<AnnounceInfo>(events[i + 1]).Seat);
                                break;
                            default:
                                var card = Assert.IsType<CardInfo>(events[i + 1]);
                                Assert.Equal(turn.Seat, card.Seat);
                                Assert.False(card.IsAuto);
                                break;
                        }

                        break;
                    case BidInfo { IsAuto: false }:
                    case CardInfo { IsAuto: false }:
                    case AnnounceInfo:
                        Assert.IsType<TurnInfo>(events[i - 1]);
                        break;
                    case DealInfo deal:
                        Assert.Equal(5, deal.MyHand.Count);
                        Assert.Equal(new[] { 5, 5, 5, 5 }, deal.CardCounts);
                        if (previous != null)
                        {
                            Assert.Same(previous, events[i - 1]);
                            Assert.Equal(previous.RoundNumber + 1, deal.RoundNumber);
                            Assert.Equal(previous.SouthNorthGamePoints, deal.SouthNorthPoints);
                            Assert.Equal(previous.EastWestGamePoints, deal.EastWestPoints);
                            Assert.Equal(previous.HangingAfter, deal.HangingPoints);
                        }

                        break;
                    case RoundEndInfo end:
                        Assert.Equal(end.IsGameOver, i == events.Count - 1);
                        previous = end;
                        break;
                }
            }

            Assert.True(previous!.IsGameOver);
        }

        // A computer seat that notes which seat it is asked to decide for.
        private sealed class SeatSpy : IPlayer
        {
            private readonly DummyPlayer player = new();

            public SeatSpy(PlayerPosition seat) => this.Seat = seat;

            public PlayerPosition Seat { get; }

            public HashSet<PlayerPosition> SeatsAsked { get; } = new();

            public int Decisions { get; private set; }

            public BidType GetBid(PlayerGetBidContext context) => this.Note(context, this.player.GetBid(context));

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.Note(context, this.player.GetAnnounces(context));

            public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.Note(context, this.player.PlayCard(context));

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
            {
            }

            public void EndOfRound(RoundResult roundResult)
            {
            }

            public void EndOfGame(GameResult gameResult)
            {
            }

            private T Note<T>(BasePlayerContext context, T decision)
            {
                this.SeatsAsked.Add(context.MyPosition);
                this.Decisions++;
                return decision;
            }
        }
    }
}
