using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.UI.Converters;

public sealed class GoldItemStatusLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GoldItemStatus status)
            return GoldItemStatusDisplay.ToArabic(status);
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
