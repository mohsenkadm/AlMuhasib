using System.Globalization;
using System.Windows.Data;

namespace AlMuhasib.UI.Converters;

/// <summary>Converts a parent ActualWidth to a fraction (ConverterParameter, default 0.4).</summary>
public sealed class PercentWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double width || width <= 0)
            return 360d;

        var factor = 0.4;
        if (parameter is string s &&
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0 && parsed <= 1)
        {
            factor = parsed;
        }

        return Math.Max(280, width * factor);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
