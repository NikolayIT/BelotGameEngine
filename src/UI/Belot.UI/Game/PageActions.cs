namespace Belot.UI.Game
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Serializes a visible page's navigation and dialogs. A confirmation belongs to the page
    /// visit that opened it; returning to the page cannot revive a stale confirmation.
    /// </summary>
    public sealed class PageActions
    {
        private readonly OneAtATime gate = new();

        private bool active;

        private long visit;

        public void Activate()
        {
            this.active = true;
            this.visit++;
        }

        public void Deactivate()
        {
            this.active = false;
            this.visit++;
        }

        public Task RunAsync(Func<Task> action) => this.active ? this.gate.RunAsync(action) : Task.CompletedTask;

        public Task ConfirmAsync(Func<Task<bool>> confirm, Func<Task> confirmed) => this.RunAsync(async () =>
        {
            var visit = this.visit;
            if (await confirm() && this.active && this.visit == visit)
            {
                await confirmed();
            }
        });
    }
}
