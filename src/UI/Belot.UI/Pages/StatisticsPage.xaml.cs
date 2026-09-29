namespace Belot.UI.Pages
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;

    using Belot.UI.Game;
    using Belot.UI.Localization;
    using Belot.UI.Scaling;

    using Microsoft.Maui.Controls.Shapes;

    public partial class StatisticsPage : ContentPage
    {
        // A double tap on Back goes back once.
        private readonly PageActions actions = new();

        private int metricColumns;

        public StatisticsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.actions.Activate();
            UiScale.Current.PropertyChanged -= this.OnScaleChanged;
            UiScale.Current.PropertyChanged += this.OnScaleChanged;
            this.Populate();
            this.UpdateMetricLayout();
        }

        protected override void OnDisappearing()
        {
            this.actions.Deactivate();
            UiScale.Current.PropertyChanged -= this.OnScaleChanged;
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
                this.Populate();
            }

            this.UpdateMetricLayout();
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
                LineBreakMode = LineBreakMode.WordWrap,
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
                RowSpacing = Ui.Size(4),
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
            };
            grid.Add(chip, 0, 0);
            grid.Add(score, 1, 0);
            grid.Add(when, 2, 0);
            grid.Add(versus, 0, 1);
            Grid.SetColumnSpan(versus, 3);

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
            await this.actions.RunAsync(() => Shell.Current.GoToAsync(".."));
        }

        private void OnScaleChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(UiScale.SystemFontScale) or nameof(UiScale.Page))
            {
                this.UpdateMetricLayout();
            }
        }

        private void UpdateMetricLayout()
        {
            var scale = UiScale.Current;
            var width = PageColumn.Width(this.Width) - this.ContentColumn.Padding.HorizontalThickness;
            var columns = UiScale.StatisticColumnsFor(width / scale.Page, scale.SystemFontScale);
            if (columns == this.metricColumns)
            {
                return;
            }

            this.metricColumns = columns;
            Grid.SetRow(this.StatisticsHeading, columns == 1 ? 1 : 0);
            Grid.SetColumn(this.StatisticsHeading, columns == 1 ? 0 : 1);
            Grid.SetColumnSpan(this.StatisticsHeading, columns == 1 ? 2 : 1);
            this.MetricsGrid.ColumnDefinitions.Clear();
            for (var column = 0; column < columns; column++)
            {
                this.MetricsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }

            var metrics = new[] { this.CurrentMetric, this.PeakMetric, this.GamesMetric, this.WinRateMetric };
            this.MetricsGrid.RowDefinitions.Clear();
            for (var row = 0; row < metrics.Length / columns; row++)
            {
                this.MetricsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            for (var index = 0; index < metrics.Length; index++)
            {
                Grid.SetRow(metrics[index], index / columns);
                Grid.SetColumn(metrics[index], index % columns);
            }
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
