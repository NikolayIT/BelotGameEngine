namespace Belot.UI.Pages
{
    using System.Diagnostics;

    /// <summary>The felt behind a page, on the phones whose screen goes around the system's bars.</summary>
    internal static class PageBackground
    {
        // An iPhone keeps a page's content inside the safe area, so a gradient on the content ends
        // in a hard edge below the status bar and above the home indicator. On the page itself it
        // covers the whole screen, and the content stays where it is.
        [Conditional("IOS")]
        public static void CoverTheWholeScreen(ContentPage page)
        {
            if (page.Content is Microsoft.Maui.Controls.Layout root)
            {
                page.Background = root.Background;
                root.Background = Brush.Transparent;
            }
        }
    }
}
