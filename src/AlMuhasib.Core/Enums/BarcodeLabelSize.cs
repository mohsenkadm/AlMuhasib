namespace AlMuhasib.Core.Enums;

/// <summary>مقاسات ملصق طباعة الباركود بالمليمتر.</summary>
public enum BarcodeLabelSize
{
    /// <summary>صغير 30 × 15 مم — باركود فقط</summary>
    Mm30x15 = 0,

    /// <summary>كبير 38 × 25 مم — باركود مع الاسم والسعر</summary>
    Mm38x25 = 1
}

public static class BarcodeLabelSizeHelper
{
    public const double Dpi = 96.0;
    public const double MmPerInch = 25.4;

    public static double MmToDip(double mm) => mm * Dpi / MmPerInch;

    public static (double WidthMm, double HeightMm) GetMillimeters(BarcodeLabelSize size) => size switch
    {
        BarcodeLabelSize.Mm30x15 => (30, 15),
        BarcodeLabelSize.Mm38x25 => (38, 25),
        _ => (38, 25)
    };

    public static (double WidthDip, double HeightDip) GetPageSizeDip(BarcodeLabelSize size)
    {
        var (w, h) = GetMillimeters(size);
        return (MmToDip(w), MmToDip(h));
    }

    public static string GetDisplayName(BarcodeLabelSize size) => size switch
    {
        BarcodeLabelSize.Mm30x15 => "صغير 30×15 مم — باركود فقط",
        BarcodeLabelSize.Mm38x25 => "كبير 38×25 مم — مع الاسم والسعر",
        _ => size.ToString()
    };

    /// <summary>الملصق الصغير: باركود فقط بدون نصوص.</summary>
    public static bool ShowProductDetails(BarcodeLabelSize size) => size != BarcodeLabelSize.Mm30x15;

    public static BarcodeLabelLayoutMetrics GetLayout(BarcodeLabelSize size)
    {
        var (w, h) = GetPageSizeDip(size);
        return size switch
        {
            BarcodeLabelSize.Mm30x15 => new BarcodeLabelLayoutMetrics(
                PageWidth: w,
                PageHeight: h,
                Padding: 1.5,
                CompanyFontSize: 0,
                ProductFontSize: 0,
                CodeFontSize: 0,
                PriceFontSize: 0,
                GoldFontSize: 0,
                BarcodeWidth: Math.Max(48, w - 4),
                BarcodeHeight: Math.Max(22, h - 4),
                SectionBottomMargin: 0),
            _ => new BarcodeLabelLayoutMetrics(
                PageWidth: w,
                PageHeight: h,
                Padding: 2.5,
                CompanyFontSize: 7,
                ProductFontSize: 8.5,
                CodeFontSize: 7,
                PriceFontSize: 11.5,
                GoldFontSize: 7,
                BarcodeWidth: Math.Max(52, w - 8),
                BarcodeHeight: Math.Max(22, h * 0.34),
                SectionBottomMargin: 0)
        };
    }
}

public readonly record struct BarcodeLabelLayoutMetrics(
    double PageWidth,
    double PageHeight,
    double Padding,
    double CompanyFontSize,
    double ProductFontSize,
    double CodeFontSize,
    double PriceFontSize,
    double GoldFontSize,
    double BarcodeWidth,
    double BarcodeHeight,
    double SectionBottomMargin);
