namespace Belot.UI.Scaling
{
    using System;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// A design size times the current scale, as whatever the target property needs: a number
    /// (font size, width, spacing, translation), an int (a button's corner radius), a
    /// <see cref="CornerRadius"/>, a <see cref="Thickness"/> ("16,12" or "24,20,24,32") or a
    /// <see cref="GridLength"/>.
    /// </summary>
    public sealed class ScaleConverter : IValueConverter
    {
        public static ScaleConverter Instance { get; } = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var scale = value is double s ? s : 1d;
            var sizes = (parameter as string ?? "0")
                .Split(',')
                .Select(x => double.Parse(x.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture) * scale)
                .ToArray();

            if (targetType == typeof(Thickness))
            {
                return sizes.Length switch
                {
                    1 => new Thickness(sizes[0]),
                    2 => new Thickness(sizes[0], sizes[1]),
                    _ => new Thickness(sizes[0], sizes[1], sizes[2], sizes[3]),
                };
            }

            if (targetType == typeof(CornerRadius))
            {
                return new CornerRadius(sizes[0]);
            }

            if (targetType == typeof(GridLength))
            {
                return new GridLength(sizes[0]);
            }

            if (targetType == typeof(int))
            {
                return (int)Math.Round(sizes[0]);
            }

            return sizes[0];
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
