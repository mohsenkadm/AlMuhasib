using System.Globalization;
using System.Windows.Data;

namespace AlMuhasib.UI.Converters;

/// <summary>
/// Returns true if the bound integer value is less than 3 (for warning styling).
/// Used as a singleton via <see cref="Instance"/>.
/// </summary>
public class LessThanConverter : IValueConverter
{
    public static readonly LessThanConverter Instance = new();

    public int Threshold { get; set; } = 3;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int i)
            return i < Threshold;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
