namespace Belot.UI.Pages
{
    using System;
    using System.ComponentModel;
    using System.Linq;

    using Belot.UI.Game;
    using Belot.UI.Localization;
    using Belot.UI.Scaling;

    public partial class SettingsPage : ContentPage
    {
        private const string RepoUrl = "https://github.com/NikolayIT/BelotGameEngine";

        private readonly PageActions actions = new();

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.actions.Activate();
            UiScale.Current.PropertyChanged -= this.OnScaleChanged;
            UiScale.Current.PropertyChanged += this.OnScaleChanged;

            this.HapticsSwitch.IsToggled = AppSettings.HapticsEnabled;
            this.AssistsSwitch.IsToggled = AppSettings.AssistsEnabled;
            this.VersionLabel.Text = LocalizationManager.Instance.Format("Settings_Version", AppInfo.Current.VersionString);

            this.RefreshLanguageButtons();
            this.RefreshSpeedButtons();
            this.UpdateChoiceLayout();
        }

        protected override void OnDisappearing()
        {
            this.actions.Deactivate();
            UiScale.Current.PropertyChanged -= this.OnScaleChanged;
            base.OnDisappearing();
        }

        // Sizes follow the window (see UiScale).
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            UiScale.Current.Update(width, height);
            this.ContentColumn.WidthRequest = PageColumn.Width(width);
            this.UpdateChoiceLayout();
        }

        private static void StyleSegment(Button button, bool selected, string label)
        {
            button.BackgroundColor = selected ? Color.FromArgb("#E2B864") : Color.FromArgb("#26FFFFFF");
            button.TextColor = selected ? Color.FromArgb("#1A1006") : Colors.White;
            button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
            var state = LocalizationManager.Instance[selected ? "Common_Selected" : "Common_NotSelected"];
            SemanticProperties.SetDescription(button, $"{label}, {state}");
        }

        private static void ArrangeChoices(Grid grid, Button[] buttons, int columns)
        {
            if (grid.ColumnDefinitions.Count == columns)
            {
                return;
            }

            grid.ColumnDefinitions.Clear();
            for (var column = 0; column < columns; column++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }

            grid.RowDefinitions.Clear();
            for (var row = 0; row < buttons.Length / columns; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            for (var index = 0; index < buttons.Length; index++)
            {
                Grid.SetRow(buttons[index], index / columns);
                Grid.SetColumn(buttons[index], index % columns);
            }
        }

        private void OnScaleChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(UiScale.SystemFontScale) or nameof(UiScale.Page))
            {
                this.UpdateChoiceLayout();
            }
        }

        private void UpdateChoiceLayout()
        {
            var scale = UiScale.Current;
            var width = PageColumn.Width(this.Width) - this.ContentColumn.Padding.HorizontalThickness;
            var languageWidth = (width - this.LanguageChoicesBorder.Padding.HorizontalThickness) / scale.Page;
            var speedWidth = (width - this.SpeedChoicesBorder.Padding.HorizontalThickness) / scale.Page;
            ArrangeChoices(
                this.LanguageChoicesGrid,
                new[] { this.LangEnButton, this.LangBgButton },
                UiScale.SettingsChoiceColumnsFor(languageWidth, scale.SystemFontScale, 2));
            ArrangeChoices(
                this.SpeedChoicesGrid,
                new[] { this.SpeedRelaxedButton, this.SpeedNormalButton, this.SpeedFastButton },
                UiScale.SettingsChoiceColumnsFor(speedWidth, scale.SystemFontScale, 3));
        }

        private async void OnBack(object? sender, EventArgs e)
        {
            await this.actions.RunAsync(() => Shell.Current.GoToAsync(".."));
        }

        private void OnLanguageEn(object? sender, EventArgs e) => this.SetLanguage(LocalizationManager.English);

        private void OnLanguageBg(object? sender, EventArgs e) => this.SetLanguage(LocalizationManager.Bulgarian);

        private void SetLanguage(string language)
        {
            LocalizationManager.Instance.SetLanguage(language);
            this.RefreshLanguageButtons();
            this.RefreshSpeedButtons();
            this.VersionLabel.Text = LocalizationManager.Instance.Format("Settings_Version", AppInfo.Current.VersionString);
        }

        private void OnSpeedRelaxed(object? sender, EventArgs e) => this.SetSpeed(GameSpeed.Relaxed);

        private void OnSpeedNormal(object? sender, EventArgs e) => this.SetSpeed(GameSpeed.Normal);

        private void OnSpeedFast(object? sender, EventArgs e) => this.SetSpeed(GameSpeed.Fast);

        private void OnHapticsToggled(object? sender, ToggledEventArgs e) => AppSettings.HapticsEnabled = e.Value;

        private void OnAssistsToggled(object? sender, ToggledEventArgs e) => AppSettings.AssistsEnabled = e.Value;

        private async void OnResetStats(object? sender, EventArgs e)
        {
            var text = LocalizationManager.Instance;
            await this.actions.ConfirmAsync(
                () => this.DisplayAlertAsync(text["Reset_Title"], text["Reset_Message"], text["Reset_Confirm"], text["Common_Cancel"]),
                async () =>
                {
                    PlayerRatingStore.Reset();
                    MatchHistoryStore.Clear();
                    OpponentStatsStore.Clear(AiLevels.All.Select(o => o.Id));

                    await this.DisplayAlertAsync(text["Settings_ResetStats"], text["Reset_Done"], "OK");
                });
        }

        private async void OnOpenGitHub(object? sender, EventArgs e)
        {
            try
            {
                await this.actions.RunAsync(() => Browser.Default.OpenAsync(RepoUrl, BrowserLaunchMode.SystemPreferred));
            }
            catch
            {
                // No browser available: nothing sensible to do.
            }
        }

        private void SetSpeed(GameSpeed speed)
        {
            AppSettings.Speed = speed;
            this.RefreshSpeedButtons();
        }

        private void RefreshLanguageButtons()
        {
            var isBg = LocalizationManager.Instance.IsBulgarian;
            StyleSegment(this.LangEnButton, !isBg, "English");
            StyleSegment(this.LangBgButton, isBg, "Български");
        }

        private void RefreshSpeedButtons()
        {
            var speed = AppSettings.Speed;
            var text = LocalizationManager.Instance;
            StyleSegment(this.SpeedRelaxedButton, speed == GameSpeed.Relaxed, text["Speed_Relaxed"]);
            StyleSegment(this.SpeedNormalButton, speed == GameSpeed.Normal, text["Speed_Normal"]);
            StyleSegment(this.SpeedFastButton, speed == GameSpeed.Fast, text["Speed_Fast"]);
        }
    }
}
