using System.Globalization;
using System.Windows.Data;

namespace AlMuhasib.UI.Converters;

public sealed class PaginationStatisticsTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var paginationText = values.Length > 0 ? values[0] as string : null;
        if (!string.IsNullOrWhiteSpace(paginationText))
            return paginationText;

        var totalCount = values.Length > 1 && values[1] is int tc ? tc : 0;
        var totalRecords = values.Length > 2 && values[2] is int tr ? tr : 0;
        var total = totalCount > 0 ? totalCount : totalRecords;

        return total > 0 ? $"إجمالي {total:N0} سجل" : "لا توجد سجلات";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
