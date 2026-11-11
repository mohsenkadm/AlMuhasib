namespace AlMuhasib.Core.Enums;

/// <summary>مقاسات ملصق طباعة الباركود بالمليمتر.</summary>
public enum BarcodeLabelSize
{
    Mm30x15 = 0,
    Mm38x25 = 1,
    Mm30x20 = 2,
    Mm40x20 = 3,
    Mm40x25 = 4,
    Mm40x30 = 5,
    Mm50x25 = 6,
    Mm50x30 = 7,
    Mm58x40 = 8,
    Mm60x40 = 9,
    Mm80x50 = 10,
    Mm100x50 = 11,
    Custom = 12
}

public static class BarcodeLabelSizeHelper
{
    public const double Dpi = 96.0;
    public const double MmPerInch = 25.4;
    public const double DefaultCustomWidthMm = 40;
    public const double DefaultCustomHeightMm = 30;
    public const double DefaultGapMm = 2;
    public const BarcodeLabelSize DefaultSize = BarcodeLabelSize.Mm40x30;

    public static double MmToDip(double mm) => mm * Dpi / MmPerInch;

    public static IReadOnlyList<BarcodeLabelSize> AllSizes { get; } =
    [
        BarcodeLabelSize.Mm30x15,
        BarcodeLabelSize.Mm30x20,
        BarcodeLabelSize.Mm38x25,
        BarcodeLabelSize.Mm40x20,
        BarcodeLabelSize.Mm40x25,
        BarcodeLabelSize.Mm40x30,
        BarcodeLabelSize.Mm50x25,
        BarcodeLabelSize.Mm50x30,
        BarcodeLabelSize.Mm58x40,
        BarcodeLabelSize.Mm60x40,
        BarcodeLabelSize.Mm80x50,
        BarcodeLabelSize.Mm100x50,
        BarcodeLabelSize.Custom
    ];

    public static (double WidthMm, double HeightMm) GetMillimeters(
        BarcodeLabelSize size,
        double? customWidthMm = null,
        double? customHeightMm = null)
    {
        if (size == BarcodeLabelSize.Custom)
        {
            var w = ClampDimension(customWidthMm ?? DefaultCustomWidthMm);
            var h = ClampDimension(customHeightMm ?? DefaultCustomHeightMm);
            return (w, h);
        }

        return size switch
        {
            BarcodeLabelSize.Mm30x15 => (30, 15),
            BarcodeLabelSize.Mm30x20 => (30, 20),
            BarcodeLabelSize.Mm38x25 => (38, 25),
            BarcodeLabelSize.Mm40x20 => (40, 20),
            BarcodeLabelSize.Mm40x25 => (40, 25),
            BarcodeLabelSize.Mm40x30 => (40, 30),
            BarcodeLabelSize.Mm50x25 => (50, 25),
            BarcodeLabelSize.Mm50x30 => (50, 30),
            BarcodeLabelSize.Mm58x40 => (58, 40),
            BarcodeLabelSize.Mm60x40 => (60, 40),
            BarcodeLabelSize.Mm80x50 => (80, 50),
            BarcodeLabelSize.Mm100x50 => (100, 50),
            _ => (40, 30)
        };
    }

    public static double ClampDimension(double mm) => Math.Clamp(mm, 10, 200);

    public static (double WidthDip, double HeightDip) GetPageSizeDip(
        BarcodeLabelSize size,
        double? customWidthMm = null,
        double? customHeightMm = null)
    {
        var (w, h) = GetMillimeters(size, customWidthMm, customHeightMm);
        return (MmToDip(w), MmToDip(h));
    }

    public static string GetDisplayName(BarcodeLabelSize size) => size switch
    {
        BarcodeLabelSize.Mm30x15 => "30 × 15 مم",
        BarcodeLabelSize.Mm30x20 => "30 × 20 مم",
        BarcodeLabelSize.Mm38x25 => "38 × 25 مم",
        BarcodeLabelSize.Mm40x20 => "40 × 20 مم",
        BarcodeLabelSize.Mm40x25 => "40 × 25 مم",
        BarcodeLabelSize.Mm40x30 => "40 × 30 مم",
        BarcodeLabelSize.Mm50x25 => "50 × 25 مم",
        BarcodeLabelSize.Mm50x30 => "50 × 30 مم",
        BarcodeLabelSize.Mm58x40 => "58 × 40 مم",
        BarcodeLabelSize.Mm60x40 => "60 × 40 مم",
        BarcodeLabelSize.Mm80x50 => "80 × 50 مم",
        BarcodeLabelSize.Mm100x50 => "100 × 50 مم",
        BarcodeLabelSize.Custom => "مخصص",
        _ => size.ToString()
    };

    /// <summary>للتوافق الخلفي — المحتوى يُتحكم به الآن عبر أعلام الملصق.</summary>
    public static bool ShowProductDetails(BarcodeLabelSize size) => size != BarcodeLabelSize.Mm30x15;

    public static BarcodeLabelLayoutMetrics GetLayout(
        BarcodeLabelSize size,
        double? customWidthMm = null,
        double? customHeightMm = null)
    {
        var (wMm, hMm) = GetMillimeters(size, customWidthMm, customHeightMm);
        var w = MmToDip(wMm);
        var h = MmToDip(hMm);
        var isTiny = hMm <= 18;
        var isCompact = hMm <= 25;
        var padding = isTiny ? 1.5 : isCompact ? 2.0 : 2.5;
        var horizontalInset = isTiny ? 3.0 : 6.0;

        var companyFs = ScaleFont(6.5, hMm, min: 5, max: 9);
        var productFs = ScaleFont(8.0, hMm, min: 6, max: 11);
        var codeFs = ScaleFont(6.5, hMm, min: 5, max: 9);
        var priceFs = ScaleFont(11.0, hMm, min: 8, max: 14);
        var goldFs = ScaleFont(6.5, hMm, min: 5, max: 9);

        var barcodeWidth = Math.Max(40, w - horizontalInset * 2);
        var barcodeHeight = isTiny
            ? Math.Max(14, h - 4)
            : Math.Max(18, Math.Min(h * 0.38, MmToDip(Math.Min(18, hMm * 0.45))));

        return new BarcodeLabelLayoutMetrics(
            PageWidth: w,
            PageHeight: h,
            Padding: padding,
            CompanyFontSize: companyFs,
            ProductFontSize: productFs,
            CodeFontSize: codeFs,
            PriceFontSize: priceFs,
            GoldFontSize: goldFs,
            BarcodeWidth: barcodeWidth,
            BarcodeHeight: barcodeHeight,
            SectionBottomMargin: 0);
    }

    private static double ScaleFont(double baseSize, double heightMm, double min, double max)
    {
        var scaled = baseSize * (heightMm / 30.0);
        return Math.Clamp(scaled, min, max);
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
