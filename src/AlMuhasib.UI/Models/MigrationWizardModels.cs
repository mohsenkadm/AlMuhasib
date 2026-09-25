using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Models;

public enum MigrationStepKind
{
    Capital,
    CashAndBank,
    Investors,
    Warehouses,
    Categories,
    Products,
    PricingTypes,
    ProductPricing,
    Customers,
    Suppliers,
    ExpenseTypes,
    Installments
}

public partial class MigrationStepInfo : ObservableObject
{
    public MigrationStepKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ShortTitle { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public PackIconKind Icon { get; init; }
    public bool IsOptional { get; init; } = true;

    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isSaved;
}

public partial class MigrationNamedBalanceRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _fileNumber;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private decimal _profitPercentage;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _accountNumber;
    [ObservableProperty] private string _kind = "Cash"; // Cash | Bank
    [ObservableProperty] private string? _categoryName;
    [ObservableProperty] private string? _barcode;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitCost;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private string? _pricingTypeName;
    [ObservableProperty] private string? _productName;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _purchasePrice;
    [ObservableProperty] private int _numberOfInstallments = 1;
    [ObservableProperty] private int _paidInstallmentsCount;
    [ObservableProperty] private string? _errorText;
    [ObservableProperty] private bool _isValid = true;
}
