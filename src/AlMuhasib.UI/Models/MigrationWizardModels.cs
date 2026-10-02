using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Models;

public enum MigrationStepKind
{
    /// <summary>الفروع + رأس المال لكل فرع + تفعيل العملات.</summary>
    Branches,
    Capital,
    Categories,
    Warehouses,
    PricingTypes,
    Products,
    CashAndBank,
    Investors,
    Customers,
    Suppliers,
    ExpenseTypes,
    Installments,
    /// <summary>متوافق مع خطوات قديمة — لم يعد يُعرض في المعالج.</summary>
    ProductPricing
}

public partial class MigrationStepInfo : ObservableObject
{
    public MigrationStepKind Kind { get; init; }
    public string Title { get; set; } = string.Empty;
    public string ShortTitle { get; init; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PackIconKind Icon { get; init; }
    public bool IsOptional { get; set; } = true;
    public int StepNumber { get; set; }

    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isSaved;
}

public partial class MigrationProductPriceCell : ObservableObject
{
    [ObservableProperty] private string _pricingTypeName = string.Empty;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _salePriceUsd;
    [ObservableProperty] private decimal _purchasePrice;
    [ObservableProperty] private decimal _purchasePriceUsd;
}

/// <summary>كمية افتتاحية لمخزن واحد داخل صف منتج المعالج.</summary>
public partial class MigrationWarehouseQtyCell : ObservableObject
{
    [ObservableProperty] private string _warehouseName = string.Empty;
    [ObservableProperty] private decimal _quantity;
}

/// <summary>محرر أسعار عند الإدخال اليدوي للمنتج (قبل الإضافة للجدول).</summary>
public partial class MigrationProductPriceEditor : ObservableObject
{
    [ObservableProperty] private string _pricingTypeName = string.Empty;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _salePriceUsd;
    [ObservableProperty] private decimal _purchasePrice;
    [ObservableProperty] private decimal _purchasePriceUsd;
}

public partial class MigrationNamedBalanceRow : ObservableObject
{
    /// <summary>معرّف السجل في قاعدة البيانات عند عرض مدخل موجود مسبقاً.</summary>
    [ObservableProperty] private int? _sourceId;
    [ObservableProperty] private bool _isExisting;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _fileNumber;
    [ObservableProperty] private decimal _amount;
    /// <summary>رصيد افتتاحي بالدولار عند تعدد العملات (مستقل عن Amount بالدينار).</summary>
    [ObservableProperty] private decimal _amountUsd;
    [ObservableProperty] private decimal _profitPercentage;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _accountNumber;
    [ObservableProperty] private string _kind = "Cash";
    [ObservableProperty] private string? _categoryName;
    [ObservableProperty] private string? _barcode;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitCost;
    [ObservableProperty] private decimal _unitCostUsd;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private string? _pricingTypeName;
    [ObservableProperty] private string? _productName;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _purchasePrice;
    [ObservableProperty] private int _numberOfInstallments = 1;
    [ObservableProperty] private int _paidInstallmentsCount;
    [ObservableProperty] private string? _errorText;
    [ObservableProperty] private bool _isValid = true;
    [ObservableProperty] private string? _warehouseName;
    [ObservableProperty] private string? _branchName;
    [ObservableProperty] private int? _branchId;
    [ObservableProperty] private AccountingCurrency _costCurrency = AccountingCurrency.IQD;
    [ObservableProperty] private decimal _fxRate = 1m;
    [ObservableProperty] private string _pricesSummary = string.Empty;

    // حقول المنتج حسب الميزات (معالج النقل)
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _scientificName;
    [ObservableProperty] private string? _usageInstructions;
    [ObservableProperty] private string? _vehicleType;
    [ObservableProperty] private string? _chassisNumber;
    [ObservableProperty] private string? _carModel;
    [ObservableProperty] private string? _vehicleColor;
    [ObservableProperty] private string? _passengerCountText;
    [ObservableProperty] private string? _plateNumber;
    [ObservableProperty] private string _plateTypeText = "بدون";
    [ObservableProperty] private decimal _weight;
    [ObservableProperty] private string _weightUnit = "كغ";
    [ObservableProperty] private string _discountTypeText = "بدون";
    [ObservableProperty] private decimal _discountValue;
    [ObservableProperty] private string? _discountExpiresText;

    public ObservableCollection<MigrationProductPriceCell> ProductPrices { get; } = [];
    public ObservableCollection<MigrationWarehouseQtyCell> WarehouseQtys { get; } = [];

