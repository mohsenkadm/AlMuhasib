using System.Collections.ObjectModel;
using System.Text.Json;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class PosCartLine : ObservableObject
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public int ProductId { get; init; }
    public int? PricingTypeId { get; set; }
    public string ProductName { get; init; } = string.Empty;
    public Product? SourceProduct { get; init; }

    [ObservableProperty] private decimal _quantity = 1m;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _lineTotal;

    public bool ProductDiscountFeatureEnabled { get; set; }

    /// <summary>سطر هدية من عرض منتجات.</summary>
    public bool IsOfferGift { get; set; }
    public int? OfferId { get; set; }

    // Clothing
    public int? ProductSizeId { get; set; }
    [ObservableProperty] private string _sizeName = string.Empty;
    public int? ProductColorId { get; set; }
    [ObservableProperty] private string _colorName = string.Empty;
    public ObservableCollection<ProductColor> AvailableColors { get; } = [];
    [ObservableProperty] private ProductColor? _selectedColor;

    // Custom / template fields
    [ObservableProperty] private string _customField1Label = string.Empty;
    [ObservableProperty] private string _customField2Label = string.Empty;
    [ObservableProperty] private string _customField1 = string.Empty;
    [ObservableProperty] private string _customField2 = string.Empty;

    // Batch / expiry
    public ObservableCollection<ProductBatch> AvailableBatches { get; } = [];
    [ObservableProperty] private ProductBatch? _selectedBatch;
    public int? BatchId { get; set; }
    [ObservableProperty] private string _batchNumber = string.Empty;
    [ObservableProperty] private DateTime? _expiryDate;

    // Serial
    [ObservableProperty] private string _serialNumber = string.Empty;
    public ObservableCollection<string> AvailableSerials { get; } = [];

    // Pricing options
    public ObservableCollection<ProductPricingOption> AvailablePricingOptions { get; } = [];
    [ObservableProperty] private ProductPricingOption? _selectedPricingOption;
    [ObservableProperty] private string _pricingTypeName = string.Empty;

    public string? UsageInstructions => SourceProduct?.UsageInstructions;

    public string DisplayName
    {
        get
        {
            var name = IsOfferGift ? $"هدية — {ProductName}" : ProductName;
            var attrs = new List<string>(2);
            if (!string.IsNullOrWhiteSpace(SizeName)) attrs.Add(SizeName);
            if (!string.IsNullOrWhiteSpace(ColorName)) attrs.Add(ColorName);
            if (attrs.Count > 0)
                name = $"{name} — {string.Join(" — ", attrs)}";

            var extras = new List<string>(4);
            if (!string.IsNullOrWhiteSpace(BatchNumber)) extras.Add($"دفعة: {BatchNumber}");
            if (ExpiryDate.HasValue) extras.Add($"انتهاء: {ExpiryDate:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(SerialNumber)) extras.Add($"سيريال: {SerialNumber}");
            if (!string.IsNullOrWhiteSpace(CustomField1) && string.IsNullOrWhiteSpace(SizeName))
                extras.Add(CustomField1);
            if (!string.IsNullOrWhiteSpace(CustomField2) && string.IsNullOrWhiteSpace(ColorName))
                extras.Add(CustomField2);

            return extras.Count == 0 ? name : $"{name} | {string.Join(" | ", extras)}";
        }
    }

    partial void OnQuantityChanged(decimal value)
    {
        if (IsOfferGift)
        {
            DiscountAmount = 0m;
            LineTotal = 0m;
            return;
        }

        RefreshProductDiscount();
        Recalc();
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        if (IsOfferGift)
        {
            if (value != 0m)
                UnitPrice = 0m;
            DiscountAmount = 0m;
            LineTotal = 0m;
            return;
        }

        RefreshProductDiscount();
        Recalc();
    }

    partial void OnDiscountAmountChanged(decimal value) => Recalc();

    partial void OnSelectedColorChanged(ProductColor? value)
    {
        if (value is null) return;
        ProductColorId = value.Id;
        ColorName = value.ColorName;
        CustomField2 = value.ColorName;
        if (string.IsNullOrWhiteSpace(CustomField2Label))
            CustomField2Label = ClothingSizeInvoiceHelper.ColorLabel;
        OnPropertyChanged(nameof(DisplayName));
    }

    partial void OnSelectedBatchChanged(ProductBatch? value)
    {
        if (value is null)
        {
            BatchId = null;
            BatchNumber = string.Empty;
            ExpiryDate = null;
        }
        else
        {
            BatchId = value.Id;
            BatchNumber = value.BatchNumber ?? string.Empty;
            ExpiryDate = value.ExpiryDate;
        }
        OnPropertyChanged(nameof(DisplayName));
    }

    partial void OnSelectedPricingOptionChanged(ProductPricingOption? value)
    {
        if (value is null) return;
        PricingTypeId = value.PricingTypeId;
        PricingTypeName = value.Name;
        UnitPrice = value.Price;
    }

    partial void OnSizeNameChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnColorNameChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnBatchNumberChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnSerialNumberChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnCustomField1Changed(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnCustomField2Changed(string value) => OnPropertyChanged(nameof(DisplayName));

    public void RefreshProductDiscount()
    {
        if (!ProductDiscountFeatureEnabled || SourceProduct is null)
        {
            DiscountAmount = 0m;
            return;
        }

        DiscountAmount = ProductDiscountHelper.CalculateLineDiscount(SourceProduct, Quantity, UnitPrice);
    }

    private void Recalc() =>
        LineTotal = ProductDiscountHelper.CalculateLineTotal(Quantity, UnitPrice, DiscountAmount);

    public string? ToCustomFieldsJson()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(CustomField1Label) && !string.IsNullOrWhiteSpace(CustomField1))
            dict[CustomField1Label] = CustomField1.Trim();
        if (!string.IsNullOrWhiteSpace(CustomField2Label) && !string.IsNullOrWhiteSpace(CustomField2))
            dict[CustomField2Label] = CustomField2.Trim();

        if (!string.IsNullOrWhiteSpace(BatchNumber))
            dict["__batch"] = BatchNumber.Trim();
        if (ExpiryDate.HasValue)
            dict["__expiry"] = ExpiryDate.Value.ToString("yyyy-MM-dd");
        if (BatchId.HasValue)
            dict["__batchId"] = BatchId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(SerialNumber))
            dict["__serial"] = SerialNumber.Trim();
        if (ProductSizeId.HasValue)
            dict["__sizeId"] = ProductSizeId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(SizeName))
            dict["__size"] = SizeName.Trim();
        if (ProductColorId.HasValue)
            dict["__colorId"] = ProductColorId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(ColorName))
            dict["__color"] = ColorName.Trim();

        return dict.Count == 0 ? null : JsonSerializer.Serialize(dict, JsonOptions);
    }

    public static PosCartLine FromProduct(
        Product product,
        decimal unitPrice,
        decimal quantity = 1m,
        int? pricingTypeId = null,
        bool productDiscountEnabled = false,
        int? productSizeId = null,
        string? sizeName = null,
        int? productColorId = null,
        string? colorName = null)
    {
        var line = new PosCartLine
        {
            ProductId = product.Id,
            PricingTypeId = pricingTypeId,
            ProductName = product.Name,
            SourceProduct = product,
            ProductDiscountFeatureEnabled = productDiscountEnabled,
            Quantity = quantity,
            UnitPrice = unitPrice,
            ProductSizeId = productSizeId,
            SizeName = sizeName ?? string.Empty,
            ProductColorId = productColorId,
            ColorName = colorName ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(sizeName))
        {
            line.CustomField1 = sizeName;
            line.CustomField1Label = ClothingSizeInvoiceHelper.SizeLabel;
        }

        if (!string.IsNullOrWhiteSpace(colorName))
        {
            line.CustomField2 = colorName;
            line.CustomField2Label = ClothingSizeInvoiceHelper.ColorLabel;
        }

        line.RefreshProductDiscount();
        line.Recalc();
        return line;
    }
}
