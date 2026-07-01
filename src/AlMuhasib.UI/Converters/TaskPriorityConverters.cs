using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AlMuhasib.UI.Converters;

public sealed class TaskPriorityToBrushConverter : IValueConverter
{
    public static readonly TaskPriorityToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var priority = value is int p ? p : 99;
        var key = priority switch
        {
            1 => "DashboardPriorityHighBrush",
            2 => "DashboardPriorityMediumBrush",
            _ => "DashboardPriorityLowBrush"
        };

        return Application.Current.TryFindResource(key) as Brush ?? Brushes.SteelBlue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class TaskPriorityToLightBrushConverter : IValueConverter
{
    public static readonly TaskPriorityToLightBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var priority = value is int p ? p : 99;
        var key = priority switch
        {
            1 => "DashboardPriorityHighLightBrush",
            2 => "DashboardPriorityMediumLightBrush",
            _ => "DashboardPriorityLowLightBrush"
        };

        return Application.Current.TryFindResource(key) as Brush ?? Brushes.AliceBlue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
