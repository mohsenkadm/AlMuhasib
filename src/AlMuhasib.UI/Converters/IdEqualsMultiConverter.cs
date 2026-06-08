using System.Globalization;
using System.Windows.Data;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Converters;

public class IdEqualsMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return false;

        var selected = values[0] as UserNoteItem;
        var item = values[1] as UserNoteItem;

        if (selected is null || item is null)
            return false;

        return selected.Id == item.Id;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
