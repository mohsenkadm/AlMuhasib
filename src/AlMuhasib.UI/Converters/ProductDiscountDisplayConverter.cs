using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Converters;

public sealed class ProductDiscountDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Product product)
            return ProductDiscountHelper.FormatDiscountDisplay(product);
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
