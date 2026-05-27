using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Converters;

/// <summary>Converts VoucherType enum to Arabic display text.</summary>
public class VoucherTypeToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VoucherType type)
        {
            return type switch
            {
                VoucherType.Receipt => "سند قبض",
                VoucherType.Payment => "سند صرف",
                VoucherType.BankReceipt => "سند قبض مصرفي",
                VoucherType.InvestorDeposit => "إيداع مستثمر",
                VoucherType.InvestorWithdrawal => "سحب مستثمر",
                VoucherType.DebtReceipt => "سند قبض دين",
                _ => value.ToString() ?? string.Empty
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts VoucherType enum to a theme brush.</summary>
public class VoucherTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VoucherType type)
        {
            return type switch
            {
                VoucherType.Receipt => new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47)),     // Green
                VoucherType.Payment => new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)),     // Red
                VoucherType.BankReceipt => new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0)), // Blue
                VoucherType.InvestorDeposit => new SolidColorBrush(Color.FromRgb(0x00, 0xAC, 0xC1)),  // Cyan accent
                VoucherType.InvestorWithdrawal => new SolidColorBrush(Color.FromRgb(0x6A, 0x1B, 0x9A)), // Purple
                VoucherType.DebtReceipt => new SolidColorBrush(Color.FromRgb(0x00, 0x83, 0x8F)),  // Teal
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts TransferAccountType enum to Arabic text.</summary>
public class TransferAccountTypeToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TransferAccountType type)
        {
            return type switch
            {
                TransferAccountType.CashBox => "قاصة",
                TransferAccountType.Bank => "مصرف",
                _ => value.ToString() ?? string.Empty
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
