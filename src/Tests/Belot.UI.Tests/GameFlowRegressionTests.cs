namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.DummyPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    [Collection(AppState.Name)]
    public class GameFlowRegressionTests
    {
        public GameFlowRegressionTests() => AppState.Reset();

        [Fact]
        public void AcceptedMovesCloseTheirControlsBeforeTheSessionResumes() => UiThread.Run(async () =>
        {
            var (session, table, _) = GameTableTests.Table("dummy", "dummy", "dummy", seed: 5);
            using (table)
            {
                var driver = new TableDriver(session, 5) { AutoPlay = false };
                var decisions = new HashSet<BelotDecision>();
                driver.Turn += turn =>
                {
                    if (!turn.IsHuman)
                    {
                        return;
                    }

                    decisions.Add(turn.Decision);
                    switch (turn.Decision)
                    {
                        case BelotDecision.Bid:
                            table.BidCommand.Execute(table.BidOptions.First(o => o.IsEnabled && o.Bid != BidType.Pass));
                            break;
                        case BelotDecision.Announce:
                            table.DeclareCommand.Execute(null);
                            break;
                        default:
                            var invalid = Card.AllCards.First(c => !session.GetView(Seats.Person)!.PlayableCards.Contains(c));
                            table.TapCardCommand.Execute(new CardSlot(invalid));
                            Assert.True(table.IsMyTurn);
                            table.TapCardCommand.Execute(table.MyHand.First(c => c.IsPlayable));
                            break;
                    }

                    Assert.Equal(BelotDecision.None, session.AwaitedDecision);
                    Assert.False(table.IsMyTurn);
                    Assert.False(table.IsHintVisible);
                    Assert.False(table.IsBidPanelVisible);
                    Assert.False(table.IsAnnouncePanelVisible);
                };
                await driver.PlayToTheEndAsync();
                Assert.Contains(BelotDecision.Bid, decisions);
                Assert.Contains(BelotDecision.Announce, decisions);
                Assert.Contains(BelotDecision.PlayCard, decisions);
            }
        });

        [Fact]
        public void RepeatedLeaveAndLateCommandsDoNotNavigateOrRestartAgain() => UiThread.Run(async () =>
        {
            var (session, table, host) = GameTableTests.Table("dummy", "dummy", "dummy", seed: 5);
            table.StartGame();
            table.LeaveCommand.Execute(null);
            table.LeaveCommand.Execute(null);
            table.StartGame();
            Assert.False(session.IsRunning);
            Assert.Equal(1, host.Leaves);
            table.Dispose();
            table.LeaveCommand.Execute(null);
            table.StartGame();
            Assert.False(session.IsRunning);
            Assert.Equal(1, host.Leaves);
            await session.Completion;
        });

        [Fact]
        public void ASessionErrorClearsTheDecisionBeforeReportingIt() => UiThread.Run(async () =>
        {
            var session = GameSessionTests.NewSession(5);
            var reported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            session.TurnStarted += turn =>
            {
                if (turn.IsHuman)
                {
                    throw new InvalidOperationException("The turn could not be displayed.");
                }
            };
            session.GameError += _ => reported.TrySetResult();
            session.Start();
            await reported.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(session.IsRunning);
            Assert.Equal(BelotDecision.None, session.AwaitedDecision);
            Assert.False(session.TryBid(BidType.Pass));
            Assert.Null(await session.GetHintAsync());
            await session.Completion;
        });

        [Fact]
        public void FailedHintsShowANoticeAndCanBeRetriedWithoutEndingTheGame() => UiThread.Run(async () =>
        {
            var fail = true;
            var session = GameSessionTests.NewSession(5, hint: () => new HintPlayer(() =>
            {
                if (fail)
                {
                    throw new InvalidOperationException("Hint failure.");
                }
            }));
            var host = new FakeTableHost();
            using var table = new GameViewModel(session, new Lineup(AiLevels.ById("dummy"), AiLevels.ById("dummy"), AiLevels.ById("dummy")), host);
            var tester = new TableTester(session, table, 5);
            table.StartGame();
            await tester.Until(() => table.IsMyTurn, "a hintable turn");
            await table.ShowHintAsync();
            Assert.False(table.IsHintBusy);
            Assert.Equal(LocalizationManager.Instance["Hint_Unavailable"], table.ToastMessage);
            Assert.True(table.IsMyTurn);
            Assert.True(session.IsRunning);
            Assert.False(table.IsGameOverlayVisible);
            fail = false;
            await table.ShowHintAsync();
            Assert.False(table.IsHintBusy);
            Assert.Single(table.BidOptions, option => option.IsHinted);
            host.RunTimers();
            Assert.False(table.IsToastVisible);
        });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void HintsCompletingAfterDisposalDoNotChangeTheTable(bool fail) => UiThread.Run(async () =>
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            var session = GameSessionTests.NewSession(5, hint: () => new HintPlayer(() =>
            {
                started.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("The hint was not released.");
                }

                if (fail)
                {
                    throw new InvalidOperationException("Late hint failure.");
                }
            }));
            var host = new FakeTableHost();
            using var table = new GameViewModel(session, new Lineup(AiLevels.ById("dummy"), AiLevels.ById("dummy"), AiLevels.ById("dummy")), host);
            var tester = new TableTester(session, table, 5);
            table.StartGame();
            await tester.Until(() => table.IsMyTurn, "a hintable turn");
            var hint = table.ShowHintAsync();
            try
            {
                await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
                table.Dispose();
                var changes = 0;
                table.PropertyChanged += (_, _) => changes++;
                release.Set();
                await hint.WaitAsync(TimeSpan.FromSeconds(10));
                host.RunTimers();
                Assert.Equal(0, changes);
                Assert.DoesNotContain(table.BidOptions, option => option.IsHinted);
                Assert.False(session.IsRunning);
            }
            finally
            {
                release.Set();
            }
        });

        [Fact]
        public void HintsSelectOnlyDeclarationsThatCanBeAnnouncedTogether() => UiThread.Run(async () =>
        {
            const int seed = 76;
            var (session, table, _) = GameTableTests.Table("dummy", "dummy", "dummy", seed);
            using (table)
            {
                var conflicts = 0;
                var tester = new TableTester(session, table, seed);
                tester.OnDecision = async decision =>
                {
                    if (decision != BelotDecision.Announce)
                    {
                        return;
                    }

                    var offered = table.AnnounceOptions.Select(o => o.Announce).ToArray();
                    if (!offered.Any(a => offered.Any(b => a != b && RandomMoves.Conflict(a, b))))
                    {
                        return;
                    }

                    conflicts++;
                    await table.ShowHintAsync();
                    var selected = table.AnnounceOptions.Where(o => o.IsSelected).Select(o => o.Announce).ToArray();
                    foreach (var first in selected)
                    {
                        Assert.All(selected.Where(a => a != first), other => Assert.False(RandomMoves.Conflict(first, other)));
                    }

                    Assert.All(table.AnnounceOptions, option => Assert.Equal(option.IsSelected, option.IsHinted));
                };
                table.StartGame();
                await tester.PlayToTheEndAsync();
                Assert.True(conflicts > 0);
            }
        });

        [Fact]
        public void AHintCanChooseTheSecondOfTwoDeclarationsOfTheSameKind() => UiThread.Run(async () =>
        {
            const int seed = 41;
            var session = GameSessionTests.NewSession(seed, hint: () => new HintPlayer(() => { }, lastAnnounceOnly: true));
            using var table = new GameViewModel(session, new Lineup(AiLevels.ById("dummy"), AiLevels.ById("dummy"), AiLevels.ById("dummy")), new FakeTableHost());
            var tester = new TableTester(session, table, seed);
            var checkedChoice = false;
            tester.OnDecision = async decision =>
            {
                if (decision != BelotDecision.Announce || table.AnnounceOptions.Count < 2)
                {
                    return;
                }

                var expected = table.AnnounceOptions.Last();
                if (!table.AnnounceOptions.Take(table.AnnounceOptions.Count - 1).Any(x => x.Announce.Type == expected.Announce.Type))
                {
                    return;
                }

                checkedChoice = true;
                await table.ShowHintAsync();
                Assert.Same(expected, Assert.Single(table.AnnounceOptions, option => option.IsSelected));
                Assert.Same(expected, Assert.Single(table.AnnounceOptions, option => option.IsHinted));
            };
            table.StartGame();
            await tester.PlayToTheEndAsync();
            Assert.True(checkedChoice);
        });

        private sealed class HintPlayer : IPlayer
        {
            private readonly DummyPlayer player = new();

            private readonly Action beforeDecision;

            private readonly bool lastAnnounceOnly;

            public HintPlayer(Action beforeDecision, bool lastAnnounceOnly = false)
            {
                this.beforeDecision = beforeDecision;
                this.lastAnnounceOnly = lastAnnounceOnly;
            }

            public BidType GetBid(PlayerGetBidContext context)
            {
                this.beforeDecision();
                return this.player.GetBid(context);
            }

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
            {
                this.beforeDecision();
                return this.lastAnnounceOnly ? context.AvailableAnnounces.TakeLast(1).ToArray() : this.player.GetAnnounces(context);
            }

            public PlayCardAction PlayCard(PlayerPlayCardContext context)
            {
                this.beforeDecision();
                return this.player.PlayCard(context);
            }

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
            {
            }

            public void EndOfRound(RoundResult roundResult)
            {
            }

            public void EndOfGame(GameResult gameResult)
            {
            }
        }
    }
}
