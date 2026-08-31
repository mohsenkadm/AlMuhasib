namespace AlMuhasib.Core.Models.Import;

/// <summary>أعمدة قالب واستيراد المنتجات — مرتبة وثابتة بالأسماء العربية.</summary>
public static class ProductImportSchema
{
    public const string Name = "الاسم";
    public const string Barcode = "الباركود";
    public const string Category = "التصنيف";
    public const string Description = "الوصف";
    public const string ScientificName = "الاسم العلمي";
    public const string UsageInstructions = "طريقة الاستخدام";
    public const string Weight = "الوزن";
    public const string WeightUnit = "وحدة الوزن";
    public const string DiscountType = "نوع الخصم";
    public const string DiscountValue = "قيمة الخصم";
    public const string DiscountExpiresAt = "انتهاء الخصم";
    public const string SalePrice = "سعر البيع";
    public const string PurchasePrice = "سعر الشراء";
    public const string MinQuantity = "الحد الأدنى";

    public static IReadOnlyList<string> BuildHeaders(ProductImportOptions? options = null)
    {
        options ??= new ProductImportOptions();
        var headers = new List<string> { Name, Barcode, Category, Description, MinQuantity };

        if (options.IncludePharmacyFields)
        {
            headers.Add(ScientificName);
            headers.Add(UsageInstructions);
        }

        if (options.IncludeWeightFields)
        {
            headers.Add(Weight);
            headers.Add(WeightUnit);
        }

        if (options.IncludeDiscountFields)
        {
            headers.Add(DiscountType);
            headers.Add(DiscountValue);
            headers.Add(DiscountExpiresAt);
        }

        if (options.IncludePricingFields)
        {
            headers.Add(SalePrice);
            headers.Add(PurchasePrice);
        }

        foreach (var field in options.CustomFields.OrderBy(f => f.Slot))
        {
            var header = string.IsNullOrWhiteSpace(field.Header) ? $"حقل {field.Slot}" : field.Header.Trim();
            if (!headers.Contains(header, StringComparer.Ordinal))
                headers.Add(header);
        }

        return headers;
    }

    public static object[] BuildSampleRow(ProductImportOptions? options = null)
    {
        options ??= new ProductImportOptions();
        var headers = BuildHeaders(options);
        var row = new object[headers.Count];
        for (var i = 0; i < headers.Count; i++)
        {
            row[i] = headers[i] switch
            {
                Name => "منتج 1",
                Barcode => "123456",
                Category => "عام",
                Description => "وصف مختصر",
                ScientificName => "",
                UsageInstructions => "",
                Weight => "1",
                WeightUnit => "كغ",
                DiscountType => "بدون",
                DiscountValue => "0",
                DiscountExpiresAt => "",
                SalePrice => "1000",
                PurchasePrice => "800",
                MinQuantity => "5",
                _ => ""
            };
        }

        return row;
    }
}
