namespace Belot.UI.Tests
{
    using System;
    using System.Threading.Tasks;

    using Belot.UI.Game;

    using Xunit;

    // The pages open and close pages through OneAtATime, so a quick double tap navigates once
    // (two taps on an opponent used to open two games; two on Back popped two pages).
    public class OneAtATimeTests
    {
        [Fact]
        public async Task ACallWhileAnotherRunsShouldBeIgnored()
        {
            var gate = new OneAtATime();
            var runs = 0;
            var navigation = new TaskCompletionSource();

            var first = gate.RunAsync(() =>
            {
                runs++;
                return navigation.Task;
            });
            var second = gate.RunAsync(() =>
            {
                runs++;
                return Task.CompletedTask;
            });

            Assert.True(second.IsCompletedSuccessfully);
            Assert.Equal(1, runs);
            Assert.True(gate.IsRunning);

            navigation.SetResult();
            await first;
            Assert.False(gate.IsRunning);

            await gate.RunAsync(() =>
            {
                runs++;
                return Task.CompletedTask;
            });
            Assert.Equal(2, runs);
        }

        [Fact]
        public async Task AFailedRunShouldNotBlockTheNextOne()
        {
            var gate = new OneAtATime();
            await Assert.ThrowsAsync<InvalidOperationException>(() => gate.RunAsync(() => throw new InvalidOperationException()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => gate.RunAsync(() => Task.FromException(new InvalidOperationException())));

            var ran = false;
            await gate.RunAsync(() =>
            {
                ran = true;
                return Task.CompletedTask;
            });
            Assert.True(ran);
        }
    }
}
