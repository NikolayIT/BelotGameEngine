namespace Belot.UI.Pages
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Belot.Engine.Players;
    using Belot.UI.Game;
    using Belot.UI.Localization;
    using Belot.UI.Scaling;

    using Microsoft.Maui.Controls.Shapes;

    public partial class StartPage : ContentPage
    {
        // A double tap opens one page: two quick taps on Play would open two games.
        private readonly PageActions actions = new();

        // The level last chosen: its tagline and record show under the pickers.
        private AiLevel? described;

        private bool refreshingLevels;

        private bool? headerStacked;

        private int lineupColumns;

        public StartPage()
        {
            this.InitializeComponent();
            this.NameEntry.Text = AppSettings.PlayerName;
            this.PartnerNameEntry.Text = AppSettings.PartnerName;
            this.WestNameEntry.Text = AppSettings.WestName;
            this.EastNameEntry.Text = AppSettings.EastName;

            // The installed version, not a copy in the page.
            this.FooterLabel.Text = $"v{AppInfo.Current.VersionString} · github.com/NikolayIT/BelotGameEngine";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.actions.Activate();
            UiScale.Current.PropertyChanged -= this.OnScaleChanged;
            UiScale.Current.PropertyChanged += this.OnScaleChanged;
            this.ApplyTexts();
            this.UpdateResponsiveLayout();
        }

        protected override void OnDisappearing()
        {
            this.actions.Deactivate();
            UiScale.Current.PropertyChanged -= this.OnScaleChanged;
            this.SaveNames();
            base.OnDisappearing();
        }

        // Sizes follow the window (see UiScale); the lists built in code are rebuilt at a new scale.
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            var scale = UiScale.Current.Page;
            UiScale.Current.Update(width, height);
            this.ContentColumn.WidthRequest = PageColumn.Width(width);
            if (UiScale.Current.Page != scale)
            {
                this.ApplyTexts();
            }

            this.UpdateResponsiveLayout();
        }

        private static View BuildHistoryRow(LocalizationManager text, MatchHistoryEntry entry)
        {
            var chip = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(6) },
                BackgroundColor = entry.Won ? Color.FromArgb("#1FA15A") : Color.FromArgb("#C0504D"),
                Padding = Ui.Pad(9, 2),
                VerticalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = entry.Won ? text["History_Win"] : text["History_Loss"],
                    TextColor = Colors.White,
                    FontSize = Ui.Size(12),
                    FontAttributes = FontAttributes.Bold,
                },
            };

            var score = new Label
            {
                Text = entry.ScoreText,
                TextColor = Colors.White,
                FontSize = Ui.Size(14),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
            };

            var with = new Label
            {
                Text = text.Format("History_With", entry.PartnerName, entry.RivalsName),
                TextColor = Color.FromArgb("#C7D2BD"),
                FontSize = Ui.Size(12),
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.WordWrap,
            };

            var grid = new Grid
            {
                ColumnSpacing = Ui.Size(12),
                RowSpacing = Ui.Size(4),
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                },
            };
            grid.Add(chip, 0, 0);
            grid.Add(score, 1, 0);
            grid.Add(with, 0, 1);
            Grid.SetColumnSpan(with, 2);

            return new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                BackgroundColor = Color.FromArgb("#1E000000"),
                Padding = Ui.Pad(12, 8),
                Content = grid,
            };
        }

        private static void FillPicker(Picker picker, PlayerPosition seat)
        {
            picker.Items.Clear();
            foreach (var level in AiLevels.All)
            {
                picker.Items.Add($"{level.DisplayName} · {level.Elo}");
            }

            picker.SelectedIndex = LineupSelection.SelectedIndex(seat);
            DescribePicker(picker, AiLevels.All[picker.SelectedIndex]);
        }

        private static void DescribePicker(Picker picker, AiLevel level)
        {
            SemanticProperties.SetDescription(picker, picker.Title);
            SemanticProperties.SetHint(picker, level.Tagline);
        }

        private void OnScaleChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(UiScale.SystemFontScale) or nameof(UiScale.Page))
            {
                this.UpdateResponsiveLayout();
            }
        }

        private void UpdateResponsiveLayout()
        {
            var scale = UiScale.Current;
            var width = PageColumn.Width(this.Width) - this.ContentColumn.Padding.HorizontalThickness;
            var stacked = UiScale.StartColumnsFor(width / scale.Page, scale.SystemFontScale) == 1;
            if (stacked != this.headerStacked)
            {
                this.headerStacked = stacked;
                Grid.SetColumnSpan(this.TitleLabel, stacked ? 3 : 1);
                Grid.SetRow(this.LangButton, stacked ? 1 : 0);
                Grid.SetRow(this.SettingsButton, stacked ? 1 : 0);
            }

            var lineupWidth = (width - this.LineupBorder.Padding.HorizontalThickness) / scale.Page;
            var columns = UiScale.StartColumnsFor(lineupWidth, scale.SystemFontScale);
            if (columns == this.lineupColumns)
            {
                return;
            }

            this.lineupColumns = columns;
            this.LineupGrid.ColumnDefinitions.Clear();
            this.LineupGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            if (columns == 2)
            {
                this.LineupGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.2, GridUnitType.Star)));
            }

            var names = new[] { this.PartnerNameGroup, this.WestNameGroup, this.EastNameGroup };
            var pickers = new[] { this.PartnerPicker, this.WestPicker, this.EastPicker };
            this.LineupGrid.RowDefinitions.Clear();
            for (var row = 0; row < 6 / columns; row++)
            {
                this.LineupGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            for (var index = 0; index < names.Length; index++)
            {
                Grid.SetRow(names[index], columns == 1 ? index * 2 : index);
                Grid.SetColumn(pickers[index], columns == 1 ? 0 : 1);
                Grid.SetRow(pickers[index], columns == 1 ? (index * 2) + 1 : index);
            }
        }

        private void OnToggleLanguage(object? sender, EventArgs e)
        {
            LocalizationManager.Instance.Toggle();
            this.ApplyTexts();
        }

        private async void OnOpenSettings(object? sender, EventArgs e)
        {
            await this.actions.RunAsync(() => Shell.Current.GoToAsync("SettingsPage"));
        }

        private async void OnOpenStatistics(object? sender, EventArgs e)
        {
            await this.actions.RunAsync(() => Shell.Current.GoToAsync("StatisticsPage"));
        }

        private async void OnOpenRules(object? sender, EventArgs e)
        {
            await this.actions.RunAsync(() => Shell.Current.GoToAsync("RulesPage"));
        }

        private void OnPlayerNameChanged(object? sender, EventArgs e)
        {
            AppSettings.PlayerName = this.NameEntry.Text?.Trim() ?? string.Empty;
        }

        private void OnBotNameChanged(object? sender, EventArgs e) => this.SaveNames();

        private async void OnPlay(object? sender, EventArgs e)
        {
            await this.actions.RunAsync(this.PlayAsync);
        }

        private async void OnPlayOnline(object? sender, EventArgs e)
        {
            this.SaveNames();
            await OnlinePlay.OpenAsync(
                this.actions,
                url => Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred),
                () =>
                {
                    var text = LocalizationManager.Instance;
                    return this.DisplayAlertAsync(text["Start_PlayOnline"], text["Start_OnlineUnavailable"], "OK");
                });
        }

        private Task PlayAsync()
        {
            this.SaveNames();
            var name = this.NameEntry.Text?.Trim() ?? string.Empty;
            var query = $"?name={Uri.EscapeDataString(name)}&partner={Uri.EscapeDataString(AppSettings.PartnerLevel)}"
                + $"&west={Uri.EscapeDataString(AppSettings.WestLevel)}&east={Uri.EscapeDataString(AppSettings.EastLevel)}";
            return Shell.Current.GoToAsync($"GamePage{query}");
        }

        private void SaveNames()
        {
            AppSettings.PlayerName = this.NameEntry.Text?.Trim() ?? string.Empty;
            AppSettings.PartnerName = this.PartnerNameEntry.Text ?? string.Empty;
            AppSettings.WestName = this.WestNameEntry.Text ?? string.Empty;
            AppSettings.EastName = this.EastNameEntry.Text ?? string.Empty;
        }

        // Refreshes everything composed in code (so it follows a language switch). Static text
        // uses {loc:Tr} bindings and refreshes itself.
        private void ApplyTexts()
        {
            var text = LocalizationManager.Instance;
            this.LangButton.Text = text.IsBulgarian ? "English" : "Български";
            this.SeeAllButton.Text = $"{text["Start_SeeAll"]} ›";
            this.RatingLabel.Text = $"{PlayerRatingStore.CurrentElo.ToString(CultureInfo.InvariantCulture)} ELO";
            var games = PlayerRatingStore.GamesPlayed;
            this.RecordLabel.Text = games > 0
                ? text.Format("Start_RecordFormat", games, PlayerRatingStore.Wins, PlayerRatingStore.Losses)
                : text["Start_NoGames"];
            SemanticProperties.SetDescription(this.RatingButton, text.Format("Start_RatingAccessibility", this.RatingLabel.Text, this.RecordLabel.Text));

            this.RefreshLevels();
            this.HistoryList.Clear();
            var entries = MatchHistoryStore.All().Take(3).ToList();
            this.NoHistoryLabel.IsVisible = entries.Count == 0;
            this.SeeAllButton.IsVisible = entries.Count > 0;
            foreach (var entry in entries)
            {
                this.HistoryList.Add(BuildHistoryRow(text, entry));
            }
        }

        private void RefreshLevels()
        {
            this.refreshingLevels = true;
            try
            {
                FillPicker(this.PartnerPicker, PlayerPosition.North);
                FillPicker(this.WestPicker, PlayerPosition.West);
                FillPicker(this.EastPicker, PlayerPosition.East);
            }
            finally
            {
                this.refreshingLevels = false;
            }

            this.ShowLevelDescription();
        }

        private void OnPartnerChanged(object? sender, EventArgs e) => this.Pick(PlayerPosition.North, this.PartnerPicker);

        private void OnWestChanged(object? sender, EventArgs e) => this.Pick(PlayerPosition.West, this.WestPicker);

        private void OnEastChanged(object? sender, EventArgs e) => this.Pick(PlayerPosition.East, this.EastPicker);

        private void Pick(PlayerPosition seat, Picker picker)
        {
            if (this.refreshingLevels)
            {
                return;
            }

            var level = LineupSelection.Select(seat, picker.SelectedIndex);
            if (level == null)
            {
                return;
            }

            this.described = level;
            DescribePicker(picker, level);
            this.ShowLevelDescription();
        }

        private void ShowLevelDescription()
        {
            var level = this.described ?? AiLevels.ById(AppSettings.EastLevel);
            this.TaglineLabel.Text = $"{level.DisplayName}: {level.Tagline}";
            this.LevelRecordLabel.Text = level.RecordText;
            this.LevelRecordLabel.IsVisible = level.HasRecord;
        }
    }
}
