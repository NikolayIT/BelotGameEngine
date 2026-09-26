namespace Belot.UI.Converters
{
    using System;
    using System.Globalization;

    /// <summary>True for a non-empty string (shows an element only when it has text).</summary>
    public sealed class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is string s && !string.IsNullOrEmpty(s);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
