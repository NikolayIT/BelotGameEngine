namespace Belot.UI.Game
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Runs one action at a time and ignores calls while it runs: a quick double tap on a button
    /// that opens (or closes) a page navigates once, not twice. No MAUI types here: the UI tests
    /// compile this file.
    /// </summary>
    public sealed class OneAtATime
    {
        private bool running;

        public bool IsRunning => this.running;

        public async Task RunAsync(Func<Task> action)
        {
            if (this.running)
            {
                return;
            }

            this.running = true;
            try
            {
                await action();
            }
            finally
            {
                this.running = false;
            }
        }
    }
}
