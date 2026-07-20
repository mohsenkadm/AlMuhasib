using System.Windows;

namespace AlMuhasib.Shared.Services;

/// <summary>Paper sizes for POS cashier receipts (96 DPI DIP units).</summary>
public static class PosReceiptPaperSizes
{
    public const string A4 = "A4";
    public const string Mm80 = "80mm";
    public const string Mm58 = "58mm";
    public const string Mm50 = "50mm";

    public const string Default = Mm80;

    public static readonly string[] All = [A4, Mm80, Mm58, Mm50];

    /// <summary>Width in DIPs at 96 DPI. Height is a generous roll length for thermal; A4 uses full page.</summary>
    public static Size GetPageSize(string? paperSize)
    {
        return Normalize(paperSize) switch
        {
            A4 => new Size(793.7, 1122.5),
            Mm80 => new Size(302, 2000),
            Mm58 => new Size(219, 2000),
            Mm50 => new Size(189, 2000),
            _ => new Size(302, 2000)
        };
    }

    public static bool IsThermal(string? paperSize)
    {
        var n = Normalize(paperSize);
        return n is Mm80 or Mm58 or Mm50;
    }

    public static string Normalize(string? paperSize)
    {
        if (string.IsNullOrWhiteSpace(paperSize))
            return Default;

        var trimmed = paperSize.Trim();
        foreach (var known in All)
        {
            if (known.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                return known;
        }

        return Default;
    }

    public static string GetDisplayLabel(string? paperSize) => Normalize(paperSize) switch
    {
        A4 => "A4 — فاتورة كاملة",
        Mm80 => "80mm — حرارية قياسية",
        Mm58 => "58mm — حرارية ضيقة",
        Mm50 => "50mm — حرارية صغيرة",
        _ => "80mm — حرارية قياسية"
    };
}
