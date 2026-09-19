using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.UI.Converters;

public sealed class VehiclePlateTypeDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is VehiclePlateType type
            ? VehiclePlateTypeHelper.ToDisplay(type)
            : VehiclePlateTypeHelper.NoneLabel;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        VehiclePlateTypeHelper.Parse(value?.ToString());
}
