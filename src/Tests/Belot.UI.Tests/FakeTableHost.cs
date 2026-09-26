namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.UI.Game;

    // The page around the table: timers are kept until the test runs them, like a clock the test
    // moves forward.
    internal sealed class FakeTableHost : IGameTableHost
    {
        public List<(TimeSpan Delay, Action Action)> Timers { get; } = new();

        public int Leaves { get; private set; }

        public int Vibrations { get; private set; }

        public void After(TimeSpan delay, Action action) => this.Timers.Add((delay, action));

        public void Vibrate(bool isLong) => this.Vibrations++;

        public void Leave() => this.Leaves++;

        // Runs the oldest timer still waiting.
        public void RunOldestTimer()
        {
            var (_, action) = this.Timers[0];
            this.Timers.RemoveAt(0);
            action();
        }

        // Runs the timers set so far (not the ones they set).
        public void RunTimers()
        {
            var due = this.Timers.ToList();
            this.Timers.Clear();
            foreach (var (_, action) in due)
            {
                action();
            }
        }
    }
}
