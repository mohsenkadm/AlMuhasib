using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Converters;

/// <summary>
/// Converts between PaymentMethod enum and boolean for RadioButton binding.
/// ConverterParameter should be the PaymentMethod enum name (e.g., "Cash", "Credit", "Installment").
/// </summary>
public class PaymentMethodConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PaymentMethod method && parameter is string param)
        {
            return Enum.TryParse<PaymentMethod>(param, out var target) && method == target;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string param)
        {
            if (Enum.TryParse<PaymentMethod>(param, out var target))
                return target;
        }
        return Binding.DoNothing;
    }
}
