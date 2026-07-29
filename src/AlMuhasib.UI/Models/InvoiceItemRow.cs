using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>
/// Observable row model for the invoice items DataGrid.
/// </summary>
public partial class InvoiceItemRow : ObservableObject
{
    [ObservableProperty]
    private int? _productId;

    [ObservableProperty]
    private int? _pricingTypeId;

    [ObservableProperty]
    private string _pricingTypeName = string.Empty;

    /// <summary>خيارات التسعير المتاحة للمنتج المحدد (عند تفعيل تسعير المنتجات).</summary>
    public ObservableCollection<ProductPricingOption> AvailablePricingOptions { get; } = [];

    [ObservableProperty]
    private ProductPricingOption? _selectedPricingOption;

    private bool _suppressPricingOptionApply;

    partial void OnSelectedPricingOptionChanged(ProductPricingOption? value)
    {
        if (_suppressPricingOptionApply)
            return;

        if (value is null)
        {
            PricingTypeId = null;
            PricingTypeName = string.Empty;
            return;
        }

        PricingTypeId = value.PricingTypeId;
        PricingTypeName = value.Name;
        UnitPrice = value.Price;
    }

    /// <summary>تعيين خيار التسعير دون إعادة كتابة سعر الوحدة (مثلاً عند استعادة سطر موجود).</summary>
    public void SetSelectedPricingOptionWithoutPrice(ProductPricingOption? option)
    {
        _suppressPricingOptionApply = true;
        SelectedPricingOption = option;
        if (option is not null)
        {
            PricingTypeId = option.PricingTypeId;
            PricingTypeName = option.Name;
        }
        _suppressPricingOptionApply = false;
    }

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1m;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _totalPrice;

    [ObservableProperty]
    private Product? _selectedProduct;

    /// <summary>معلومات الرصيد في المخازن</summary>
    [ObservableProperty]
    private string _stockInfo = string.Empty;

    /// <summary>إجمالي الرصيد المتاح في المخزن المحدد</summary>
    [ObservableProperty]
    private decimal _availableStock;

    // ── قوالب السوق — حقول مخصصة ──────────────────────────
    [ObservableProperty]
    private string _customField1Label = string.Empty;

    [ObservableProperty]
    private string _customField2Label = string.Empty;

    [ObservableProperty]
    private string _customField1 = string.Empty;

    [ObservableProperty]
    private string _customField2 = string.Empty;

    public bool HasCustomField1 => !string.IsNullOrWhiteSpace(CustomField1Label);
    public bool HasCustomField2 => !string.IsNullOrWhiteSpace(CustomField2Label);

    partial void OnCustomField1LabelChanged(string value) => OnPropertyChanged(nameof(HasCustomField1));
    partial void OnCustomField2LabelChanged(string value) => OnPropertyChanged(nameof(HasCustomField2));

    // ── وحدات القياس ──────────────────────────────────────
    public ObservableCollection<ProductUnit> AvailableUnits { get; } = [];

    [ObservableProperty]
    private ProductUnit? _selectedUnit;

    [ObservableProperty]
    private string _selectedUnitName = string.Empty;

    [ObservableProperty]
    private decimal _unitConversionFactor = 1m;

    partial void OnSelectedUnitChanged(ProductUnit? value)
    {
        if (value is null)
        {
            SelectedUnitName = string.Empty;
            UnitConversionFactor = 1m;
            return;
        }

        SelectedUnitName = value.UnitName;
        UnitConversionFactor = value.ConversionFactor <= 0 ? 1m : value.ConversionFactor;
    }

    // ── دفعات / صلاحية ────────────────────────────────────
    public ObservableCollection<ProductBatch> AvailableBatches { get; } = [];

    [ObservableProperty]
    private int? _batchId;

    [ObservableProperty]
    private string _batchNumber = string.Empty;

    [ObservableProperty]
    private DateTime? _expiryDate;

    [ObservableProperty]
    private ProductBatch? _selectedBatch;

    partial void OnSelectedBatchChanged(ProductBatch? value)
    {
        if (value is null)
        {
            BatchId = null;
            return;
        }

        BatchId = value.Id;
        BatchNumber = value.BatchNumber ?? string.Empty;
        ExpiryDate = value.ExpiryDate;
    }

    // ── سيريال ────────────────────────────────────────────
    [ObservableProperty]
    private string _serialNumber = string.Empty;

    private bool _isManualTotal;

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is not null)
        {
            ProductId = value.Id;
            ItemName = value.Name;
        }
        ProductChanged?.Invoke(this);
    }

    partial void OnQuantityChanged(decimal value)
    {
        _isManualTotal = false;
        RecalcTotal();
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        _isManualTotal = false;
        RecalcTotal();
    }

    partial void OnTotalPriceChanged(decimal oldValue, decimal newValue)
    {
        if (!_isRecalculating)
        {
            if (Quantity != 0)
            {
                _isRecalculating = true;
                UnitPrice = newValue / Quantity;
                _isRecalculating = false;
                _isManualTotal = false;
            }
            else
            {
                _isManualTotal = true;
            }
        }

        TotalChanged?.Invoke();
    }

    private bool _isRecalculating;

    private void RecalcTotal()
    {
        if (_isManualTotal) return;
        _isRecalculating = true;
        TotalPrice = Quantity * UnitPrice;
        _isRecalculating = false;
    }

    /// <summary>Event raised when TotalPrice changes so the parent VM can recalculate.</summary>
    public event Action? TotalChanged;

    /// <summary>Event raised when the selected product changes so the parent VM can load stock info.</summary>
    public event Action<InvoiceItemRow>? ProductChanged;
}
