using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IBarcodeLabelService
{
    void PrintLabels(IEnumerable<BarcodeLabelItem> items);

    void PrintLabels(IEnumerable<BarcodeLabelItem> items, BarcodeLabelSize size);

    void PrintLabels(
        IEnumerable<BarcodeLabelItem> items,
        BarcodeLabelSize size,
        double? customWidthMm,
        double? customHeightMm,
        double gapMm = 0);

    /// <summary>Creates a scannable Code128 barcode as PNG bytes for preview or printing.</summary>
    byte[]? CreateBarcodePng(string barcode, int width = 280, int height = 90);

    /// <summary>معاينة ملصق كاملة مطابقة للطباعة (مقاس الملصق الفعلي).</summary>
    byte[]? CreateLabelPreviewPng(BarcodeLabelItem item, BarcodeLabelSize size, double scale = 2.0);

    byte[]? CreateLabelPreviewPng(
        BarcodeLabelItem item,
        BarcodeLabelSize size,
        double? customWidthMm,
        double? customHeightMm,
        double scale = 2.0);
}

public class BarcodeLabelItem
{
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int? KaratValue { get; set; }
    public decimal? WeightGrams { get; set; }

    /// <summary>الافتراضي true لتوافق الطباعة القديمة؛ دايلوك الطباعة يضبطه حسب اختيار المستخدم.</summary>
    public bool ShowCompanyName { get; set; } = true;
    public bool ShowBranchName { get; set; }
    public bool ShowProductName { get; set; } = true;
    public bool ShowPrice { get; set; } = true;
    public bool ShowBarcodeNumber { get; set; } = true;
}
