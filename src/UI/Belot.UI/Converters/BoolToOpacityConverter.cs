namespace Belot.UI.Converters
{
    using System;
    using System.Globalization;

    /// <summary>Full opacity when true, faded when false.</summary>
    public sealed class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            (value is bool b && b) ? 1.0 : 0.4;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
