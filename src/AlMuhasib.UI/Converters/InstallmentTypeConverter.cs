using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Converters;

public class InstallmentTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is InstallmentType type ? type switch
        {
            InstallmentType.Manual => "يدوي",
            InstallmentType.Platform => "بيع منصة",
            InstallmentType.OpeningBalance => "رصيد افتتاحي",
            _ => "—"
        } : "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
