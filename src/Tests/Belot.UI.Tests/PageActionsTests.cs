namespace Belot.UI.Tests
{
    using System;
    using System.Threading.Tasks;

    using Belot.UI.Game;

    using Xunit;

    public class PageActionsTests
    {
        [Fact]
        public async Task RepeatedResetAndNavigationShareOneGateUntilTheResultDialogCloses()
        {
            var page = new PageActions();
            page.Activate();
            var confirmation = new TaskCompletionSource<bool>();
            var resultDialog = new TaskCompletionSource();
            var confirmations = 0;
            var resets = 0;
            var navigations = 0;

            Task<bool> Confirm()
            {
                confirmations++;
                return confirmation.Task;
            }

            Task Reset()
            {
                resets++;
                return resultDialog.Task;
            }

            Task Navigate()
            {
                navigations++;
                return Task.CompletedTask;
            }

            var first = page.ConfirmAsync(Confirm, Reset);
            await page.ConfirmAsync(Confirm, Reset);
            await page.RunAsync(Navigate);
            Assert.Equal(1, confirmations);
            Assert.Equal(0, navigations);

            confirmation.SetResult(true);
            Assert.Equal(1, resets);
            await page.ConfirmAsync(Confirm, Reset);
            await page.RunAsync(Navigate);
            Assert.Equal(1, confirmations);
            Assert.Equal(0, navigations);

            resultDialog.SetResult();
            await first;
            await page.RunAsync(Navigate);
            Assert.Equal(1, navigations);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LeavingThePageInvalidatesItsPendingConfirmationEvenAfterReturning(bool returnToPage)
        {
            var page = new PageActions();
            page.Activate();
            var confirmation = new TaskCompletionSource<bool>();
            var resets = 0;
            var pending = page.ConfirmAsync(() => confirmation.Task, () =>
            {
                resets++;
                return Task.CompletedTask;
            });

            page.Deactivate();
            if (returnToPage)
            {
                page.Activate();
            }

            confirmation.SetResult(true);
            await pending;
            Assert.Equal(0, resets);
        }

        [Fact]
        public async Task AHiddenPageCannotNavigateOrOpenADialog()
        {
            var page = new PageActions();
            await page.RunAsync(() => throw new InvalidOperationException("Hidden navigation"));
            page.Activate();
            page.Deactivate();
            await page.ConfirmAsync(() => throw new InvalidOperationException("Hidden confirmation"), () => Task.CompletedTask);
        }

        [Fact]
        public async Task CancelledAndFailedDialogsReleaseTheGate()
        {
            var page = new PageActions();
            page.Activate();
            await page.ConfirmAsync(() => Task.FromResult(false), () => throw new InvalidOperationException("Cancelled reset"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => page.ConfirmAsync(
                () => Task.FromException<bool>(new InvalidOperationException()),
                () => Task.CompletedTask));

            var ran = false;
            await page.RunAsync(() =>
            {
                ran = true;
                return Task.CompletedTask;
            });
            Assert.True(ran);
        }
    }
}
