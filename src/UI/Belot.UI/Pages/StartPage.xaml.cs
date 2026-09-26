namespace Belot.UI.Pages
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Belot.UI.Game;
    using Belot.UI.Localization;
    using Belot.UI.Scaling;

    using Microsoft.Maui.Controls.Shapes;

    public partial class StartPage : ContentPage
    {
        // A double tap opens one page: two quick taps on Play would open two games.
        private readonly OneAtATime navigation = new();

        // The level last tapped: its tagline and record show under the pickers.
        private AiLevel? described;

        public StartPage()
        {
            this.InitializeComponent();
            this.NameEntry.Text = AppSettings.PlayerName;

            // The installed version, not a copy in the page.
            this.FooterLabel.Text = $"v{AppInfo.Current.VersionString} · github.com/NikolayIT/BelotGameEngine";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.ApplyTexts();
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
                LineBreakMode = LineBreakMode.TailTruncation,
            };

            var grid = new Grid
            {
                ColumnSpacing = Ui.Size(12),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                },
            };
            grid.Add(chip, 0, 0);
            grid.Add(score, 1, 0);
            grid.Add(with, 2, 0);

            return new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                BackgroundColor = Color.FromArgb("#1E000000"),
                Padding = Ui.Pad(12, 8),
                Content = grid,
            };
        }

        // One tile per level; the chosen one is outlined in gold.
        private static void BuildLevelTiles(Grid host, string selectedId, Action<AiLevel> select)
        {
            host.Clear();
            for (var i = 0; i < AiLevels.All.Count; i++)
            {
                var level = AiLevels.All[i];
                var selected = level.Id == selectedId;
                var tile = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
                    StrokeThickness = selected ? 2 : 0,
                    Stroke = Color.FromArgb("#F4D586"),
                    BackgroundColor = selected ? Color.FromArgb("#40000000") : Color.FromArgb("#1E000000"),
                    Padding = Ui.Pad(4, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = Ui.Size(2),
                        Children =
                        {
                            new Label { Text = level.Avatar, FontSize = Ui.Size(22), HorizontalOptions = LayoutOptions.Center },
                            new Label
                            {
                                Text = level.DisplayName,
                                FontSize = Ui.Size(12),
                                FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None,
                                TextColor = Colors.White,
                                HorizontalTextAlignment = TextAlignment.Center,
                                MaxLines = 1,
                                LineBreakMode = LineBreakMode.TailTruncation,
                            },
                            new Label { Text = level.DifficultyStars, FontSize = Ui.Size(9), TextColor = Color.FromArgb("#F4D586"), HorizontalOptions = LayoutOptions.Center },
                            new Label { Text = level.Elo.ToString(CultureInfo.InvariantCulture), FontSize = Ui.Size(10), TextColor = Color.FromArgb("#C7D2BD"), HorizontalOptions = LayoutOptions.Center },
                        },
                    },
                };
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => select(level);
                tile.GestureRecognizers.Add(tap);
                host.Add(tile, i, 0);
            }
        }

        private void OnToggleLanguage(object? sender, EventArgs e)
        {
            LocalizationManager.Instance.Toggle();
            this.ApplyTexts();
        }

        private async void OnOpenSettings(object? sender, EventArgs e)
        {
            await this.navigation.RunAsync(() => Shell.Current.GoToAsync("SettingsPage"));
        }

        private async void OnOpenStatistics(object? sender, EventArgs e)
        {
            await this.navigation.RunAsync(() => Shell.Current.GoToAsync("StatisticsPage"));
        }

        private async void OnOpenRules(object? sender, EventArgs e)
        {
            await this.navigation.RunAsync(() => Shell.Current.GoToAsync("RulesPage"));
        }

        private void OnPlayerNameChanged(object? sender, EventArgs e)
        {
            AppSettings.PlayerName = this.NameEntry.Text?.Trim() ?? string.Empty;
        }

        private async void OnPlay(object? sender, EventArgs e)
        {
            await this.navigation.RunAsync(this.PlayAsync);
        }

        private Task PlayAsync()
        {
            var name = this.NameEntry.Text?.Trim() ?? string.Empty;
            AppSettings.PlayerName = name;
            var query = $"?name={Uri.EscapeDataString(name)}&partner={Uri.EscapeDataString(AppSettings.PartnerLevel)}"
                + $"&west={Uri.EscapeDataString(AppSettings.WestLevel)}&east={Uri.EscapeDataString(AppSettings.EastLevel)}";
            return Shell.Current.GoToAsync($"GamePage{query}");
        }

        // Refreshes everything composed in code (so it follows a language switch). Static text
        // uses {loc:Tr} bindings and refreshes itself.
        private void ApplyTexts()
        {
            var text = LocalizationManager.Instance;
            this.LangButton.Text = text.IsBulgarian ? "English" : "Български";
            this.SeeAllLabel.Text = $"{text["Start_SeeAll"]} ›";
            this.RatingCaptionLabel.Text = text["Start_YourRating"].ToUpperInvariant();
            this.RatingLabel.Text = PlayerRatingStore.CurrentElo.ToString(CultureInfo.InvariantCulture);
            var games = PlayerRatingStore.GamesPlayed;
            this.RecordLabel.Text = games > 0
                ? text.Format("Start_RecordFormat", games, PlayerRatingStore.Wins, PlayerRatingStore.Losses)
                : text["Start_NoGames"];

            this.ShowLevels();
            this.HistoryList.Clear();
            var entries = MatchHistoryStore.All().Take(3).ToList();
            this.NoHistoryLabel.IsVisible = entries.Count == 0;
            this.SeeAllLabel.IsVisible = entries.Count > 0;
            foreach (var entry in entries)
            {
                this.HistoryList.Add(BuildHistoryRow(text, entry));
            }
        }

        private void ShowLevels()
        {
            BuildLevelTiles(this.PartnerHost, AiLevels.ById(AppSettings.PartnerLevel).Id, level => this.Pick(level, () => AppSettings.PartnerLevel = level.Id));
            BuildLevelTiles(this.WestHost, AiLevels.ById(AppSettings.WestLevel).Id, level => this.Pick(level, () => AppSettings.WestLevel = level.Id));
            BuildLevelTiles(this.EastHost, AiLevels.ById(AppSettings.EastLevel).Id, level => this.Pick(level, () => AppSettings.EastLevel = level.Id));

            var level = this.described ?? AiLevels.ById(AppSettings.EastLevel);
            this.TaglineLabel.Text = $"{level.Avatar} {level.DisplayName}: {level.Tagline}";
            this.LevelRecordLabel.Text = level.RecordText;
            this.LevelRecordLabel.IsVisible = level.HasRecord;
        }

        private void Pick(AiLevel level, Action save)
        {
            save();
            this.described = level;
            this.ShowLevels();
        }
    }
}
