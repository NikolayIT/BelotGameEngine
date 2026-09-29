namespace Belot.UI.Game
{
    using System;
    using System.Threading.Tasks;

    /// <summary>Opens multiplayer while keeping a failed launch tied to the current page visit.</summary>
    public static class OnlinePlay
    {
        public const string Url = "https://ednaigra.com/play?game=belot";

        public static Task OpenAsync(PageActions actions, Func<string, Task<bool>> openBrowser, Func<Task> showFailure) => actions.ConfirmAsync(
            async () =>
            {
                try
                {
                    return !await openBrowser(Url);
                }
                catch
                {
                    return true;
                }
            },
            showFailure);
    }
}
