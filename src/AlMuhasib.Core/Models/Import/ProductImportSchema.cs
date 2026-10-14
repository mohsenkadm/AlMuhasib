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
    public const string VehicleType = "نوع السيارة";
    public const string ChassisNumber = "صنف السيارة";
    public const string CarModel = "موديل السيارة";
    public const string VehicleColor = "اللون";
    public const string PassengerCount = "عدد الركاب";
    public const string PlateNumber = "رقم اللوحة";
    public const string PlateType = "نوع اللوحة";

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

        if (options.IncludeCarShowroomFields)
        {
            headers.Add(VehicleType);
            headers.Add(ChassisNumber);
            headers.Add(CarModel);
            headers.Add(VehicleColor);
            headers.Add(PassengerCount);
            headers.Add(PlateNumber);
            headers.Add(PlateType);
        }

        foreach (var field in options.CustomFields.OrderBy(f => f.Slot))
        {
            var header = string.IsNullOrWhiteSpace(field.Header) ? $"حقل {field.Slot}" : field.Header.Trim();
            if (!headers.Contains(header, StringComparer.Ordinal))
                headers.Add(header);
        }

        if (options.IncludeBranchColumns)
        {
            foreach (var branch in options.Branches)
            {
                var header = string.IsNullOrWhiteSpace(branch.Header)
                    ? BuildBranchHeader(branch.BranchName)
                    : branch.Header.Trim();
                if (!headers.Contains(header, StringComparer.Ordinal))
                    headers.Add(header);
            }
        }

        return headers;
    }

    public static string BuildBranchHeader(string branchName)
    {
        var sanitized = SanitizeHeader(branchName);
        return string.IsNullOrEmpty(sanitized) ? "ظهور_فرع" : $"ظهور_فرع_{sanitized}";
    }

    public static string SanitizeHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var chars = value.Trim().Select(c => char.IsWhiteSpace(c) || c is '/' or '\\' or ':' or '*' or '?' or '[' or ']'
            ? '_'
            : c).ToArray();
        return new string(chars);
    }

    public static bool IsBranchVisibleCell(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var v = value.Trim();
        return v is "1" or "نعم" or "yes" or "true" or "y" or "✓" or "✔"
            || v.Equals("نعم", StringComparison.OrdinalIgnoreCase)
            || v.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public static object[] BuildSampleRow(ProductImportOptions? options = null)
    {
        options ??= new ProductImportOptions();
        var headers = BuildHeaders(options);
        var branchHeaders = options.IncludeBranchColumns
            ? options.Branches
                .Select(b => string.IsNullOrWhiteSpace(b.Header) ? BuildBranchHeader(b.BranchName) : b.Header.Trim())
                .ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        var row = new object[headers.Count];
        var firstBranchDone = false;
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            if (branchHeaders.Contains(header))
            {
                // عيّنة: نعم للفرع الأول، لا للبقية
                row[i] = firstBranchDone ? "لا" : "نعم";
                firstBranchDone = true;
                continue;
            }

            row[i] = header switch
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
                VehicleType => "سيدان",
                ChassisNumber => "",
                CarModel => "2024",
                VehicleColor => "أبيض",
                PassengerCount => "5",
                PlateNumber => "",
                PlateType => "رسمي",
                _ => ""
            };
        }

        return row;
    }
}
