namespace AlMuhasib.Core.Models.Import;

/// <summary>خيارات أعمدة استيراد/إدخال المنتجات حسب الميزات المفعّلة والحقول المخصصة.</summary>
public sealed class ProductImportOptions
{
    public bool IncludePharmacyFields { get; set; }
    public bool IncludeWeightFields { get; set; }
    public bool IncludeDiscountFields { get; set; }
    public bool IncludePricingFields { get; set; }

    /// <summary>تسميات الحقول المخصصة الظاهرة (مفتاح cf1..cf8 → التسمية).</summary>
    public IReadOnlyList<ProductImportCustomField> CustomFields { get; set; } = [];
}

public sealed class ProductImportCustomField
{
    public int Slot { get; set; }
    public string Header { get; set; } = string.Empty;
    public string SlotKey => $"cf{Slot}";
}
