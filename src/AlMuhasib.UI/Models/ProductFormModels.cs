using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>صف سعر لنوع تسعير داخل نموذج المنتج.</summary>
public partial class ProductFormPriceRow : ObservableObject
{
    public int? ProductPriceId { get; set; }
    public int PricingTypeId { get; init; }
    public string PricingTypeName { get; init; } = string.Empty;
    public bool IsDefault { get; init; }

    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _purchasePrice;
    [ObservableProperty] private decimal _salePriceUsd;
    [ObservableProperty] private decimal _purchasePriceUsd;
    [ObservableProperty] private bool _showUsd;

    public decimal EstimatedProfit => SalePrice - PurchasePrice;
    public decimal EstimatedProfitUsd => SalePriceUsd - PurchasePriceUsd;

    public string EstimatedProfitText =>
        ShowUsd
            ? $"{EstimatedProfit:N0} د.ع · {EstimatedProfitUsd:N2} $"
            : $"{EstimatedProfit:N0} د.ع";

    partial void OnSalePriceChanged(decimal value) => NotifyProfit();
    partial void OnPurchasePriceChanged(decimal value) => NotifyProfit();
    partial void OnSalePriceUsdChanged(decimal value) => NotifyProfit();
    partial void OnPurchasePriceUsdChanged(decimal value) => NotifyProfit();
    partial void OnShowUsdChanged(bool value) => NotifyProfit();

    private void NotifyProfit()
    {
        OnPropertyChanged(nameof(EstimatedProfit));
        OnPropertyChanged(nameof(EstimatedProfitUsd));
        OnPropertyChanged(nameof(EstimatedProfitText));
    }
}

/// <summary>صف كمية مخزن داخل نموذج المنتج.</summary>
public partial class ProductFormStockRow : ObservableObject
{
    public int WarehouseId { get; init; }
    public int BranchId { get; init; }
    public int? WarehouseStockId { get; set; }

    [ObservableProperty] private string _warehouseName = string.Empty;
    [ObservableProperty] private string _branchName = string.Empty;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _minQuantity;

    public string DisplayName => string.IsNullOrWhiteSpace(BranchName)
        ? WarehouseName
        : $"{WarehouseName} · {BranchName}";
}

/// <summary>صف تعبئة معلّق قبل الحفظ.</summary>
public partial class ProductFormUnitRow : ObservableObject
{
    public int? ProductUnitId { get; set; }
    public int? PackagingTypeId { get; set; }

    [ObservableProperty] private string _unitName = string.Empty;
    [ObservableProperty] private decimal _conversionFactor = 1m;
    [ObservableProperty] private bool _isDefault;
}

/// <summary>خيار فرع لظهور المنتج.</summary>
public partial class ProductBranchOption : ObservableObject
{
    public int BranchId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsMain { get; init; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Code) ? Name : $"{Name} ({Code})";

    public string TypeLabel => IsMain ? "رئيسي" : "فرعي";

    public event Action? SelectionChanged;

    private bool _suppress;

    [ObservableProperty] private bool _isSelected;

    public void SetSelectedSilent(bool value)
    {
        _suppress = true;
        IsSelected = value;
        _suppress = false;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        if (!_suppress)
            SelectionChanged?.Invoke();
    }
}
