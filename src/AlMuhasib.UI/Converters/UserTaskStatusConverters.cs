using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Converters;

public class UserTaskStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not UserTaskStatus status)
            return Brushes.Gray;

        var color = status switch
        {
            UserTaskStatus.InProgress => "#1565C0",
            UserTaskStatus.Completed => "#2E7D32",
            _ => "#F9A825"
        };

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class UserTaskStatusToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not UserTaskStatus status)
            return Brushes.LightGray;

        var color = status switch
        {
            UserTaskStatus.InProgress => "#E3F2FD",
            UserTaskStatus.Completed => "#E8F5E9",
            _ => "#FFF8E1"
        };

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
