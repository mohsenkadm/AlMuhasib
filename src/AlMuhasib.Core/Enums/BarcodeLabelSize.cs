namespace AlMuhasib.Core.Enums;

/// <summary>مقاسات ملصق طباعة الباركود بالمليمتر.</summary>
public enum BarcodeLabelSize
{
    /// <summary>30 × 15 مم</summary>
    Mm30x15 = 0,

    /// <summary>38 × 25 مم</summary>
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
        BarcodeLabelSize.Mm30x15 => "30 × 15 مم",
        BarcodeLabelSize.Mm38x25 => "38 × 25 مم",
        _ => size.ToString()
    };

    public static BarcodeLabelLayoutMetrics GetLayout(BarcodeLabelSize size)
    {
        var (w, h) = GetPageSizeDip(size);
        return size switch
        {
            BarcodeLabelSize.Mm30x15 => new BarcodeLabelLayoutMetrics(
                PageWidth: w,
                PageHeight: h,
                Padding: 2,
                CompanyFontSize: 6,
                ProductFontSize: 7,
                CodeFontSize: 6,
                PriceFontSize: 7,
                GoldFontSize: 6,
                BarcodeWidth: Math.Max(40, w - 8),
                BarcodeHeight: Math.Max(18, h * 0.35),
                SectionBottomMargin: 0),
            _ => new BarcodeLabelLayoutMetrics(
                PageWidth: w,
                PageHeight: h,
                Padding: 3,
                CompanyFontSize: 7.5,
                ProductFontSize: 9,
                CodeFontSize: 7.5,
                PriceFontSize: 9,
                GoldFontSize: 7.5,
                BarcodeWidth: Math.Max(50, w - 10),
                BarcodeHeight: Math.Max(24, h * 0.38),
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
