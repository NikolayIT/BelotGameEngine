namespace Belot.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.UI.Localization;

    /// <summary>
    /// The game table: what the game page shows and the commands it sends. It follows a
    /// <see cref="GameSession"/> through its events, which carry everything shown (the session's
    /// live view is ahead of them while the rules play forced moves), and reads the person's view
    /// only when the person has to decide, to offer exactly what the rules allow.
    /// <para>No MAUI types here: the UI tests compile this file and play whole games through it.</para>
    /// </summary>
    public sealed class GameViewModel : ObservableObject, IDisposable
    {
        private static readonly TimeSpan NoticeDuration = TimeSpan.FromSeconds(2.5);

        private static readonly BidType[] BidButtons =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades,
            BidType.NoTrumps, BidType.AllTrumps, BidType.Double, BidType.ReDouble, BidType.Pass,
        };

        private static readonly ValidAnnouncesService AnnouncesService = new();

        private readonly GameSession session;

        private readonly IGameTableHost host;

        private readonly SeatViewModel[] seats;

        private BidType contract = BidType.Pass;

        private PlayerPosition declarer;

        private int toastVersion;

        private int hintVersion;

        private int ourPoints;

        private int themPoints;

        private int hangingPoints;

        private int roundNumber;

        private bool isMyTurn;

        private bool isHintBusy;

        private bool isBidPanelVisible;

        private bool isAnnouncePanelVisible;

        private string statusMessage = string.Empty;

        private string? toastMessage;

        private CardSlot? lastTrickSouth;

        private CardSlot? lastTrickEast;

        private CardSlot? lastTrickNorth;

        private CardSlot? lastTrickWest;

        private bool isRoundOverlayVisible;

        private RoundResultModel? roundResult;

        private bool isGameOverlayVisible;

        private string gameOverlayIcon = string.Empty;

        private string gameOverlayTitle = string.Empty;

        private string gameOverlayBody = string.Empty;

        private string ratingChangeText = string.Empty;

        private bool isLeaving;

        private bool isDisposed;

        private int ourMatchWins;

        private int themMatchWins;

        public GameViewModel(GameSession session, Lineup lineup, IGameTableHost host)
        {
            this.session = session;
            this.Lineup = lineup;
            this.host = host;
            this.seats = Seats.All.Select(seat => new SeatViewModel(seat) { Name = session.GetName(seat) }).ToArray();
            this.BidOptions = BidButtons.Select(bid => new BidOption(bid)).ToArray();
            this.ApplyBidTexts();

            this.TapCardCommand = new RelayCommand<CardSlot>(this.OnTapCard);
            this.BidCommand = new RelayCommand<BidOption>(this.OnBid);
            this.ToggleAnnounceCommand = new RelayCommand<AnnounceOption>(this.OnToggleAnnounce);
            this.DeclareCommand = new RelayCommand(this.OnDeclare);
            this.HintCommand = new RelayCommand(() => _ = this.ShowHintAsync());
            this.RoundOverlayContinueCommand = new RelayCommand(this.OnRoundOverlayContinue);
            this.PlayAgainCommand = new RelayCommand(this.OnPlayAgain);
            this.LeaveCommand = new RelayCommand(this.OnLeave);

            session.RoundStarted += this.OnRoundStarted;
            session.TurnStarted += this.OnTurnStarted;
            session.BidMade += this.OnBidMade;
            session.ContractSettled += this.OnContractSettled;
            session.AnnouncesDeclared += this.OnAnnouncesDeclared;
            session.CardPlayed += this.OnCardPlayed;
            session.TrickCollected += this.OnTrickCollected;
            session.RoundFinished += this.OnRoundFinished;
            session.GameOver += this.OnGameOver;
            session.GameError += this.OnGameError;
            LocalizationManager.Instance.PropertyChanged += this.OnLanguageChanged;
        }

        public Lineup Lineup { get; }

        public SeatViewModel South => this.seats[0];

        public SeatViewModel East => this.seats[1];

        public SeatViewModel North => this.seats[2];

        public SeatViewModel West => this.seats[3];

        public ObservableCollection<CardSlot> MyHand { get; } = new();

        public IReadOnlyList<BidOption> BidOptions { get; }

        public ObservableCollection<AnnounceOption> AnnounceOptions { get; } = new();

        public RelayCommand<CardSlot> TapCardCommand { get; }

        public RelayCommand<BidOption> BidCommand { get; }

        public RelayCommand<AnnounceOption> ToggleAnnounceCommand { get; }

        public RelayCommand DeclareCommand { get; }

        public RelayCommand HintCommand { get; }

        public RelayCommand RoundOverlayContinueCommand { get; }

        public RelayCommand PlayAgainCommand { get; }

        public RelayCommand LeaveCommand { get; }

        public BidType Contract => this.contract;

        public bool HasContract => this.contract != BidType.Pass;

        public string ContractText => this.HasContract ? BelotTexts.ContractText(this.contract) : string.Empty;

        public string ContractColor => BelotTexts.ContractColor(this.contract);

        public string DeclarerName => this.HasContract ? this.session.GetName(this.declarer) : string.Empty;

        /// <summary>Gets the contract so far, for the bidding panel.</summary>
        public string CurrentBidText => this.HasContract
            ? LocalizationManager.Instance.Format("Bid_Current", this.ContractText, this.DeclarerName)
            : LocalizationManager.Instance["Bid_NoneYet"];

        public int UsPoints
        {
            get => this.ourPoints;
            private set => this.SetField(ref this.ourPoints, value, nameof(this.UsPoints), nameof(this.UsPointsDisplay));
        }

        public int ThemPoints
        {
            get => this.themPoints;
            private set => this.SetField(ref this.themPoints, value, nameof(this.ThemPoints), nameof(this.ThemPointsDisplay));
        }

        public string UsPointsDisplay => $"{this.ourPoints} / {this.session.PointsToWin}";

        public string ThemPointsDisplay => $"{this.themPoints} / {this.session.PointsToWin}";

        public int HangingPoints
        {
            get => this.hangingPoints;
            private set => this.SetField(ref this.hangingPoints, value, nameof(this.HangingPoints), nameof(this.HasHanging));
        }

        public bool HasHanging => this.hangingPoints > 0;

        public int RoundNumber
        {
            get => this.roundNumber;
            private set => this.SetField(ref this.roundNumber, value, nameof(this.RoundNumber));
        }

        public bool IsMyTurn
        {
            get => this.isMyTurn;
            private set => this.SetField(ref this.isMyTurn, value, nameof(this.IsMyTurn), nameof(this.IsHintVisible));
        }

        public bool IsHintVisible => this.isMyTurn && AppSettings.AssistsEnabled;

        public bool IsHintBusy
        {
            get => this.isHintBusy;
            private set => this.SetField(ref this.isHintBusy, value, nameof(this.IsHintBusy));
        }

        public bool IsBidPanelVisible
        {
            get => this.isBidPanelVisible;
            private set => this.SetField(ref this.isBidPanelVisible, value, nameof(this.IsBidPanelVisible));
        }

        public bool IsAnnouncePanelVisible
        {
            get => this.isAnnouncePanelVisible;
            private set => this.SetField(ref this.isAnnouncePanelVisible, value, nameof(this.IsAnnouncePanelVisible));
        }

        /// <summary>Gets the text of the declare button: "Declare" with a combination selected, "Don't declare" without.</summary>
        public string DeclareButtonText => this.AnnounceOptions.Any(x => x.IsSelected)
            ? LocalizationManager.Instance["Game_Declare"]
            : LocalizationManager.Instance["Game_DeclareNothing"];

        public string StatusMessage
        {
            get => this.statusMessage;
            private set => this.SetField(ref this.statusMessage, value ?? string.Empty, nameof(this.StatusMessage));
        }

        public string? ToastMessage
        {
            get => this.toastMessage;
            private set => this.SetField(ref this.toastMessage, value, nameof(this.ToastMessage), nameof(this.IsToastVisible));
        }

        public bool IsToastVisible => !string.IsNullOrEmpty(this.toastMessage);

        public CardSlot? LastTrickSouth
        {
            get => this.lastTrickSouth;
            private set => this.SetField(ref this.lastTrickSouth, value, nameof(this.LastTrickSouth), nameof(this.HasLastTrick));
        }

        public CardSlot? LastTrickEast
        {
            get => this.lastTrickEast;
            private set => this.SetField(ref this.lastTrickEast, value, nameof(this.LastTrickEast));
        }

        public CardSlot? LastTrickNorth
        {
            get => this.lastTrickNorth;
            private set => this.SetField(ref this.lastTrickNorth, value, nameof(this.LastTrickNorth));
        }

        public CardSlot? LastTrickWest
        {
            get => this.lastTrickWest;
            private set => this.SetField(ref this.lastTrickWest, value, nameof(this.LastTrickWest));
        }

        public bool HasLastTrick => this.lastTrickSouth != null;

        public bool IsRoundOverlayVisible
        {
            get => this.isRoundOverlayVisible;
            private set => this.SetField(ref this.isRoundOverlayVisible, value, nameof(this.IsRoundOverlayVisible));
        }

        public RoundResultModel? RoundResult
        {
            get => this.roundResult;
            private set => this.SetField(ref this.roundResult, value, nameof(this.RoundResult));
        }

        public bool IsGameOverlayVisible
        {
            get => this.isGameOverlayVisible;
            private set => this.SetField(ref this.isGameOverlayVisible, value, nameof(this.IsGameOverlayVisible));
        }

        public string GameOverlayIcon
        {
            get => this.gameOverlayIcon;
            private set => this.SetField(ref this.gameOverlayIcon, value, nameof(this.GameOverlayIcon));
        }

        public string GameOverlayTitle
        {
            get => this.gameOverlayTitle;
            private set => this.SetField(ref this.gameOverlayTitle, value, nameof(this.GameOverlayTitle));
        }

        public string GameOverlayBody
        {
            get => this.gameOverlayBody;
            private set => this.SetField(ref this.gameOverlayBody, value, nameof(this.GameOverlayBody));
        }

        public string RatingChangeText
        {
            get => this.ratingChangeText;
            private set => this.SetField(ref this.ratingChangeText, value, nameof(this.RatingChangeText), nameof(this.IsRatingChangeVisible));
        }

        public bool IsRatingChangeVisible => this.ratingChangeText.Length > 0;

        public string MatchScoreText => $"{this.ourMatchWins} – {this.themMatchWins}";

        public bool HasMatchHistory => this.ourMatchWins + this.themMatchWins > 1;

        public SeatViewModel Seat(PlayerPosition seat) => this.seats[seat.Index()];

        public void StartGame()
        {
            if (!this.isDisposed && !this.isLeaving)
            {
                this.session.Start();
            }
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.hintVersion++;
            this.toastVersion++;
            this.session.RoundStarted -= this.OnRoundStarted;
            this.session.TurnStarted -= this.OnTurnStarted;
            this.session.BidMade -= this.OnBidMade;
            this.session.ContractSettled -= this.OnContractSettled;
            this.session.AnnouncesDeclared -= this.OnAnnouncesDeclared;
            this.session.CardPlayed -= this.OnCardPlayed;
            this.session.TrickCollected -= this.OnTrickCollected;
            this.session.RoundFinished -= this.OnRoundFinished;
            this.session.GameOver -= this.OnGameOver;
            this.session.GameError -= this.OnGameError;
            LocalizationManager.Instance.PropertyChanged -= this.OnLanguageChanged;
            this.session.Stop();
        }

        /// <summary>Asks for a hint and shows it for a while; awaitable for tests.</summary>
        internal async Task ShowHintAsync()
        {
            if (this.isDisposed || this.isLeaving || !this.IsHintVisible || this.IsHintBusy)
            {
                return;
            }

            this.IsHintBusy = true;
            try
            {
                var hint = await this.session.GetHintAsync();
                if (hint == null)
                {
                    return;
                }

                this.ClearHints();
                var version = ++this.hintVersion;
                switch (hint.Type)
                {
                    case BelotActionType.Bid:
                        var option = this.BidOptions.FirstOrDefault(x => x.Bid == hint.BidType);
                        if (option != null)
                        {
                            option.IsHinted = true;
                        }

                        break;
                    case BelotActionType.Announce:
                        this.ApplyAnnounceHint(hint);
                        break;
                    default:
                        var slot = this.MyHand.FirstOrDefault(x => x.Card == hint.Card);
                        if (slot != null)
                        {
                            slot.IsHinted = true;
                        }

                        break;
                }

                this.host.After(NoticeDuration, () =>
                {
                    if (this.hintVersion == version)
                    {
                        this.ClearHints();
                    }
                });
            }
            catch (Exception)
            {
                // A failed hint must not end the game or fault the fire-and-forget command.
                if (!this.isDisposed && !this.isLeaving && this.IsMyTurn)
                {
                    this.ShowToast(LocalizationManager.Instance["Hint_Unavailable"]);
                }
            }
            finally
            {
                if (!this.isDisposed)
                {
                    this.IsHintBusy = false;
                }
            }
        }

        private static bool Conflict(BelotAnnounce first, BelotAnnounce second) =>
            AnnouncesService.HaveCommonCards(new Announce(first.Type, first.Card), new Announce(second.Type, second.Card));

        private void ApplyAnnounceHint(BelotAction hint)
        {
            foreach (var option in this.AnnounceOptions)
            {
                option.IsSelected = false;
                option.IsHinted = false;
            }

            var selected = new List<BelotAnnounce>();
            foreach (var announce in hint.Announces)
            {
                // Announce keeps its card internal. Its public, invariant text identifies both
                // rank and suit, including two offered sequences of the same length.
                var option = this.AnnounceOptions.FirstOrDefault(x =>
                    new Announce(x.Announce.Type, x.Announce.Card).ToString() == announce.ToString());
                if (option != null && selected.All(x => !Conflict(x, option.Announce)))
                {
                    // The engine accepts the first of any overlapping combinations in the
                    // returned order; show the same selection instead of checking both.
                    option.IsSelected = true;
                    option.IsHinted = true;
                    selected.Add(option.Announce);
                }
            }

            this.Raise(nameof(this.DeclareButtonText));
        }

        private void OnRoundStarted(DealInfo deal)
        {
            this.IsBidPanelVisible = false;
            this.IsAnnouncePanelVisible = false;
            this.AnnounceOptions.Clear();
            this.SetContract(BidType.Pass, deal.FirstToPlay);
            foreach (var seat in this.seats)
            {
                seat.PlayedCard = null;
                seat.BubbleText = string.Empty;
                seat.DeclaredText = string.Empty;
                seat.IsDeclarer = false;
                seat.IsToMove = false;
                seat.IsDealer = seat.Seat == deal.Dealer;
                seat.CardCount = deal.CardCounts[seat.Seat.Index()];
            }

            this.ShowHand(deal.MyHand, BidType.Pass);
            this.UsPoints = deal.SouthNorthPoints;
            this.ThemPoints = deal.EastWestPoints;
            this.HangingPoints = deal.HangingPoints;
            this.RoundNumber = deal.RoundNumber;
            this.LastTrickSouth = this.LastTrickEast = this.LastTrickNorth = this.LastTrickWest = null;
            this.IsMyTurn = false;
            this.StatusMessage = string.Empty;
        }

        private void OnTurnStarted(TurnInfo turn)
        {
            foreach (var seat in this.seats)
            {
                seat.IsToMove = seat.Seat == turn.Seat;
            }

            if (!turn.IsHuman)
            {
                this.IsMyTurn = false;
                this.StatusMessage = LocalizationManager.Instance.Format("Status_Thinking", this.session.GetName(turn.Seat));
                return;
            }

            // Every turn of the person is a point where the table and the rules agree: show the
            // hand as the view has it and offer exactly what may be done.
            var view = this.session.GetView(Seats.Person)!;
            this.ShowHand(view.Hand, turn.Decision == BelotDecision.Bid ? BidType.Pass : view.Contract.Type);
            this.IsMyTurn = true;
            switch (turn.Decision)
            {
                case BelotDecision.Bid:
                    foreach (var option in this.BidOptions)
                    {
                        option.IsHinted = false;
                        option.IsEnabled = option.Bid == BidType.Pass || view.AvailableBids.HasFlag(option.Bid);
                    }

                    this.Raise(nameof(this.CurrentBidText));
                    this.IsBidPanelVisible = true;
                    this.StatusMessage = LocalizationManager.Instance["Status_YourBid"];
                    break;
                case BelotDecision.Announce:
                    this.OfferAnnounces(view);
                    this.StatusMessage = LocalizationManager.Instance["Status_YourDeclaration"];
                    break;
                default:
                    var playable = new HashSet<Card>(view.PlayableCards);
                    var belotes = new HashSet<Card>(view.BeloteCards);
                    foreach (var slot in this.MyHand)
                    {
                        slot.IsPlayable = playable.Contains(slot.Card!);
                        slot.BadgeText = AppSettings.AssistsEnabled && belotes.Contains(slot.Card!)
                            ? LocalizationManager.Instance["Game_BeloteBadge"]
                            : string.Empty;
                    }

                    this.StatusMessage = LocalizationManager.Instance["Status_YourTurn"];
                    this.Vibrate(false);
                    break;
            }
        }

        private void OfferAnnounces(BelotSeatView view)
        {
            this.AnnounceOptions.Clear();
            var selected = new List<BelotAnnounce>();
            foreach (var announce in view.AvailableAnnounces)
            {
                // Everything that does not share a card with a combination already chosen: the
                // offered order puts the carres first, as the rules prefer.
                var option = new AnnounceOption(announce) { IsSelected = selected.All(x => !Conflict(x, announce)) };
                if (option.IsSelected)
                {
                    selected.Add(announce);
                }

                this.AnnounceOptions.Add(option);
            }

            this.Raise(nameof(this.DeclareButtonText));
            this.IsAnnouncePanelVisible = true;
        }

        private void OnBidMade(BidInfo bid)
        {
            this.Seat(bid.Seat).BubbleText = BelotTexts.ShortBidText(bid.Bid);
            this.SetContract(bid.ContractType, bid.ContractPlayer);
            if (bid.Seat == Seats.Person)
            {
                this.IsBidPanelVisible = false;
                this.EndMyTurn();
            }
        }

        private void OnContractSettled(ContractInfo settled)
        {
            this.SetContract(settled.Contract, settled.Declarer);
            foreach (var seat in this.seats)
            {
                seat.BubbleText = string.Empty;
                seat.IsDeclarer = seat.Seat == settled.Declarer;
                seat.CardCount = settled.CardCounts[seat.Seat.Index()];
            }

            this.ShowHand(settled.MyHand, settled.Contract);
            this.StatusMessage = string.Empty;
        }

        private void OnAnnouncesDeclared(AnnounceInfo declared)
        {
            var seat = this.Seat(declared.Seat);
            if (declared.Combinations.Count > 0)
            {
                var text = string.Join(", ", declared.Combinations.Select(x => BelotTexts.CombinationText(x.Type, x.Card)));
                seat.BubbleText = text;
                seat.DeclaredText = seat.HasDeclared ? $"{seat.DeclaredText}, {text}" : text;
            }

            if (declared.Seat == Seats.Person)
            {
                this.IsAnnouncePanelVisible = false;
                this.EndMyTurn();
            }
        }

        private void OnCardPlayed(CardInfo played)
        {
            var seat = this.Seat(played.Seat);
            if (played.TrickNumber == 2 && played.IndexInTrick == 0)
            {
                // The declarations were for the first trick; their summary stays under the names.
                foreach (var each in this.seats)
                {
                    each.BubbleText = string.Empty;
                }
            }

            seat.PlayedCard = new CardSlot(played.Card);
            seat.CardCount--;
            if (played.Seat == Seats.Person)
            {
                var slot = this.MyHand.FirstOrDefault(x => x.Card == played.Card);
                if (slot != null)
                {
                    this.MyHand.Remove(slot);
                }

                this.EndMyTurn();
            }

            if (played.Belote)
            {
                var belote = BelotTexts.CombinationText(AnnounceType.Belot, played.Card);
                seat.DeclaredText = seat.HasDeclared ? $"{seat.DeclaredText}, {belote}" : belote;
                this.ShowToast(played.Seat == Seats.Person
                    ? LocalizationManager.Instance["Toast_Belote"]
                    : LocalizationManager.Instance.Format("Toast_SeatBelote", this.session.GetName(played.Seat)));
            }
        }

        private void OnTrickCollected(TrickInfo trick)
        {
            this.LastTrickEast = Slot(trick.CardOf(PlayerPosition.East));
            this.LastTrickNorth = Slot(trick.CardOf(PlayerPosition.North));
            this.LastTrickWest = Slot(trick.CardOf(PlayerPosition.West));
            this.LastTrickSouth = Slot(trick.CardOf(PlayerPosition.South));
            foreach (var seat in this.seats)
            {
                seat.PlayedCard = null;
            }

            static CardSlot? Slot(Card? card) => card == null ? null : new CardSlot(card);
        }

        private void OnRoundFinished(RoundEndInfo round)
        {
            this.ShowRound(round);
            this.IsRoundOverlayVisible = true;
            this.Vibrate(true);
        }

        private void ShowRound(RoundEndInfo round)
        {
            foreach (var seat in this.seats)
            {
                seat.IsToMove = false;
            }

            this.IsMyTurn = false;
            this.StatusMessage = string.Empty;
            this.RoundResult = new RoundResultModel(round, this.session.GetName);
            this.UsPoints = round.SouthNorthGamePoints;
            this.ThemPoints = round.EastWestGamePoints;
            this.HangingPoints = round.HangingAfter;
        }

        private void OnGameOver(GameOverInfo over)
        {
            this.ShowRound(over.LastRound);
            var text = LocalizationManager.Instance;
            var won = over.WeWon;
            if (won)
            {
                this.ourMatchWins++;
            }
            else
            {
                this.themMatchWins++;
            }

            var change = PlayerRatingStore.RecordResult(this.Lineup.Partner.Elo, this.Lineup.RivalsElo, won);
            MatchHistoryStore.Add(new MatchHistoryEntry(
                this.Lineup.Partner.Id,
                this.Lineup.West.Id,
                this.Lineup.East.Id,
                over.LastRound.SouthNorthGamePoints,
                over.LastRound.EastWestGamePoints,
                won,
                DateTime.UtcNow));
            foreach (var rival in this.Lineup.RivalLevels)
            {
                OpponentStatsStore.Record(rival.Id, won);
            }

            this.GameOverlayIcon = won ? "🏆" : "😞";
            this.GameOverlayTitle = won ? text["GameOver_Won"] : text["GameOver_Lost"];
            this.GameOverlayBody = text.Format("GameOver_Score", over.LastRound.SouthNorthGamePoints, over.LastRound.EastWestGamePoints);
            this.RatingChangeText = text.Format("Rating_Change", change.OldElo, change.NewElo, change.Delta >= 0 ? $"+{change.Delta}" : change.Delta.ToString());
            this.Raise(nameof(this.MatchScoreText), nameof(this.HasMatchHistory));
            this.IsRoundOverlayVisible = false;
            this.IsGameOverlayVisible = true;
            this.Vibrate(true);
        }

        private void OnGameError(Exception error)
        {
            this.IsBidPanelVisible = false;
            this.IsAnnouncePanelVisible = false;
            this.IsRoundOverlayVisible = false;
            this.EndMyTurn();
            foreach (var seat in this.seats)
            {
                seat.IsToMove = false;
            }

            this.StatusMessage = string.Empty;
            this.GameOverlayIcon = "⚠️";
            this.GameOverlayTitle = LocalizationManager.Instance["Error_Title"];
            this.GameOverlayBody = error.Message;
            this.RatingChangeText = string.Empty;
            this.IsGameOverlayVisible = true;
        }

        private void OnTapCard(CardSlot? slot)
        {
            if (!this.isDisposed && !this.isLeaving && slot?.Card != null && this.session.TryPlay(slot.Card))
            {
                this.EndMyTurn();
            }
        }

        private void OnBid(BidOption? option)
        {
            if (!this.isDisposed && !this.isLeaving && option is { IsEnabled: true } && this.session.TryBid(option.Bid))
            {
                this.EndMyTurn();
            }
        }

        private void OnToggleAnnounce(AnnounceOption? option)
        {
            if (this.isDisposed || this.isLeaving || !this.session.IsAwaiting(BelotDecision.Announce)
                || option == null || !this.AnnounceOptions.Contains(option))
            {
                return;
            }

            option.IsSelected = !option.IsSelected;
            if (option.IsSelected)
            {
                // A card may take part in only one combination.
                foreach (var other in this.AnnounceOptions)
                {
                    if (other != option && Conflict(other.Announce, option.Announce))
                    {
                        other.IsSelected = false;
                    }
                }
            }

            this.Raise(nameof(this.DeclareButtonText));
        }

        private void OnDeclare()
        {
            if (!this.isDisposed && !this.isLeaving
                && this.session.TryDeclare(this.AnnounceOptions.Where(x => x.IsSelected).Select(x => x.Announce).ToArray()))
            {
                this.EndMyTurn();
            }
        }

        private void OnRoundOverlayContinue()
        {
            if (this.isDisposed || this.isLeaving || !this.IsRoundOverlayVisible)
            {
                return;
            }

            this.IsRoundOverlayVisible = false;
            this.session.Continue();
        }

        private void OnPlayAgain()
        {
            // A second tap on the same button must not deal another game.
            if (this.isDisposed || this.isLeaving || !this.IsGameOverlayVisible)
            {
                return;
            }

            this.IsGameOverlayVisible = false;
            this.RatingChangeText = string.Empty;
            this.session.Restart();
        }

        private void OnLeave()
        {
            if (this.isDisposed || this.isLeaving)
            {
                return;
            }

            this.isLeaving = true;
            this.EndMyTurn();
            this.session.Stop();
            this.host.Leave();
        }

        private void EndMyTurn()
        {
            this.IsMyTurn = false;
            this.IsBidPanelVisible = false;
            this.IsAnnouncePanelVisible = false;
            this.ClearHints();
            foreach (var slot in this.MyHand)
            {
                slot.IsPlayable = true;
                slot.BadgeText = string.Empty;
            }
        }

        private void ClearHints()
        {
            foreach (var option in this.BidOptions)
            {
                option.IsHinted = false;
            }

            foreach (var announce in this.AnnounceOptions)
            {
                announce.IsHinted = false;
            }

            foreach (var slot in this.MyHand)
            {
                slot.IsHinted = false;
            }
        }

        // Rebuilds the hand only when it changed, so a card being tapped is not replaced under the finger.
        private void ShowHand(IEnumerable<Card> cards, BidType sortContract)
        {
            var sorted = HandOrder.Sort(cards, sortContract);
            if (sorted.SequenceEqual(this.MyHand.Select(x => x.Card!)))
            {
                return;
            }

            this.MyHand.Clear();
            foreach (var card in sorted)
            {
                this.MyHand.Add(new CardSlot(card));
            }

            this.South.CardCount = sorted.Count;
        }

        private void SetContract(BidType type, PlayerPosition player)
        {
            this.contract = type;
            this.declarer = player;
            this.Raise(
                nameof(this.Contract),
                nameof(this.HasContract),
                nameof(this.ContractText),
                nameof(this.ContractColor),
                nameof(this.DeclarerName),
                nameof(this.CurrentBidText));
        }

        private void ShowToast(string message)
        {
            this.ToastMessage = message;
            var version = ++this.toastVersion;
            this.host.After(NoticeDuration, () =>
            {
                if (this.toastVersion == version)
                {
                    this.ToastMessage = null;
                }
            });
        }

        private void Vibrate(bool isLong)
        {
            if (AppSettings.HapticsEnabled)
            {
                this.host.Vibrate(isLong);
            }
        }

        private void ApplyBidTexts()
        {
            foreach (var option in this.BidOptions)
            {
                option.Text = BelotTexts.BidText(option.Bid);
            }
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            this.ApplyBidTexts();
            this.Raise(nameof(this.CurrentBidText), nameof(this.ContractText), nameof(this.DeclareButtonText));
        }
    }
}
