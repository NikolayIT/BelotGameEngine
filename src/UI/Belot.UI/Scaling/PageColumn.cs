namespace Belot.UI.Scaling
{
    using System;

    using Belot.UI.Game;

    /// <summary>
    /// The width of a scrolling page's content column: the whole page on a phone, a readable
    /// column (540 design units) centered on a bigger screen.
    /// </summary>
    public static class PageColumn
    {
        public const double MaxDesignWidth = 540;

        public static double Width(double pageWidth) => Math.Max(0, Math.Min(pageWidth, MaxDesignWidth * UiScale.Current.Page));
    }
}
