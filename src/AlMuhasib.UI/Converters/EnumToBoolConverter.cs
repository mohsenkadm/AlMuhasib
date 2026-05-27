using System.Globalization;
using System.Windows.Data;

namespace AlMuhasib.UI.Converters;

/// <summary>
/// Converts any enum value to bool for RadioButton binding.
/// ConverterParameter should be the enum value name.
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is not string paramStr) return false;
        return value.ToString() == paramStr;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string paramStr)
            return Enum.Parse(targetType, paramStr);
        return Binding.DoNothing;
    }
}
