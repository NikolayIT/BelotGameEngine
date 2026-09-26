namespace Belot.UI.Converters
{
    using System;
    using System.Globalization;

    /// <summary>A "#RRGGBB" string (the view models are MAUI-free) to a colour.</summary>
    public sealed class HexToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is string hex && hex.Length > 0 ? Color.FromArgb(hex) : Colors.Black;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
