using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.UI.Converters;

/// <summary>
/// values[0]=amount, values[1]=AccountingCurrency?, values[2]=ShowMultiCurrency? (اختياري).
/// عند إيقاف تعدد العملات يُعرض دائماً كدينار.
/// </summary>
public sealed class AmountWithCurrencyMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length == 0 || values[0] is null || values[0] == DependencyProperty.UnsetValue)
            return string.Empty;

        if (!TryToDecimal(values[0], culture, out var amount))
            return values[0]?.ToString() ?? string.Empty;

        var showMulti = ResolveShowMulti(values, index: 2);

        var currency = AccountingCurrency.IQD;
        if (showMulti && values.Length > 1 && values[1] != DependencyProperty.UnsetValue)
            currency = AccountingCurrencyHelper.ResolveOrDefault(values[1]);

        return AccountingCurrencyHelper.Format(amount, currency);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    internal static bool ResolveShowMulti(object[] values, int index)
    {
        // الافتراضي true حتى لا نخفي الدولار إن فشل الربط الثالث
        if (values.Length <= index || values[index] is null || values[index] == DependencyProperty.UnsetValue)
            return true;
        return values[index] is bool flag ? flag : true;
    }

    private static bool TryToDecimal(object value, CultureInfo culture, out decimal amount)
    {
        switch (value)
        {
            case decimal d:
                amount = d;
                return true;
            case double dbl:
                amount = (decimal)dbl;
                return true;
            case float f:
                amount = (decimal)f;
                return true;
            case int i:
                amount = i;
                return true;
            case long l:
                amount = l;
                return true;
            case string s when decimal.TryParse(s, NumberStyles.Any, culture, out var parsed):
                amount = parsed;
                return true;
            default:
                amount = 0;
                return false;
        }
    }
}

/// <summary>values[0]=AccountingCurrency?, values[1]=ShowMultiCurrency? → تسمية العملة.</summary>
public sealed class CurrencyLabelMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var showMulti = AmountWithCurrencyMultiConverter.ResolveShowMulti(values, index: 1);
        if (!showMulti)
            return AccountingCurrencyHelper.GetLabel(AccountingCurrency.IQD);

        var raw = values is { Length: > 0 } ? values[0] : null;
        if (raw is null || raw == DependencyProperty.UnsetValue)
            return AccountingCurrencyHelper.GetLabel(AccountingCurrency.IQD);

        return AccountingCurrencyHelper.GetLabel(AccountingCurrencyHelper.ResolveOrDefault(raw));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
