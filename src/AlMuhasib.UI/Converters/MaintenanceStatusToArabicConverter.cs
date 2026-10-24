using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.UI.Converters;

public sealed class MaintenanceStatusToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is MaintenanceStatus status
            ? MaintenanceStatusHelper.ToArabic(status)
            : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
