namespace Belot.UI.Pages
{
    using System;
    using System.Globalization;
    using System.Linq;

    using Belot.UI.Game;
    using Belot.UI.Localization;
    using Belot.UI.Scaling;

    using Microsoft.Maui.Controls.Shapes;

    public partial class StatisticsPage : ContentPage
    {
        // A double tap on Back goes back once.
        private readonly OneAtATime navigation = new();

        public StatisticsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.Populate();
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
                this.Populate();
            }
        }

        private static string BuildStreakText(LocalizationManager mgr)
        {
            var streak = PlayerRatingStore.CurrentStreak;
            var best = PlayerRatingStore.BestWinStreak;

            var current = streak switch
            {
                > 0 => $"{streak}{mgr["History_Win"]}",
                < 0 => $"{-streak}{mgr["History_Loss"]}",
                _ => "—",
            };

            return $"{mgr["Stats_CurrentStreak"]}: {current} · {mgr["Stats_BestStreak"]}: {best}";
        }

        private static View BuildLevelRow(LocalizationManager mgr, AiLevel level)
        {
            var (games, wins) = OpponentStatsStore.For(level.Id);

            var avatar = new Label
            {
                Text = level.Avatar,
                FontSize = Ui.Size(20),
                VerticalOptions = LayoutOptions.Center,
            };

            var name = new Label
            {
                Text = level.DisplayName,
                TextColor = Colors.White,
                FontSize = Ui.Size(15),
                FontAttributes = FontAttributes.Bold,
            };

            var sub = new Label
            {
                Text = games > 0 ? mgr.Format("Stats_GamesFormat", games) : mgr["Stats_NotPlayed"],
                TextColor = Color.FromArgb("#B9C7B0"),
                FontSize = Ui.Size(11),
            };

            var middle = new VerticalStackLayout
            {
                Spacing = Ui.Size(2),
                VerticalOptions = LayoutOptions.Center,
                Children = { name, sub },
            };

            var right = new VerticalStackLayout
            {
                Spacing = Ui.Size(2),
                VerticalOptions = LayoutOptions.Center,
            };

            if (games > 0)
            {
                right.Children.Add(new Label
                {
                    Text = $"{wins}{mgr["History_Win"]} – {games - wins}{mgr["History_Loss"]}",
                    TextColor = Color.FromArgb("#F4D586"),
                    FontSize = Ui.Size(15),
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.End,
                });
                right.Children.Add(new Label
                {
                    Text = StatsText.WinRate(wins, games),
                    TextColor = Color.FromArgb("#B9C7B0"),
                    FontSize = Ui.Size(11),
                    HorizontalOptions = LayoutOptions.End,
                });
            }

            var grid = new Grid
            {
                ColumnSpacing = Ui.Size(12),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
            };
            grid.Add(avatar, 0, 0);
            grid.Add(middle, 1, 0);
            grid.Add(right, 2, 0);

            return new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
                BackgroundColor = Color.FromArgb("#26000000"),
                Padding = Ui.Pad(14, 10),
                Content = grid,
            };
        }

        private static View BuildHistoryRow(LocalizationManager mgr, MatchHistoryEntry entry)
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
                    Text = entry.Won ? mgr["History_Win"] : mgr["History_Loss"],
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

            var versus = new Label
            {
                Text = mgr.Format("History_With", entry.PartnerName, entry.RivalsName),
                TextColor = Color.FromArgb("#C7D2BD"),
                FontSize = Ui.Size(13),
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
            };

            var when = new Label
            {
                Text = entry.WhenUtc.ToLocalTime().ToString("d MMM", CultureInfo.CurrentCulture),
                TextColor = Color.FromArgb("#8FA695"),
                FontSize = Ui.Size(11),
                VerticalOptions = LayoutOptions.Center,
            };

            var grid = new Grid
            {
                ColumnSpacing = Ui.Size(12),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
            };
            grid.Add(chip, 0, 0);
            grid.Add(score, 1, 0);
            grid.Add(versus, 2, 0);
            grid.Add(when, 3, 0);

            return new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                BackgroundColor = Color.FromArgb("#1E000000"),
                Padding = Ui.Pad(12, 8),
                Content = grid,
            };
        }

        private async void OnBack(object? sender, EventArgs e)
        {
            await this.navigation.RunAsync(() => Shell.Current.GoToAsync(".."));
        }

        private void Populate()
        {
            var mgr = LocalizationManager.Instance;

            var games = PlayerRatingStore.GamesPlayed;
            var wins = PlayerRatingStore.Wins;
            var losses = PlayerRatingStore.Losses;

            this.CurrentEloLabel.Text = PlayerRatingStore.CurrentElo.ToString(CultureInfo.InvariantCulture);
            this.PeakEloLabel.Text = PlayerRatingStore.PeakElo.ToString(CultureInfo.InvariantCulture);
            this.GamesLabel.Text = games.ToString(CultureInfo.InvariantCulture);
            this.WinLossLabel.Text = $"{wins}{mgr["History_Win"]} – {losses}{mgr["History_Loss"]}";
            this.WinRateLabel.Text = StatsText.WinRate(wins, games);

            this.StreakLabel.Text = BuildStreakText(mgr);

            var history = MatchHistoryStore.All();
            var hasAnything = games > 0 || history.Count > 0;

            this.EmptyLabel.IsVisible = !hasAnything;
            this.ByLevelCaption.IsVisible = hasAnything;
            this.LevelListHost.IsVisible = hasAnything;
            this.HistoryCaption.IsVisible = history.Count > 0;
            this.HistoryListHost.IsVisible = history.Count > 0;

            this.LevelListHost.Clear();
            if (hasAnything)
            {
                foreach (var level in AiLevels.All)
                {
                    this.LevelListHost.Add(BuildLevelRow(mgr, level));
                }
            }

            this.HistoryListHost.Clear();
            foreach (var entry in history)
            {
                this.HistoryListHost.Add(BuildHistoryRow(mgr, entry));
            }
        }
    }
}
