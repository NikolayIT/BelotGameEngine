namespace Belot.UI.Game
{
    using System;

    /// <summary>The card boxes and overlap that fit a side seat's actual remaining space.</summary>
    public readonly record struct VerticalCardBackGeometry(double CardWidth, double CardHeight, double Step, double Height)
    {
        public static VerticalCardBackGeometry Fit(int count, double availableWidth, double availableHeight, double scale)
        {
            if (count <= 0)
            {
                return default;
            }

            scale = double.IsFinite(scale) && scale > 0 ? scale : 1;
            var normalWidth = 38 * scale;
            var normalHeight = 53 * scale;
            var normalStep = 13 * scale;
            var width = Limit(availableWidth, normalWidth);
            var height = Limit(availableHeight, normalHeight + ((count - 1) * normalStep));
            if (width == 0 || height == 0)
            {
                return default;
            }

            var factor = Math.Min(1, width / normalWidth);
            var cardWidth = normalWidth * factor;
            var cardHeight = normalHeight * factor;
            var minimumStep = 2 * scale * factor;
            var minimumHeight = cardHeight + ((count - 1) * minimumStep);
            if (height < minimumHeight)
            {
                // Keep every back represented even when a full-height card cannot fit.
                var shrink = height / minimumHeight;
                factor *= shrink;
                cardWidth *= shrink;
                cardHeight *= shrink;
            }

            var step = count == 1 ? 0 : Math.Min(normalStep * factor, Math.Max(0, (height - cardHeight) / (count - 1)));
            return new VerticalCardBackGeometry(cardWidth, cardHeight, step, cardHeight + ((count - 1) * step));
        }

        private static double Limit(double constraint, double natural) =>
            double.IsPositiveInfinity(constraint) ? natural : double.IsFinite(constraint) ? Math.Max(0, constraint) : 0;
    }
}
