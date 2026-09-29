namespace Belot.UI.Tests
{
    using System;
    using System.Threading.Tasks;

    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    public class OnlinePlayTests
    {
        [Fact]
        public async Task SuccessfulLaunchShouldOpenTheBelotChooserWithoutAnError()
        {
            var page = new PageActions();
            page.Activate();
            string? opened = null;

            await OnlinePlay.OpenAsync(
                page,
                url =>
                {
                    opened = url;
                    return Task.FromResult(true);
                },
                () => throw new InvalidOperationException("Successful browser launch showed an error."));

            var destination = new Uri(Assert.IsType<string>(opened));
            Assert.Equal("https", destination.Scheme);
            Assert.Equal("ednaigra.com", destination.Host);
            Assert.Equal("/play", destination.AbsolutePath);
            Assert.Equal("?game=belot", destination.Query);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedBrowserLaunchShouldShowARecoverableError(bool throws)
        {
            var page = new PageActions();
            page.Activate();
            var errors = 0;

            await OnlinePlay.OpenAsync(
                page,
                _ => throws ? throw new InvalidOperationException("No browser") : Task.FromResult(false),
                () =>
                {
                    errors++;
                    return Task.CompletedTask;
                });

            Assert.Equal(1, errors);
        }

        [Fact]
        public async Task RepeatedTapsShouldShareThePageGateAndAllowRetryAfterTheErrorCloses()
        {
            var page = new PageActions();
            page.Activate();
            var browser = new TaskCompletionSource<bool>();
            var dialog = new TaskCompletionSource();
            var launches = 0;
            var errors = 0;
            Task<bool> Open(string url)
            {
                launches++;
                return browser.Task;
            }

            Task ShowFailure()
            {
                errors++;
                return dialog.Task;
            }

            var first = OnlinePlay.OpenAsync(page, Open, ShowFailure);
            await OnlinePlay.OpenAsync(page, Open, ShowFailure);
            await page.RunAsync(() => throw new InvalidOperationException("Navigation during browser launch"));
            Assert.Equal(1, launches);

            browser.SetResult(false);
            Assert.Equal(1, errors);
            await OnlinePlay.OpenAsync(page, Open, ShowFailure);
            Assert.Equal(1, launches);

            dialog.SetResult();
            await first;
            await OnlinePlay.OpenAsync(page, Open, ShowFailure);
            Assert.Equal(2, launches);
            Assert.Equal(2, errors);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AFailedLaunchShouldNotShowAnErrorAfterLeavingTheOriginalPageVisit(bool returnToPage)
        {
            var page = new PageActions();
            page.Activate();
            var browser = new TaskCompletionSource<bool>();
            var pending = OnlinePlay.OpenAsync(
                page,
                _ => browser.Task,
                () => throw new InvalidOperationException("Stale browser error"));

            page.Deactivate();
            if (returnToPage)
            {
                page.Activate();
            }

            browser.SetResult(false);
            await pending;
        }

        [Theory]
        [InlineData(LocalizationManager.English, "Play people online")]
        [InlineData(LocalizationManager.Bulgarian, "Играй с хора онлайн")]
        public void OnlinePlayShouldExplainItsDestinationAndRecoveryInBothLanguages(string language, string title)
        {
            var text = AppStrings.Table(language);
            Assert.Equal(title, text["Start_PlayOnline"]);
            Assert.Contains("ednaigra.com", text["Start_OnlineHint"]);
            Assert.Contains("ednaigra.com", text["Start_OnlineUnavailable"]);
        }
    }
}
