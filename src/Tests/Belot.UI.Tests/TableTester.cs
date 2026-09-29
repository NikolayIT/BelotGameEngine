namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    // Plays games at the game table (GameViewModel) the way people do: only through its commands,
    // looking only at what it shows. At every decision it checks the screen against the person's
    // view: the hand in display order, what may be bid, declared or played, the card counts, the
    // contract, the score, the cards on the table and the last trick. It also checks every deal's
    // result screen and the game-over screen against the session's own results.
    internal sealed class TableTester
    {
        private static readonly LocalizationManager Loc = LocalizationManager.Instance;

        private readonly GameSession session;

        private readonly GameViewModel table;

        private readonly Random random;

        private TaskCompletionSource changed = NewSignal();

        public TableTester(GameSession session, GameViewModel table, int seed)
        {
            this.session = session;
            this.table = table;
            this.random = new Random(seed);

            // Subscribed after the table, so the table has already updated when these run.
            table.PropertyChanged += (_, _) => this.Pulse();
            session.RoundStarted += _ => this.Pulse();
            session.TurnStarted += _ => this.Pulse();
            session.BidMade += bid => this.Moved(bid.Seat, bid.IsAuto);
            session.AnnouncesDeclared += declared => this.Moved(declared.Seat, false);
            session.CardPlayed += card => this.Moved(card.Seat, card.IsAuto);
            session.TrickCollected += _ => this.Pulse();
            session.RoundFinished += round =>
            {
                this.Rounds.Add(round);
                this.Pulse();
            };
            session.GameOver += over =>
            {
                this.Rounds.Add(over.LastRound);
                this.Over = over;
                this.Pulse();
            };
            session.GameError += error =>
            {
                this.Errors.Add(error);
                this.Pulse();
            };
        }

        // The person's own decisions carried out so far.
        public int Moves { get; private set; }

        public int Decisions { get; private set; }

        public HashSet<BelotDecision> DecisionKinds { get; } = new();

        public List<RoundEndInfo> Rounds { get; } = new();

        public GameOverInfo? Over { get; private set; }

        public List<Exception> Errors { get; } = new();

        // Called at every decision, before the move, with the decision.
        public Func<BelotDecision, Task>? OnDecision { get; set; }

        // Waits until the condition holds, re-checking whenever the table or the session changes.
        public async Task Until(Func<bool> condition, string what)
        {
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (!condition())
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    throw new TimeoutException($"The table never got to: {what}.");
                }

                var signal = this.changed;
                try
                {
                    await signal.Task.WaitAsync(remaining);
                }
                catch (TimeoutException)
                {
                    throw new TimeoutException($"The table never got to: {what}.");
                }
            }
        }

        // Waits for the next thing to do at the table and does it; returns false once the game is over.
        public async Task<bool> StepAsync()
        {
            await this.Until(
                () => this.table.IsGameOverlayVisible || this.table.IsRoundOverlayVisible || this.table.IsMyTurn,
                "a decision or a result");
            Assert.Empty(this.Errors);

            if (this.table.IsGameOverlayVisible)
            {
                this.CheckGameOver();
                return false;
            }

            if (this.table.IsRoundOverlayVisible)
            {
                this.CheckRoundResult(this.Rounds[^1]);
                this.table.RoundOverlayContinueCommand.Execute(null);
                Assert.False(this.table.IsRoundOverlayVisible);
                return true;
            }

            var decision = this.session.AwaitedDecision;
            Assert.NotEqual(BelotDecision.None, decision);
            this.Decisions++;
            this.DecisionKinds.Add(decision);
            this.CheckTable(decision);
            if (this.OnDecision != null)
            {
                await this.OnDecision(decision);
            }

            var before = this.Moves;
            this.MakeAMove(decision);
            await this.Until(() => this.Moves > before, "the move being made");
            return true;
        }

        public async Task PlayToTheEndAsync()
        {
            while (await this.StepAsync())
            {
            }
        }

        private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        private void Moved(PlayerPosition seat, bool isAuto)
        {
            if (seat == Seats.Person && !isAuto)
            {
                this.Moves++;
            }

            this.Pulse();
        }

        private void Pulse()
        {
            var signal = this.changed;
            this.changed = NewSignal();
            signal.TrySetResult();
        }

        private void MakeAMove(BelotDecision decision)
        {
            switch (decision)
            {
                case BelotDecision.Bid:
                    var options = this.table.BidOptions.Where(o => o.IsEnabled && o.IsVisible && o.Bid != BidType.Pass).ToList();
                    var pass = this.table.BidOptions.Single(o => o.Bid == BidType.Pass);
                    this.table.BidCommand.Execute(options.Count == 0 || this.random.Next(3) != 0 ? pass : options[this.random.Next(options.Count)]);
                    break;
                case BelotDecision.Announce:
                    // Tap some combinations on and off; the table never keeps two that share a card.
                    for (var taps = this.random.Next(4); taps > 0; taps--)
                    {
                        this.table.ToggleAnnounceCommand.Execute(this.table.AnnounceOptions[this.random.Next(this.table.AnnounceOptions.Count)]);
                        this.CheckNoSharedCards();
                    }

                    this.table.DeclareCommand.Execute(null);
                    break;
                default:
                    var playable = this.table.MyHand.Where(s => s.IsPlayable).ToList();

                    // A newly explicit forced move must not shift later seeded choices.
                    this.table.TapCardCommand.Execute(playable[playable.Count == 1 ? 0 : this.random.Next(playable.Count)]);
                    break;
            }
        }

        private void CheckNoSharedCards()
        {
            var selected = this.table.AnnounceOptions.Where(o => o.IsSelected).Select(o => o.Announce).ToList();
            foreach (var first in selected)
            {
                Assert.All(selected.Where(x => x != first), second => Assert.False(RandomMoves.Conflict(first, second)));
            }

            Assert.Equal(selected.Count > 0 ? Loc["Game_Declare"] : Loc["Game_DeclareNothing"], this.table.DeclareButtonText);
        }

        // Everything the table shows at a decision, against the person's view.
        private void CheckTable(BelotDecision decision)
        {
            var view = this.session.GetView(Seats.Person)!;
            Assert.True(this.table.IsMyTurn);
            Assert.Equal(decision, view.Decision);

            // The hand, in display order: the contract's trumps first once there is one.
            var contract = decision == BelotDecision.Bid ? BidType.Pass : view.Contract.Type;
            Assert.Equal(HandOrder.Sort(view.Hand, contract), this.table.MyHand.Select(s => s.Card));
            Assert.All(this.table.MyHand, s => Assert.False(s.IsFaceDown));
            Assert.DoesNotContain(this.table.MyHand, s => s.IsHinted);

            foreach (var seat in Seats.All)
            {
                var shown = this.table.Seat(seat);
                Assert.Equal(this.session.GetName(seat), shown.Name);
                Assert.Equal(view.CardCounts[seat.Index()], shown.CardCount);
                Assert.Equal(shown.CardCount, shown.Backs.Count);
                Assert.Equal(seat == Seats.Person, shown.IsToMove);
                Assert.Equal(seat == Seats.Previous(view.FirstToPlayInTheRound), shown.IsDealer);

                // The cards on the table are the trick in progress.
                var played = view.CurrentTrick.FirstOrDefault(c => c.Player == seat)?.Card;
                Assert.Equal(played, shown.PlayedCard?.Card);
            }

            Assert.Equal(view.SouthNorthPoints, this.table.UsPoints);
            Assert.Equal(view.EastWestPoints, this.table.ThemPoints);
            Assert.Equal(view.HangingPoints, this.table.HangingPoints);
            Assert.Equal(view.RoundNumber, this.table.RoundNumber);
            Assert.Equal(view.Contract.Type, this.table.Contract);
            if (view.Contract.Type != BidType.Pass)
            {
                Assert.Equal(this.session.GetName(view.Contract.Player), this.table.DeclarerName);
                Assert.Equal(decision != BelotDecision.Bid, this.table.Seat(view.Contract.Player).IsDeclarer);
            }

            // The last-trick corner shows this deal's last trick.
            if (view.Tricks.Count == 0)
            {
                Assert.False(this.table.HasLastTrick);
            }
            else
            {
                var last = view.Tricks[^1];
                Card? Of(PlayerPosition seat) => last.Cards.First(c => c.Player == seat).Card;
                Assert.Equal(Of(PlayerPosition.South), this.table.LastTrickSouth?.Card);
                Assert.Equal(Of(PlayerPosition.East), this.table.LastTrickEast?.Card);
                Assert.Equal(Of(PlayerPosition.North), this.table.LastTrickNorth?.Card);
                Assert.Equal(Of(PlayerPosition.West), this.table.LastTrickWest?.Card);
            }

            Assert.Equal(AppSettings.AssistsEnabled, this.table.IsHintVisible);
            Assert.Equal(decision == BelotDecision.Bid, this.table.IsBidPanelVisible);
            Assert.Equal(decision == BelotDecision.Announce, this.table.IsAnnouncePanelVisible);
            switch (decision)
            {
                case BelotDecision.Bid:
                    foreach (var option in this.table.BidOptions)
                    {
                        var allowed = option.Bid == BidType.Pass || view.AvailableBids.HasFlag(option.Bid);
                        Assert.Equal(allowed, option.IsEnabled);
                        Assert.False(option.IsHinted);
                    }

                    break;
                case BelotDecision.Announce:
                    // Everything offered, and at first all of it that does not share a card.
                    Assert.Equal(
                        view.AvailableAnnounces.Select(a => (a.Type, a.Card)),
                        this.table.AnnounceOptions.Select(o => (o.Announce.Type, o.Announce.Card)));
                    var chosen = new List<BelotAnnounce>();
                    foreach (var option in this.table.AnnounceOptions)
                    {
                        Assert.Equal(chosen.All(c => !RandomMoves.Conflict(c, option.Announce)), option.IsSelected);
                        if (option.IsSelected)
                        {
                            chosen.Add(option.Announce);
                        }
                    }

                    this.CheckNoSharedCards();
                    break;
                default:
                    Assert.Equal(
                        view.PlayableCards.OrderBy(c => c.GetHashCode()),
                        this.table.MyHand.Where(s => s.IsPlayable).Select(s => s.Card!).OrderBy(c => c.GetHashCode()));

                    // Beginner assists: a belote badge on the cards that would claim one.
                    foreach (var slot in this.table.MyHand)
                    {
                        var badge = AppSettings.AssistsEnabled && view.BeloteCards.Contains(slot.Card!);
                        Assert.Equal(badge ? Loc["Game_BeloteBadge"] : string.Empty, slot.BadgeText);
                    }

                    break;
            }
        }

        private void CheckRoundResult(RoundEndInfo round)
        {
            Assert.False(this.table.IsMyTurn);
            Assert.False(this.table.IsBidPanelVisible);
            Assert.False(this.table.IsAnnouncePanelVisible);
            Assert.Equal(round.SouthNorthGamePoints, this.table.UsPoints);
            Assert.Equal(round.EastWestGamePoints, this.table.ThemPoints);
            Assert.Equal(round.HangingAfter, this.table.HangingPoints);
            this.CheckRoundScreen(round);
        }

        // The deal's screen: its outcome told from our side, and the points adding up.
        private void CheckRoundScreen(RoundEndInfo round)
        {
            var screen = this.table.RoundResult!;
            Assert.Same(round, screen.Round);
            var ours = Seats.IsUs(round.Declarer);
            var title = round.Outcome switch
            {
                RoundOutcome.PassedOut => Loc["Round_PassedOut"],
                RoundOutcome.Hanging => Loc["Round_Hanging"],
                RoundOutcome.Made => ours ? Loc["Round_WeMade"] : Loc["Round_TheyMade"],
                _ => ours ? Loc["Round_WeInside"] : Loc["Round_TheyInside"],
            };
            Assert.Equal(title, screen.Title);
            Assert.Equal(round.Outcome == RoundOutcome.PassedOut, screen.IsPassedOut);
            Assert.Equal(round.SouthNorthGamePoints, screen.UsTotal);
            Assert.Equal(round.EastWestGamePoints, screen.ThemTotal);
            Assert.Equal($"+{round.SouthNorthAwarded}", screen.UsAwarded);
            Assert.Equal($"+{round.EastWestAwarded}", screen.ThemAwarded);
            Assert.Equal(round.HangingAfter > 0, screen.HasHanging);
            if (screen.IsPassedOut)
            {
                return;
            }

            Assert.Equal(round.SouthNorthInDeal, screen.UsCards + screen.UsCombinations + screen.UsCapot);
            Assert.Equal(round.EastWestInDeal, screen.ThemCards + screen.ThemCombinations + screen.ThemCapot);

            // Card points: 162 in a suit, 258 in all trumps, 260 in no trumps (doubled).
            Assert.InRange(screen.UsCards, 0, 260);
            Assert.InRange(screen.ThemCards, 0, 260);
            Assert.Equal(round.IsCapot ? 90 : 0, screen.UsCapot + screen.ThemCapot);
            Assert.Equal(round.Combinations.Any(c => Seats.IsUs(c.Seat)), screen.HasUsCombinations);
            Assert.Equal(round.Combinations.Any(c => !Seats.IsUs(c.Seat)), screen.HasThemCombinations);
        }

        private void CheckGameOver()
        {
            var over = this.Over!;
            Assert.True(this.Rounds[^1].IsGameOver);
            Assert.False(this.table.IsMyTurn);
            Assert.False(this.table.IsRoundOverlayVisible);
            Assert.Equal(over.WeWon ? Loc["GameOver_Won"] : Loc["GameOver_Lost"], this.table.GameOverlayTitle);
            Assert.Equal(Loc.Format("GameOver_Score", over.LastRound.SouthNorthGamePoints, over.LastRound.EastWestGamePoints), this.table.GameOverlayBody);
            this.CheckRoundScreen(over.LastRound);
            var winners = over.WeWon ? over.LastRound.SouthNorthGamePoints : over.LastRound.EastWestGamePoints;
            var losers = over.WeWon ? over.LastRound.EastWestGamePoints : over.LastRound.SouthNorthGamePoints;
            Assert.True(winners >= BelotMatch.PointsToWin && winners > losers);
        }
    }
}
