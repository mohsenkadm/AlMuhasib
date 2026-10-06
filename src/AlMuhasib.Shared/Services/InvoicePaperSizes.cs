using System.Windows;
using QuestPDF.Helpers;

namespace AlMuhasib.Shared.Services;

/// <summary>Paper sizes for full-page invoices and reports (96 DPI DIP units).</summary>
public static class InvoicePaperSizes
{
    public const string A4 = "A4";
    public const string A5 = "A5";
    public const string Letter = "Letter";

    public const string Default = A4;

    /// <summary>Reference A4 width used to scale fonts/paddings for other sizes.</summary>
    public const double ReferenceWidth = 793.7;

    public static readonly string[] All = [A4, A5, Letter];

    public static Size GetPageSize(string? paperSize) => Normalize(paperSize) switch
    {
        A5 => new Size(559.37, 793.7),
        Letter => new Size(816, 1056),
        _ => new Size(793.7, 1122.5)
    };

    /// <summary>Linear scale relative to A4 width (A5 ≈ 0.707).</summary>
    public static double GetScale(string? paperSize)
    {
        var size = GetPageSize(paperSize);
        return size.Width / ReferenceWidth;
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
        A5 => "A5 — فاتورة نصف صفحة",
        Letter => "Letter — ورق أمريكي",
        _ => "A4 — فاتورة كاملة"
    };

    public static PageSize GetQuestPdfPageSize(string? paperSize) => Normalize(paperSize) switch
    {
        A5 => PageSizes.A5,
        Letter => PageSizes.Letter,
        _ => PageSizes.A4
    };
}
