using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AlMuhasib.UI.Converters;

/// <summary>
/// عرض/إدخال مبالغ بفواصل آلاف.
/// ConvertBack يزيل فواصل الآلاف دائماً ولا يفسّر الفاصلة كفاصل عشري (مهم أثناء الحذف).
/// </summary>
public sealed class DecimalThousandsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var format = parameter as string;
        if (string.IsNullOrWhiteSpace(format))
            format = "N0";

        return value switch
        {
            decimal d => d.ToString(format, culture),
            double dbl => ((decimal)dbl).ToString(format, culture),
            float f => ((decimal)f).ToString(format, culture),
            int i => ((decimal)i).ToString(format, culture),
            long l => ((decimal)l).ToString(format, culture),
            null => string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return 0m;

        var text = value.ToString()?.Trim() ?? string.Empty;
        if (text.Length == 0)
            return 0m;

        // أزل فواصل الآلاف دائماً — لا تفسّر ',' كفاصل عشري
        text = text
            .Replace("٬", string.Empty)
            .Replace(",", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);

        // إن بقي تنسيق أوروبي نادر 1.234.567,89
        if (text.Contains('.') && text.Contains(','))
        {
            text = text.Replace(".", string.Empty).Replace(',', '.');
        }

        if (text is "." or "-")
            return Binding.DoNothing;

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || decimal.TryParse(text, NumberStyles.Number, culture, out parsed))
            return parsed;

        return Binding.DoNothing;
    }
}
