using System.Collections.ObjectModel;
using AlMuhasib.Core;
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

    // ── وزن المادة (ميزة وزن القائمة) ───────────────────────
    [ObservableProperty]
    private decimal _productWeight;

    [ObservableProperty]
    private string _productWeightUnit = string.Empty;

    /// <summary>وزن السطر = وزن الوحدة × الكمية × معامل التحويل.</summary>
    public decimal LineWeight
    {
        get
        {
            if (ProductWeight <= 0) return 0m;
            var factor = UnitConversionFactor <= 0 ? 1m : UnitConversionFactor;
            return ProductWeight * Quantity * factor;
        }
    }

    partial void OnProductWeightChanged(decimal value)
    {
        OnPropertyChanged(nameof(LineWeight));
        TotalChanged?.Invoke();
    }

    partial void OnProductWeightUnitChanged(string value) => TotalChanged?.Invoke();

    partial void OnUnitConversionFactorChanged(decimal value)
    {
        OnPropertyChanged(nameof(LineWeight));
        _isManualTotal = false;
        RefreshProductDiscount();
        RecalcTotal();
        TotalChanged?.Invoke();
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

    // ── قياسات الألبسة ────────────────────────────────────
    [ObservableProperty]
    private int? _productSizeId;

    [ObservableProperty]
    private string _sizeName = string.Empty;

    // ── ألوان الألبسة ─────────────────────────────────────
    public ObservableCollection<ProductColor> AvailableColors { get; } = [];

    [ObservableProperty]
    private int? _productColorId;

    [ObservableProperty]
    private string _colorName = string.Empty;

    [ObservableProperty]
    private ProductColor? _selectedColor;

    partial void OnSelectedColorChanged(ProductColor? value)
    {
        if (value is null)
        {
            ProductColorId = null;
            return;
        }

        ProductColorId = value.Id;
        ColorName = value.ColorName;
        CustomField2 = value.ColorName;
        if (string.IsNullOrWhiteSpace(CustomField2Label))
            CustomField2Label = "اللون";
    }

    /// <summary>طريقة استخدام الدواء من بطاقة المنتج (قالب الصيدلية).</summary>
    [ObservableProperty]
    private string _usageInstructions = string.Empty;

    // ── خصم المنتج ─────────────────────────────────────────
    [ObservableProperty]
    private decimal _discountPercent;

    [ObservableProperty]
    private decimal _discountAmount;

    /// <summary>يُفعَّل من شاشة الفاتورة عند تفعيل ميزة الخصم.</summary>
    public bool ProductDiscountFeatureEnabled { get; set; }

    private bool _suppressDiscountRecalc;

    partial void OnDiscountPercentChanged(decimal value)
    {
        if (_isRecalculating || _suppressDiscountRecalc) return;
        _isManualTotal = false;
        ApplyDiscountFromPercent();
        RecalcTotal();
    }

    // ── مخزن السطر ─────────────────────────────────────────
    [ObservableProperty]
    private int? _warehouseId;

    [ObservableProperty]
    private Warehouse? _selectedWarehouse;

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        WarehouseId = value?.Id;
        WarehouseChanged?.Invoke(this);
    }

    /// <summary>معرّف المخزن الفعلي للسطر (سطر أو رأس الفاتورة).</summary>
    public int? ResolveWarehouseId(int? headerWarehouseId) =>
        WarehouseId ?? headerWarehouseId;

    /// <summary>يُستدعى عند تغيير المخزن العلوي لتطبيقه على السطر.</summary>
    public void ApplyHeaderWarehouse(Warehouse? warehouse)
    {
        SelectedWarehouse = warehouse;
        WarehouseId = warehouse?.Id;
    }

    public event Action<InvoiceItemRow>? WarehouseChanged;

    /// <summary>سطر هدية من عرض منتجات — سعر صفر ولا يفتح بحث منتج.</summary>
    [ObservableProperty]
    private bool _isOfferGift;

    [ObservableProperty]
    private int? _offerId;

    /// <summary>منع تعديل السعر على سطور الهدايا.</summary>
    public bool IsPriceEditable => !IsOfferGift;

    partial void OnIsOfferGiftChanged(bool value) => OnPropertyChanged(nameof(IsPriceEditable));

    private bool _isManualTotal;

    /// <summary>تعيين المنتج دون إطلاق ProductChanged (لاستعادة المسودة/الانتظار).</summary>
    public void AttachProductSilent(Product? product)
    {
        var handlers = ProductChanged;
        ProductChanged = null;
        try
        {
            SelectedProduct = product;
            if (product is not null)
                ProductId = product.Id;
        }
        finally
        {
            ProductChanged = handlers;
        }
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is not null)
        {
            ProductId = value.Id;
            ItemName = value.Name;
            ProductWeight = value.Weight;
            ProductWeightUnit = value.WeightUnit ?? string.Empty;
            UsageInstructions = value.UsageInstructions ?? string.Empty;
        }
        else
        {
            ProductWeight = 0m;
            ProductWeightUnit = string.Empty;
            UsageInstructions = string.Empty;
            DiscountAmount = 0m;
        }

        RefreshProductDiscount();
        ProductChanged?.Invoke(this);
    }

    partial void OnQuantityChanged(decimal value)
    {
        if (IsOfferGift)
        {
            _isManualTotal = false;
            OnPropertyChanged(nameof(LineWeight));
            RecalcTotal();
            return;
        }

        _isManualTotal = false;
        OnPropertyChanged(nameof(LineWeight));
        if (ProductDiscountFeatureEnabled && DiscountPercent > 0)
            ApplyDiscountFromPercent();
        else
            RefreshProductDiscount();
        RecalcTotal();
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        if (IsOfferGift)
        {
            if (value != 0m)
            {
                _isRecalculating = true;
                UnitPrice = 0m;
                _isRecalculating = false;
            }
            TotalPrice = 0m;
            return;
        }

        _isManualTotal = false;
        if (ProductDiscountFeatureEnabled && DiscountPercent > 0)
            ApplyDiscountFromPercent();
        else
            RefreshProductDiscount();
        RecalcTotal();
    }

    partial void OnDiscountAmountChanged(decimal value)
    {
        if (_isRecalculating || _suppressDiscountRecalc) return;
        _isManualTotal = false;
        var gross = Math.Abs(ProductDiscountHelper.ToBaseQuantity(Quantity, UnitConversionFactor) * UnitPrice);
        _suppressDiscountRecalc = true;
        DiscountPercent = ProductDiscountHelper.PercentFromDiscountAmount(gross, value);
        _suppressDiscountRecalc = false;
        RecalcTotal();
    }

    partial void OnTotalPriceChanged(decimal oldValue, decimal newValue)
    {
        if (!_isRecalculating)
        {
            var baseQty = ProductDiscountHelper.ToBaseQuantity(Quantity, UnitConversionFactor);
            if (baseQty != 0)
            {
                _isRecalculating = true;
                UnitPrice = (newValue + DiscountAmount) / baseQty;
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

    public void RefreshProductDiscount()
    {
        if (!ProductDiscountFeatureEnabled || SelectedProduct is null)
        {
            _suppressDiscountRecalc = true;
            if (DiscountPercent != 0m) DiscountPercent = 0m;
            if (DiscountAmount != 0m) DiscountAmount = 0m;
            _suppressDiscountRecalc = false;
            return;
        }

        var productPercent = ProductDiscountHelper.GetProductDiscountPercent(SelectedProduct);
        if (productPercent > 0m)
        {
            _suppressDiscountRecalc = true;
            DiscountPercent = productPercent;
            _suppressDiscountRecalc = false;
            ApplyDiscountFromPercent();
            return;
        }

        var discount = ProductDiscountHelper.CalculateLineDiscount(
            SelectedProduct, Quantity, UnitPrice, conversionFactor: UnitConversionFactor);
        var gross = Math.Abs(ProductDiscountHelper.ToBaseQuantity(Quantity, UnitConversionFactor) * UnitPrice);
        _suppressDiscountRecalc = true;
        DiscountPercent = ProductDiscountHelper.PercentFromDiscountAmount(gross, discount);
        DiscountAmount = discount;
        _suppressDiscountRecalc = false;
    }

    private void ApplyDiscountFromPercent()
    {
        if (!ProductDiscountFeatureEnabled)
        {
            DiscountAmount = 0m;
            return;
        }

        var gross = Math.Abs(ProductDiscountHelper.ToBaseQuantity(Quantity, UnitConversionFactor) * UnitPrice);
        var discount = ProductDiscountHelper.CalculateDiscountFromPercent(gross, DiscountPercent);
        _suppressDiscountRecalc = true;
        if (DiscountAmount != discount)
            DiscountAmount = discount;
        _suppressDiscountRecalc = false;
    }

    private void RecalcTotal()
    {
        if (IsOfferGift)
        {
            _isRecalculating = true;
            UnitPrice = 0m;
            DiscountPercent = 0m;
            DiscountAmount = 0m;
            TotalPrice = 0m;
            _isRecalculating = false;
            return;
        }

        if (_isManualTotal) return;
        _isRecalculating = true;
        TotalPrice = ProductDiscountHelper.CalculateLineTotal(
            Quantity, UnitPrice, DiscountAmount, UnitConversionFactor);
        _isRecalculating = false;
    }

    /// <summary>Event raised when TotalPrice changes so the parent VM can recalculate.</summary>
    public event Action? TotalChanged;

    /// <summary>Event raised when the selected product changes so the parent VM can load stock info.</summary>
    public event Action<InvoiceItemRow>? ProductChanged;
}
