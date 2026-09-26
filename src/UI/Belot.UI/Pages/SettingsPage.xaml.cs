namespace Belot.UI.Pages
{
    using System;
    using System.Linq;

    using Belot.UI.Game;
    using Belot.UI.Localization;

    public partial class SettingsPage : ContentPage
    {
        private const string RepoUrl = "https://github.com/NikolayIT/BelotGameEngine";

        // A double tap on Back goes back once.
        private readonly OneAtATime navigation = new();

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            this.HapticsSwitch.IsToggled = AppSettings.HapticsEnabled;
            this.AssistsSwitch.IsToggled = AppSettings.AssistsEnabled;
            this.VersionLabel.Text = LocalizationManager.Instance.Format("Settings_Version", AppInfo.Current.VersionString);

            this.RefreshLanguageButtons();
            this.RefreshSpeedButtons();
        }

        private static void StyleSegment(Button button, bool selected)
        {
            button.BackgroundColor = selected ? Color.FromArgb("#E2B864") : Color.FromArgb("#26FFFFFF");
            button.TextColor = selected ? Color.FromArgb("#1A1006") : Colors.White;
            button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        }

        private async void OnBack(object? sender, EventArgs e)
        {
            await this.navigation.RunAsync(() => Shell.Current.GoToAsync(".."));
        }

        private void OnLanguageEn(object? sender, EventArgs e) => this.SetLanguage(LocalizationManager.English);

        private void OnLanguageBg(object? sender, EventArgs e) => this.SetLanguage(LocalizationManager.Bulgarian);

        private void SetLanguage(string language)
        {
            LocalizationManager.Instance.SetLanguage(language);
            this.RefreshLanguageButtons();
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
            var confirmed = await this.DisplayAlertAsync(text["Reset_Title"], text["Reset_Message"], text["Reset_Confirm"], text["Common_Cancel"]);
            if (!confirmed)
            {
                return;
            }

            PlayerRatingStore.Reset();
            MatchHistoryStore.Clear();
            OpponentStatsStore.Clear(AiLevels.All.Select(o => o.Id));

            await this.DisplayAlertAsync(text["Settings_ResetStats"], text["Reset_Done"], "OK");
        }

        private async void OnOpenGitHub(object? sender, EventArgs e)
        {
            try
            {
                await Browser.Default.OpenAsync(RepoUrl, BrowserLaunchMode.SystemPreferred);
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
            StyleSegment(this.LangEnButton, !isBg);
            StyleSegment(this.LangBgButton, isBg);
        }

        private void RefreshSpeedButtons()
        {
            var speed = AppSettings.Speed;
            StyleSegment(this.SpeedRelaxedButton, speed == GameSpeed.Relaxed);
            StyleSegment(this.SpeedNormalButton, speed == GameSpeed.Normal);
            StyleSegment(this.SpeedFastButton, speed == GameSpeed.Fast);
        }
    }
}
