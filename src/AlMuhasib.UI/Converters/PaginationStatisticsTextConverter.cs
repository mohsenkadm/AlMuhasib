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

        var totalCount = ReadInt(values, 1);
        var totalRecords = ReadInt(values, 2);
        var currentPage = ReadInt(values, 3, 1);
        var pageSize = ReadInt(values, 4, 20);
        var total = totalCount > 0 ? totalCount : totalRecords;

        if (total <= 0)
            return "لا توجد سجلات";

        var start = (currentPage - 1) * pageSize + 1;
        var end = Math.Min(currentPage * pageSize, total);
        return $"عرض {start}-{end} من {total:N0}";
    }

    private static int ReadInt(object[] values, int index, int defaultValue = 0)
    {
        if (values.Length <= index)
            return defaultValue;

        return values[index] switch
        {
            int i => i,
            _ => defaultValue
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
