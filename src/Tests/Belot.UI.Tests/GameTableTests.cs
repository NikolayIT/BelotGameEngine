namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    // The game table (GameViewModel) with a real GameSession, played through its commands on a
    // UI-like thread. See TableTester for what is checked at every decision and result.
    [Collection(AppState.Name)]
    public class GameTableTests
    {
        private static readonly LocalizationManager Loc = LocalizationManager.Instance;

        public GameTableTests() => AppState.Reset();

        [Theory]
        [InlineData("dummy", "dummy", "dummy")]
        [InlineData("smart", "random", "dummy")]
        [InlineData("random", "smart", "smart")]
        public void GamesAtTheTableShouldShowTheGameAndBeRecordedOnce(string partner, string west, string east) => UiThread.Run(async () =>
        {
            var rivals = new[] { west, east }.Distinct().ToArray();
            for (var game = 1; game <= 2; game++)
            {
                var (session, table, _) = Table(partner, west, east, seed: game);
                var tester = new TableTester(session, table, game);
                table.StartGame();
                await tester.PlayToTheEndAsync();

                var won = tester.Over!.WeWon;
                Assert.True(tester.Decisions > 20);
                Assert.True(table.IsRatingChangeVisible);
                Assert.Equal(game, PlayerRatingStore.GamesPlayed);

                var history = MatchHistoryStore.All();
                Assert.Equal(game, history.Count);
                Assert.Equal((partner, west, east), (history[0].PartnerId, history[0].WestId, history[0].EastId));
                Assert.Equal(won, history[0].Won);
                Assert.Equal((table.UsPoints, table.ThemPoints), (history[0].UsPoints, history[0].ThemPoints));

                // The record against a level counts each game once, whichever side it sat on.
                Assert.All(rivals, id => Assert.Equal(game, OpponentStatsStore.For(id).Games));
                Assert.All(AiLevels.All.Where(l => !rivals.Contains(l.Id)), l => Assert.Equal(0, OpponentStatsStore.For(l.Id).Games));
                table.Dispose();
            }
        });

        // The person's rating moves by the team formula: the person and the partner against the
        // rivals' average.
        [Fact]
        public void TheRatingShouldMoveByTheTeamsExpectedScore() => UiThread.Run(async () =>
        {
            var (session, table, _) = Table("smart", "dummy", "random", seed: 4);
            var tester = new TableTester(session, table, 4);
            table.StartGame();
            await tester.PlayToTheEndAsync();

            var expected = PlayerRatingStore.ExpectedScore(PlayerRatingStore.DefaultElo, AiLevels.ById("smart").Elo, table.Lineup.RivalsElo);
            var delta = (int)Math.Round(32 * ((tester.Over!.WeWon ? 1 : 0) - expected), MidpointRounding.AwayFromZero);
            Assert.Equal(PlayerRatingStore.DefaultElo + delta, PlayerRatingStore.CurrentElo);
            var sign = delta >= 0 ? $"+{delta}" : delta.ToString();
            Assert.Equal(Loc.Format("Rating_Change", PlayerRatingStore.DefaultElo, PlayerRatingStore.CurrentElo, sign), table.RatingChangeText);
            table.Dispose();
        });

        // With the beginner assists off the table shows no belote badges and no hint button (the
        // tester checks both at every decision, against the setting).
        [Fact]
        public void WithAssistsOffTheTableShouldShowNoBadgesOrHints() => UiThread.Run(async () =>
        {
            AppSettings.AssistsEnabled = false;
            var (session, table, _) = Table("dummy", "smart", "dummy", seed: 1);
            var tester = new TableTester(session, table, 1);
            table.StartGame();
            await tester.PlayToTheEndAsync();
            Assert.False(table.IsHintVisible);
            table.Dispose();
        });

        // The deal's screen tells every way a deal can end (checked by the tester against the
        // result), and a combination the person taps on or off never leaves two sharing a card.
        [Fact]
        public void TheTableShouldGoThroughEveryKindOfDeal() => UiThread.Run(async () =>
        {
            var outcomes = new HashSet<RoundOutcome>();
            var declarations = 0;
            for (var seed = 10; seed < 30 && (outcomes.Count < 4 || declarations < 5); seed++)
            {
                var (session, table, _) = Table("dummy", "dummy", "smart", seed);
                var tester = new TableTester(session, table, seed);
                table.StartGame();
                await tester.PlayToTheEndAsync();
                outcomes.UnionWith(tester.Rounds.Select(r => r.Outcome));
                declarations += tester.DecisionKinds.Contains(BelotDecision.Announce) ? 1 : 0;
                table.Dispose();
            }

            Assert.Equal(4, outcomes.Count);
            Assert.True(declarations >= 5);
        });

        // An unexpected error ends the game on the game-over overlay, showing only the error: not
        // the last result's rating change.
        [Fact]
        public void AnErrorAfterARatedGameShouldShowOnlyTheError() => UiThread.Run(async () =>
        {
            var explode = false;
            var (session, table, _) = Table("dummy", "dummy", "dummy", seed: 3, bots: seat => new ExplodingPlayer(() => explode));
            var tester = new TableTester(session, table, 3);
            table.StartGame();
            await tester.PlayToTheEndAsync();
            Assert.True(table.IsRatingChangeVisible);

            explode = true;
            table.PlayAgainCommand.Execute(null);
            await tester.Until(() => table.IsGameOverlayVisible, "the error");

            Assert.Equal(Loc["Error_Title"], table.GameOverlayTitle);
            Assert.Contains("The computer broke", table.GameOverlayBody);
            Assert.Equal("⚠️", table.GameOverlayIcon);
            Assert.False(table.IsRatingChangeVisible);
            Assert.False(table.IsMyTurn);
            Assert.False(session.IsRunning);
            Assert.Equal(1, PlayerRatingStore.GamesPlayed);
            table.Dispose();
        });

        // A hint highlights the bid, the combinations or the card the hint player would choose,
        // for a while. Asked again before that time is up, the second hint gets its full time too.
        [Fact]
        public void AHintShouldShowTheHintPlayersChoiceForItsFullTime() => UiThread.Run(async () =>
        {
            var (session, table, host) = Table("dummy", "dummy", "dummy", seed: 5);
            var tester = new TableTester(session, table, 5);
            var hinted = new HashSet<BelotDecision>();
            tester.OnDecision = async decision =>
            {
                host.RunTimers();
                var view = session.GetView(Seats.Person)!;
                var expected = new SmartPlayer().Decide(view);
                await table.ShowHintAsync();
                Func<bool> isShown;
                switch (decision)
                {
                    case BelotDecision.Bid:
                        var option = Assert.Single(table.BidOptions, o => o.IsHinted);
                        Assert.Equal(expected.BidType, option.Bid);
                        isShown = () => option.IsHinted;
                        break;
                    case BelotDecision.Announce:
                        Assert.Equal(
                            expected.Announces.Select(a => a.Type).OrderBy(x => x),
                            table.AnnounceOptions.Where(o => o.IsHinted).Select(o => o.Announce.Type).OrderBy(x => x));
                        Assert.All(table.AnnounceOptions, o => Assert.Equal(o.IsHinted, o.IsSelected));
                        isShown = () => table.AnnounceOptions.Any(o => o.IsHinted) || expected.Announces.Count == 0;
                        break;
                    default:
                        var slot = Assert.Single(table.MyHand, s => s.IsHinted);
                        Assert.Same(expected.Card, slot.Card);
                        isShown = () => slot.IsHinted;
                        break;
                }

                if (hinted.Add(decision))
                {
                    await table.ShowHintAsync();
                    host.RunOldestTimer();
                    Assert.True(isShown());
                    host.RunOldestTimer();
                    Assert.DoesNotContain(table.MyHand, s => s.IsHinted);
                    Assert.DoesNotContain(table.BidOptions, o => o.IsHinted);
                    Assert.DoesNotContain(table.AnnounceOptions, o => o.IsHinted);
                }
                else
                {
                    host.RunTimers();
                }
            };
            table.StartGame();
            await tester.PlayToTheEndAsync();

            Assert.Contains(BelotDecision.Bid, hinted);
            Assert.Contains(BelotDecision.PlayCard, hinted);
            table.Dispose();
        });

        // "Play again" deals one new game, however fast it is tapped twice.
        [Fact]
        public void PlayAgainTappedTwiceShouldDealOneNewGame() => UiThread.Run(async () =>
        {
            var (session, table, _) = Table("dummy", "dummy", "dummy", seed: 8);
            var tester = new TableTester(session, table, 8);
            table.StartGame();
            await tester.PlayToTheEndAsync();

            var deals = 0;
            session.RoundStarted += _ => deals++;
            table.PlayAgainCommand.Execute(null);
            table.PlayAgainCommand.Execute(null);
            await tester.Until(() => table.IsMyTurn, "the first decision of the new game");
            await Task.Delay(50);

            Assert.Equal(1, deals);
            Assert.False(table.HasMatchHistory);
            await tester.PlayToTheEndAsync();
            Assert.Equal(2, PlayerRatingStore.GamesPlayed);
            Assert.True(table.HasMatchHistory);
            table.Dispose();
        });

        // Leaving stops the game at once and asks the page to go back; the game is not rated.
        [Fact]
        public void LeavingShouldStopTheGameUnrated() => UiThread.Run(async () =>
        {
            var (session, table, host) = Table("dummy", "dummy", "dummy", seed: 9, pace: new GamePace(5, 5, 5));
            var tester = new TableTester(session, table, 9);
            table.StartGame();
            for (var i = 0; i < 10; i++)
            {
                await tester.StepAsync();
            }

            var events = 0;
            session.CardPlayed += _ => events++;
            session.BidMade += _ => events++;
            table.LeaveCommand.Execute(null);
            await Task.Delay(200);

            Assert.Equal(1, host.Leaves);
            Assert.False(session.IsRunning);
            Assert.Equal(0, events);
            Assert.Equal(0, PlayerRatingStore.GamesPlayed);
            Assert.Empty(MatchHistoryStore.All());
            table.Dispose();
        });

        internal static (GameSession Session, GameViewModel Table, FakeTableHost Host) Table(
            string partner,
            string west,
            string east,
            int seed,
            GamePace? pace = null,
            Func<PlayerPosition, IPlayer>? bots = null)
        {
            var lineup = new Lineup(AiLevels.ById(partner), AiLevels.ById(west), AiLevels.ById(east));
            var names = new[]
            {
                "Ann",
                Loc.Format("Seat_East", lineup.East.DisplayName),
                Loc.Format("Seat_Partner", lineup.Partner.DisplayName),
                Loc.Format("Seat_West", lineup.West.DisplayName),
            };
            var session = new GameSession(names, bots ?? lineup.CreatePlayer, pace ?? GamePace.Instant, () => new SmartPlayer(), new Random(seed));
            var host = new FakeTableHost();
            return (session, new GameViewModel(session, lineup, host), host);
        }

        // A computer player that breaks on demand.
        private sealed class ExplodingPlayer : IPlayer
        {
            private readonly DummyPlayer player = new();

            private readonly Func<bool> explode;

            public ExplodingPlayer(Func<bool> explode) => this.explode = explode;

            public BidType GetBid(PlayerGetBidContext context) => this.explode() ? throw new InvalidOperationException("The computer broke.") : this.player.GetBid(context);

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.player.GetAnnounces(context);

            public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.explode() ? throw new InvalidOperationException("The computer broke.") : this.player.PlayCard(context);

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
