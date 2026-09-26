namespace Belot.UI.Pages
{
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;

    using Belot.UI.Game;
    using Belot.UI.Localization;

    [QueryProperty(nameof(PlayerName), "name")]
    [QueryProperty(nameof(PartnerId), "partner")]
    [QueryProperty(nameof(WestId), "west")]
    [QueryProperty(nameof(EastId), "east")]
    [SuppressMessage("Design", "CA1001", Justification = "The view model is disposed in OnDisappearing, when the page leaves the screen.")]
    public sealed partial class GamePage : ContentPage, IGameTableHost
    {
        // One leave prompt at a time (the back button and Menu both ask).
        private readonly OneAtATime leaving = new();

        private GameSession? session;

        private GameViewModel? viewModel;

        private bool started;

        public GamePage()
        {
            this.InitializeComponent();
        }

        public string PlayerName { get; set; } = string.Empty;

        public string PartnerId { get; set; } = "smart";

        public string WestId { get; set; } = "smart";

        public string EastId { get; set; } = "smart";

        void IGameTableHost.After(TimeSpan delay, Action action) => this.Dispatcher.DispatchDelayed(delay, action);

        void IGameTableHost.Vibrate(bool isLong)
        {
            try
            {
                HapticFeedback.Default.Perform(isLong ? HapticFeedbackType.LongPress : HapticFeedbackType.Click);
            }
            catch
            {
                // Not supported on this platform (e.g. desktop): skip.
            }
        }

        void IGameTableHost.Leave() => _ = Shell.Current?.GoToAsync("..");

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SetKeepScreenOn(true);
            if (this.started)
            {
                return;
            }

            this.started = true;
            var text = LocalizationManager.Instance;
            var lineup = new Lineup(AiLevels.ById(this.PartnerId), AiLevels.ById(this.WestId), AiLevels.ById(this.EastId));
            var name = string.IsNullOrWhiteSpace(this.PlayerName) ? text["Start_DefaultName"] : this.PlayerName;
            var names = new[]
            {
                name,
                text.Format("Seat_East", lineup.East.DisplayName),
                text.Format("Seat_Partner", lineup.Partner.DisplayName),
                text.Format("Seat_West", lineup.West.DisplayName),
            };
            this.session = new GameSession(names, lineup.CreatePlayer, AppSettings.Pace);
            this.viewModel = new GameViewModel(this.session, lineup, this);
            this.viewModel.PropertyChanged += this.OnViewModelPropertyChanged;
            foreach (var seat in new[] { this.viewModel.South, this.viewModel.East, this.viewModel.North, this.viewModel.West })
            {
                seat.PropertyChanged += this.OnSeatPropertyChanged;
            }

            this.BindingContext = this.viewModel;
            this.viewModel.StartGame();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            SetKeepScreenOn(false);
            if (this.viewModel != null)
            {
                this.viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
                foreach (var seat in new[] { this.viewModel.South, this.viewModel.East, this.viewModel.North, this.viewModel.West })
                {
                    seat.PropertyChanged -= this.OnSeatPropertyChanged;
                }

                this.viewModel.Dispose();
                this.viewModel = null;
            }

            this.session = null;
            this.started = false;
        }

        protected override bool OnBackButtonPressed()
        {
            _ = this.leaving.RunAsync(this.ConfirmAndLeaveAsync);
            return true; // Leaving goes through the confirmation below.
        }

        private static void AnimateCardPop(VisualElement element)
        {
            element.CancelAnimations();
            element.Scale = 0.6;
            element.Opacity = 0;
            _ = element.ScaleToAsync(1.0, 180, Easing.CubicOut);
            _ = element.FadeToAsync(1.0, 140, Easing.CubicOut);
        }

        private static void AnimatePop(VisualElement element)
        {
            element.CancelAnimations();
            element.Scale = 0.85;
            element.Opacity = 0;
            _ = element.ScaleToAsync(1.0, 200, Easing.SpringOut);
            _ = element.FadeToAsync(1.0, 150, Easing.CubicOut);
        }

        private static void AnimateFadeIn(VisualElement element)
        {
            element.CancelAnimations();
            element.Opacity = 0;
            _ = element.FadeToAsync(1.0, 220, Easing.CubicOut);
        }

        // Card games get abandoned mid-hand when the screen sleeps; keep it awake during play.
        private static void SetKeepScreenOn(bool value)
        {
            try
            {
                DeviceDisplay.Current.KeepScreenOn = value;
            }
            catch
            {
                // Not supported on this platform: fine to ignore.
            }
        }

        private async void OnMenuClicked(object? sender, EventArgs e)
        {
            await this.leaving.RunAsync(this.ConfirmAndLeaveAsync);
        }

        // Confirms before abandoning a game in progress; a finished (or never started) game just
        // leaves. The game-over "Back to menu" button calls LeaveCommand directly.
        private async Task ConfirmAndLeaveAsync()
        {
            if (this.viewModel == null)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            if (this.session is { IsRunning: true })
            {
                var text = LocalizationManager.Instance;
                var leave = await this.DisplayAlertAsync(text["Leave_Title"], text["Leave_Message"], text["Leave_Confirm"], text["Leave_Cancel"]);
                if (!leave)
                {
                    return;
                }
            }

            // The page may have gone away while the question was up.
            this.viewModel?.LeaveCommand.Execute(null);
        }

        // View-only animations: the game raises everything on the UI thread, so animating here is safe.
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var vm = this.viewModel;
            if (vm == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(GameViewModel.IsToastVisible) when vm.IsToastVisible:
                    AnimatePop(this.ToastBorder);
                    break;
                case nameof(GameViewModel.IsBidPanelVisible) when vm.IsBidPanelVisible:
                    AnimateFadeIn(this.BidPanel);
                    break;
                case nameof(GameViewModel.IsAnnouncePanelVisible) when vm.IsAnnouncePanelVisible:
                    AnimateFadeIn(this.AnnouncePanel);
                    break;
                case nameof(GameViewModel.IsRoundOverlayVisible) when vm.IsRoundOverlayVisible:
                    AnimateFadeIn(this.RoundOverlayRoot);
                    break;
                case nameof(GameViewModel.IsGameOverlayVisible) when vm.IsGameOverlayVisible:
                    AnimateFadeIn(this.GameOverlayRoot);
                    break;
            }
        }

        private void OnSeatPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SeatViewModel.PlayedCard) || sender is not SeatViewModel { HasPlayedCard: true } seat)
            {
                return;
            }

            VisualElement border = seat.Seat switch
            {
                Engine.Players.PlayerPosition.North => this.NorthPlayed,
                Engine.Players.PlayerPosition.East => this.EastPlayed,
                Engine.Players.PlayerPosition.West => this.WestPlayed,
                _ => this.SouthPlayed,
            };
            AnimateCardPop(border);
        }
    }
}
