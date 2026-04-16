using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace AlMuhasib.UI;

public class BoolToWidthConverter : MarkupExtension, IValueConverter
{
    public double ExpandedWidth { get; set; } = 260;
    public double CollapsedWidth { get; set; } = 72;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? ExpandedWidth : CollapsedWidth;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
