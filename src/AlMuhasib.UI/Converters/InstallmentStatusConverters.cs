using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Converters;

/// <summary>
/// Converts InstallmentStatus to a SolidColorBrush for color-coded display.
/// </summary>
public class InstallmentStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is InstallmentStatus status)
        {
            return status switch
            {
                InstallmentStatus.Paid => new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),       // green
                InstallmentStatus.PartiallyPaid => new SolidColorBrush(Color.FromRgb(0x00, 0xAC, 0xC1)), // cyan accent
                InstallmentStatus.Overdue => new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),      // red
                InstallmentStatus.Pending => new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75)),      // grey
                _ => new SolidColorBrush(Color.FromRgb(0x42, 0x42, 0x42))
            };
        }
        return new SolidColorBrush(Color.FromRgb(0x42, 0x42, 0x42));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts InstallmentStatus enum to Arabic display text.
/// </summary>
public class InstallmentStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is InstallmentStatus status)
        {
            return status switch
            {
                InstallmentStatus.Paid => "مسدد",
                InstallmentStatus.PartiallyPaid => "مسدد جزئياً",
                InstallmentStatus.Overdue => "متأخر",
                InstallmentStatus.Pending => "قيد الانتظار",
                _ => value.ToString()!
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
