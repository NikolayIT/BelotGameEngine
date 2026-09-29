namespace Belot.UI.Game
{
    using System;
    using System.Threading.Tasks;

    /// <summary>Opens the privacy policy and limits a launch failure to the current settings visit.</summary>
    public static class PrivacyPolicy
    {
        public const string Url = "https://nksolutions.com/belot/privacy-policy.html";

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
