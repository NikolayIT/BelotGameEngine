namespace Belot.UI.Tests
{
    using System;
    using System.Threading.Tasks;

    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    public class PrivacyPolicyTests
    {
        [Fact]
        public async Task SuccessfulLaunchOpensTheBelotPrivacyPolicyWithoutAnError()
        {
            var page = new PageActions();
            page.Activate();
            string? opened = null;
            await PrivacyPolicy.OpenAsync(
                page,
                url =>
                {
                    opened = url;
                    return Task.FromResult(true);
                },
                () => throw new InvalidOperationException("A successful launch showed an error."));

            Assert.Equal("https://nksolutions.com/belot/privacy-policy.html", opened);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedBrowserLaunchShowsARecoverableError(bool throws)
        {
            var page = new PageActions();
            page.Activate();
            var errors = 0;
            await PrivacyPolicy.OpenAsync(
                page,
                _ => throws ? throw new InvalidOperationException("No browser") : Task.FromResult(false),
                () =>
                {
                    errors++;
                    return Task.CompletedTask;
                });

            Assert.Equal(1, errors);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task LateFailuresDoNotShowAnErrorAfterLeavingTheOriginalVisit(bool returnToPage, bool throws)
        {
            var page = new PageActions();
            page.Activate();
            var browser = new TaskCompletionSource<bool>();
            var pending = PrivacyPolicy.OpenAsync(
                page,
                _ => browser.Task,
                () => throw new InvalidOperationException("A browser failure reached a stale page."));

            page.Deactivate();
            if (returnToPage)
            {
                page.Activate();
            }

            if (throws)
            {
                browser.SetException(new InvalidOperationException("Late browser failure"));
            }
            else
            {
                browser.SetResult(false);
            }

            await pending;
        }

        [Fact]
        public async Task RepeatedTapsShareThePageGateAndAllowRetryAfterTheErrorCloses()
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

            var first = PrivacyPolicy.OpenAsync(page, Open, ShowFailure);
            await PrivacyPolicy.OpenAsync(page, Open, ShowFailure);
            await page.RunAsync(() => throw new InvalidOperationException("Navigation during browser launch"));
            Assert.Equal(1, launches);
            browser.SetResult(false);
            Assert.Equal(1, errors);
            await PrivacyPolicy.OpenAsync(page, Open, ShowFailure);
            Assert.Equal(1, launches);
            dialog.SetResult();
            await first;
            await PrivacyPolicy.OpenAsync(page, Open, ShowFailure);
            Assert.Equal(2, launches);
            Assert.Equal(2, errors);
        }

        [Theory]
        [InlineData(LocalizationManager.English, "Privacy policy")]
        [InlineData(LocalizationManager.Bulgarian, "Политика за поверителност")]
        public void PrivacyPolicyExplainsItsDestinationAndRecoveryInBothLanguages(string language, string title)
        {
            var text = AppStrings.Table(language);
            Assert.Equal(title, text["Settings_PrivacyPolicy"]);
            Assert.NotEmpty(text["Settings_PrivacyPolicyHint"]);
            Assert.Contains(PrivacyPolicy.Url, text["Settings_PrivacyPolicyUnavailable"]);
        }
    }
}
