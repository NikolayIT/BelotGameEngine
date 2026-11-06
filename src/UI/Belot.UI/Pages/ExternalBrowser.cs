namespace Belot.UI.Pages
{
    /// <summary>Opens a link outside the app, as the app and its privacy policy say it does.</summary>
    internal static class ExternalBrowser
    {
        // On iOS the "preferred" browser is a Safari view inside the app, so there links go to Safari.
#if IOS
        private const BrowserLaunchMode Mode = BrowserLaunchMode.External;
#else
        private const BrowserLaunchMode Mode = BrowserLaunchMode.SystemPreferred;
#endif

        public static Task<bool> OpenAsync(string url) => Browser.Default.OpenAsync(url, Mode);
    }
}