    public DiscountType ParseDiscountType()
    {
        var value = DiscountTypeText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(value) || value is "بدون" or "لا" or "none" or "0")
            return DiscountType.None;
        if (value.Contains("نسب", StringComparison.OrdinalIgnoreCase) || value.Contains('%') || value == "1")
            return DiscountType.Percentage;
        if (value.Contains("ثابت", StringComparison.OrdinalIgnoreCase)
            || value.Contains("قيمة", StringComparison.OrdinalIgnoreCase)
            || value == "2")
            return DiscountType.FixedAmount;
        return DiscountType.None;
    }

    public DateTime? ParseDiscountExpiry()
    {
        var value = DiscountExpiresText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(value)) return null;
        if (DateTime.TryParse(value, out var dt))
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime()
                : dt.ToUniversalTime();
        return null;
    }

    public void EnsureProductStructure(
        IReadOnlyList<string> pricingTypeNames,
        IReadOnlyList<string> warehouseNames,
        bool includeUsdPrices)
    {
        // أسعار — حافظ على الترتيب المطابق لأنواع التسعير
        var priceByName = ProductPrices
            .GroupBy(p => p.PricingTypeName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        ProductPrices.Clear();
        foreach (var typeName in pricingTypeNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            if (priceByName.TryGetValue(typeName, out var existing))
                ProductPrices.Add(existing);
            else
                ProductPrices.Add(new MigrationProductPriceCell { PricingTypeName = typeName });
        }

        // كميات المخازن — بنفس ترتيب القائمة
        var qtyByName = WarehouseQtys
            .GroupBy(q => q.WarehouseName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        WarehouseQtys.Clear();
        foreach (var wh in warehouseNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            if (qtyByName.TryGetValue(wh, out var existing))
                WarehouseQtys.Add(existing);
            else
                WarehouseQtys.Add(new MigrationWarehouseQtyCell { WarehouseName = wh });
        }

        // ترحيل كمية عامة قديمة إلى أول مخزن
        if (Quantity > 0 && WarehouseQtys.Count > 0)
        {
            var target = !string.IsNullOrWhiteSpace(WarehouseName)
                ? WarehouseQtys.FirstOrDefault(q =>
                    string.Equals(q.WarehouseName, WarehouseName, StringComparison.OrdinalIgnoreCase))
                : null;
            target ??= WarehouseQtys[0];
            if (target.Quantity <= 0)
                target.Quantity = Quantity;
        }

        _ = includeUsdPrices;
        RebuildPricesSummary();
    }

    public void RebuildPricesSummary()
    {
        var parts = ProductPrices
            .Where(p => p.SalePrice > 0 || p.SalePriceUsd > 0 || p.PurchasePrice > 0)
            .Select(p =>
            {
                if (p.SalePriceUsd > 0 && p.SalePrice > 0)
                    return $"{p.PricingTypeName}: {p.SalePrice:N0}د/${p.SalePriceUsd:N0}";
                if (p.SalePriceUsd > 0)
                    return $"{p.PricingTypeName}: ${p.SalePriceUsd:N0}";
                return p.PurchasePrice > 0
                    ? $"{p.PricingTypeName}: {p.SalePrice:N0}/{p.PurchasePrice:N0}"
                    : $"{p.PricingTypeName}: {p.SalePrice:N0}";
            })
            .ToList();
        PricesSummary = parts.Count == 0 ? "—" : string.Join(" | ", parts);
        if (ProductPrices.Count > 0)
        {
            var first = ProductPrices.FirstOrDefault(p => p.SalePrice > 0 || p.SalePriceUsd > 0);
            if (first is not null)
            {
                UnitPrice = first.SalePrice > 0 ? first.SalePrice : first.SalePriceUsd;
                SalePrice = UnitPrice;
            }
        }
    }
}

/// <summary>صف فرع في معالج النقل مع رأس ماله الافتتاحي.</summary>
public partial class MigrationBranchCapitalRow : ObservableObject
{
    [ObservableProperty] private int _branchId;
    [ObservableProperty] private string _branchName = string.Empty;
    [ObservableProperty] private string _branchCode = string.Empty;
    [ObservableProperty] private bool _isMain;
    [ObservableProperty] private bool _isExisting = true;
    [ObservableProperty] private decimal _capitalAmount;
    [ObservableProperty] private decimal _capitalAmountUsd;
    [ObservableProperty] private decimal _profitOpeningBalance;
    [ObservableProperty] private decimal _profitOpeningBalanceUsd;
    [ObservableProperty] private bool _hasExistingCapital;
    [ObservableProperty] private bool _hasExistingProfit;
}
