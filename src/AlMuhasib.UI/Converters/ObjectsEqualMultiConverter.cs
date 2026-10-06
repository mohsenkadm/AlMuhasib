using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core.Entities;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.Converters;

/// <summary>
/// Compares two bound objects for POS choice-chip selection.
/// Prefers entity Id equality for Warehouse/CashBox; otherwise object.Equals.
/// </summary>
public sealed class ObjectsEqualMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is null || values[1] is null)
            return false;

        if (values[0] is Warehouse wa && values[1] is Warehouse wb)
            return wa.Id == wb.Id;
        if (values[0] is CashBox ca && values[1] is CashBox cb)
            return ca.Id == cb.Id;
        if (values[0] is PricingType pa && values[1] is PricingType pb)
            return pa.Id == pb.Id;
        if (values[0] is DiscountTypeOption da && values[1] is DiscountTypeOption db)
            return da.Type == db.Type;

        return Equals(values[0], values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
