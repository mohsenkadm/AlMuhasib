using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Converters;

public class UserTaskStatusLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not UserTaskStatus status)
            return string.Empty;

        return status switch
        {
            UserTaskStatus.InProgress => "قيد التنفيذ",
            UserTaskStatus.Completed => "مكتملة",
            _ => "قيد الانتظار"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
