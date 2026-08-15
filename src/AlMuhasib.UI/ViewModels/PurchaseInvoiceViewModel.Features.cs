using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels;

public partial class PurchaseInvoiceViewModel
{
    private IFeatureFlagService? _featureFlags;
    private IProductUnitService? _productUnitService;
    private IProductBatchService? _productBatchService;
    private IProductSerialService? _productSerialService;
    private IProductSizeService? _productSizeService;
    private IProductColorService? _productColorService;
    private IPricingTypeService? _pricingTypeService;
    private bool _suppressBulkPricingApply;

    [ObservableProperty] private bool _isReturnMode;
    [ObservableProperty] private bool _showUnitsOfMeasure;
    [ObservableProperty] private bool _showExpiryTracking;
    [ObservableProperty] private bool _showSerialNumbers;
    [ObservableProperty] private bool _showClothingSizes;
    [ObservableProperty] private bool _showProductPricing;
    [ObservableProperty] private bool _showTransportFee;
    [ObservableProperty] private decimal _transportFeeAmount;
    [ObservableProperty] private string _clothingSizeHeader = ClothingSizeInvoiceHelper.SizeLabel;
    [ObservableProperty] private string _clothingColorHeader = ClothingSizeInvoiceHelper.ColorLabel;
    [ObservableProperty] private PricingType? _selectedBulkPricingType;

    public ObservableCollection<PricingType> BulkPricingTypes { get; } = [];

    public bool ShowCustomField1 => ShowClothingSizes;
    public bool ShowCustomField2 => ShowClothingSizes;

    public void ConfigureFeatureServices(
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService,
        IProductBatchService productBatchService,
        IProductSerialService productSerialService,
        IProductSizeService productSizeService,
        IProductColorService productColorService)
    {
        if (_featureFlags is not null)
            _featureFlags.FlagsChanged -= OnFeatureFlagsChanged;

        _featureFlags = featureFlags;
        _productUnitService = productUnitService;
        _productBatchService = productBatchService;
        _productSerialService = productSerialService;
        _productSizeService = productSizeService;
        _productColorService = productColorService;
        RefreshFeatureVisibility();
        featureFlags.FlagsChanged += OnFeatureFlagsChanged;
    }

    private void OnFeatureFlagsChanged(object? sender, EventArgs e) =>
        FeatureUiRefresh.Invoke(RefreshFeatureVisibility);

    private void RefreshFeatureVisibility()
    {
        if (_featureFlags is null) return;

        ShowUnitsOfMeasure = _featureFlags.UnitsOfMeasure;
        ShowExpiryTracking = _featureFlags.ExpiryTracking;
        ShowSerialNumbers = _featureFlags.SerialNumbers;
        ShowClothingSizes = _featureFlags.TemplateClothing;
        ShowProductPricing = _featureFlags.ProductPricingEnabled;
        ShowTransportFee = _featureFlags.TransportFees;
        ClothingSizeHeader = ClothingSizeInvoiceHelper.SizeLabel;
        ClothingColorHeader = ClothingSizeInvoiceHelper.ColorLabel;
        OnPropertyChanged(nameof(ShowCustomField1));
        OnPropertyChanged(nameof(ShowCustomField2));

        if (!ShowTransportFee)
            TransportFeeAmount = 0m;

        if (!ShowUnitsOfMeasure)
        {
            foreach (var row in Items)
            {
                row.SelectedUnit = null;
                row.AvailableUnits.Clear();
                row.SelectedUnitName = string.Empty;
                row.UnitConversionFactor = 1m;
            }
        }

        if (!ShowExpiryTracking)
        {
            foreach (var row in Items)
            {
                row.SelectedBatch = null;
                row.AvailableBatches.Clear();
                row.BatchId = null;
                row.BatchNumber = string.Empty;
                row.ExpiryDate = null;
            }
        }

        if (!ShowSerialNumbers)
        {
            foreach (var row in Items)
                row.SerialNumber = string.Empty;
        }

        if (!ShowProductPricing)
        {
            ClearRowPricing();
            _suppressBulkPricingApply = true;
            SelectedBulkPricingType = null;
            _suppressBulkPricingApply = false;
            BulkPricingTypes.Clear();
        }
        else
        {
            _ = EnsureBulkPricingTypesLoadedAsync();
        }

        if (!ShowClothingSizes)
        {
            foreach (var row in Items)
            {
                row.ProductSizeId = null;
                row.SizeName = string.Empty;
                row.CustomField1 = string.Empty;
                row.CustomField1Label = string.Empty;
                row.ProductColorId = null;
                row.ColorName = string.Empty;
                row.SelectedColor = null;
                row.AvailableColors.Clear();
                row.CustomField2 = string.Empty;
                row.CustomField2Label = string.Empty;
            }
        }
        else
        {
            foreach (var row in Items)
            {
                row.CustomField1Label = ClothingSizeHeader;
                row.CustomField2Label = ClothingColorHeader;
                if (!string.IsNullOrWhiteSpace(row.SizeName))
                    row.CustomField1 = row.SizeName;
                if (!string.IsNullOrWhiteSpace(row.ColorName))
                    row.CustomField2 = row.ColorName;
            }
        }

        if (IsReturnMode && !_featureFlags.PurchaseReturns)
        {
            IsReturnMode = false;
            PageTitle = "فاتورة مشتريات";
        }

        RecalculateTotals();
    }

    private void ClearRowPricing()
    {
        foreach (var row in Items)
        {
            row.AvailablePricingOptions.Clear();
            row.SetSelectedPricingOptionWithoutPrice(null);
            row.PricingTypeId = null;
            row.PricingTypeName = string.Empty;
        }
    }

    partial void OnSelectedBulkPricingTypeChanged(PricingType? value)
    {
        if (_suppressBulkPricingApply || value is null || !ShowProductPricing)
            return;

        InvoiceBulkPricingHelper.ApplyPricingTypeToRows(Items, value.Id);
        RecalculateTotals();
    }

    private async Task EnsureBulkPricingTypesLoadedAsync()
    {
        if (!ShowProductPricing || BulkPricingTypes.Count > 0)
            return;

        await InvoiceBulkPricingHelper.LoadBulkPricingTypesAsync(_pricingTypeService, BulkPricingTypes);
    }

    public void EnterReturnMode(string? reference = null)
    {
        IsReturnMode = true;
        PageTitle = "مرتجع مشتريات";
        if (!string.IsNullOrWhiteSpace(reference) && string.IsNullOrWhiteSpace(Notes))
            Notes = $"مرتجع مشتريات — مرجع {reference}";
    }

    private async Task LoadPurchaseRowFeatureDataAsync(InvoiceItemRow row)
    {
        if (row.ProductId is not int productId || productId <= 0)
            return;

        if (ShowUnitsOfMeasure && _productUnitService is not null)
        {
            var units = await _productUnitService.GetByProductAsync(productId);
            row.AvailableUnits.Clear();
            foreach (var u in units)
                row.AvailableUnits.Add(u);

            ProductUnit? matchedUnit = null;
            if (!string.IsNullOrWhiteSpace(row.SelectedUnitName))
            {
                matchedUnit = units.FirstOrDefault(u =>
                    string.Equals(u.UnitName, row.SelectedUnitName, StringComparison.OrdinalIgnoreCase));
            }

            if (matchedUnit is null && row.UnitConversionFactor > 0 && row.UnitConversionFactor != 1m)
            {
                matchedUnit = units.FirstOrDefault(u =>
                    u.ConversionFactor == row.UnitConversionFactor);
            }

            row.SelectedUnit = matchedUnit
                ?? row.SelectedUnit
                ?? units.FirstOrDefault(u => u.IsDefault)
                ?? units.FirstOrDefault();
        }
        else
        {
            row.AvailableUnits.Clear();
            row.SelectedUnit = null;
            row.UnitConversionFactor = 1m;
        }

        await LoadPurchaseRowColorsAsync(row, productId);
        await LoadRowPricingOptionsAsync(row, productId);
    }

    private async Task LoadRowPricingOptionsAsync(InvoiceItemRow row, int productId)
    {
        if (!ShowProductPricing)
        {
            row.AvailablePricingOptions.Clear();
            row.SetSelectedPricingOptionWithoutPrice(null);
            return;
        }

        var prices = await _productPriceService.GetByProductIdAsync(productId);
        var options = InvoiceBulkPricingHelper.ToOptions(prices, usePurchasePrice: true);

        row.AvailablePricingOptions.Clear();
        foreach (var option in options)
            row.AvailablePricingOptions.Add(option);

        if (options.Count == 0)
        {
            row.SetSelectedPricingOptionWithoutPrice(null);
            row.PricingTypeId = null;
            row.PricingTypeName = string.Empty;
            return;
        }

        var preferred = InvoiceBulkPricingHelper.ResolvePreferredOption(
            options,
            row.PricingTypeId,
            SelectedBulkPricingType?.Id);

        if (preferred is null)
            return;

        var keepExistingPrice = row.PricingTypeId == preferred.PricingTypeId && row.UnitPrice > 0;
        if (keepExistingPrice)
            row.SetSelectedPricingOptionWithoutPrice(preferred);
        else
            row.SelectedPricingOption = preferred;
    }

    private async Task LoadPurchaseRowColorsAsync(InvoiceItemRow row, int productId)
    {
        if (!ShowClothingSizes || _productColorService is null)
        {
            row.AvailableColors.Clear();
            row.SelectedColor = null;
            return;
        }

        var colors = await _productColorService.GetByProductAsync(productId);
        row.AvailableColors.Clear();
        foreach (var color in colors)
            row.AvailableColors.Add(color);

        if (colors.Count == 0)
        {
            row.SelectedColor = null;
            return;
        }

        var preferred = colors.FirstOrDefault(c => c.Id == row.ProductColorId)
                        ?? colors.FirstOrDefault(c =>
                            string.Equals(c.ColorName, row.ColorName, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(c.ColorName, row.CustomField2, StringComparison.OrdinalIgnoreCase));

        if (preferred is not null)
            row.SelectedColor = preferred;
        else if (row.SelectedColor is null && colors.Count == 1)
            row.SelectedColor = colors[0];
    }

    private async Task<bool> TryPromptClothingSizesAsync(
        Product product,
        decimal unitPrice,
        InvoiceItemRow? replaceRow = null)
    {
        if (!ShowClothingSizes || _productSizeService is null)
            return false;

        if (!await _productSizeService.HasSizesAsync(product.Id))
            return false;

        var selection = await ClothingSizeInvoiceHelper.PromptAsync(
            _productSizeService,
            product,
            SelectedWarehouse?.Id,
            isSale: false,
            unitPrice);

        if (selection is null)
        {
            if (replaceRow is not null)
            {
                UnwireItemRow(replaceRow);
                Items.Remove(replaceRow);
                InvoiceProductMergeHelper.TrimEmptyRows(Items, UnwireItemRow, WireItemRow);
                RecalculateTotals();
            }
            return true;
        }

        ClothingSizeInvoiceHelper.ApplySelectionToItems(
            selection,
            Items,
            WireItemRow,
            UnwireItemRow,
            row =>
            {
                row.CustomField1Label = ClothingSizeHeader;
                row.CustomField2Label = ClothingColorHeader;
                if (!string.IsNullOrWhiteSpace(row.SizeName))
                    row.CustomField1 = row.SizeName;
            },
            replaceRow);

        foreach (var row in Items.Where(i => i.ProductId == product.Id && i.ProductSizeId is not null))
            await LoadPurchaseRowFeatureDataAsync(row);

        RecalculateTotals();
        return true;
    }

    private async Task ApplyPurchaseFeatureSideEffectsAsync(IReadOnlyList<InvoiceItemRow> rows)
    {
        if (_featureFlags is null || SelectedWarehouse is null) return;

        foreach (var row in rows)
        {
            if (row.ProductId is not int productId) continue;
            var stockQty = Math.Abs(InvoiceCustomFieldsHelper.ToStockQuantity(row));

            if (_featureFlags.ExpiryTracking && _productBatchService is not null && !IsReturnMode)
            {
                await _productBatchService.UpsertAsync(
                    productId,
                    SelectedWarehouse.Id,
                    string.IsNullOrWhiteSpace(row.BatchNumber) ? null : row.BatchNumber,
                    row.ExpiryDate,
                    stockQty);
            }

            if (_featureFlags.ExpiryTracking && _productBatchService is not null && IsReturnMode && row.BatchId is int batchId)
            {
                await _productBatchService.DeductAsync(batchId, stockQty);
            }

            if (_featureFlags.SerialNumbers && _productSerialService is not null && !IsReturnMode
                && !string.IsNullOrWhiteSpace(row.SerialNumber))
            {
                var serials = row.SerialNumber
                    .Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                await _productSerialService.AddRangeAsync(productId, SelectedWarehouse.Id, serials);
            }

            if (ShowClothingSizes && _productSizeService is not null && row.ProductSizeId is int sizeId && stockQty > 0)
            {
                if (IsReturnMode)
                    await _productSizeService.DeductStockAsync(productId, sizeId, SelectedWarehouse.Id, stockQty);
                else
                    await _productSizeService.AdjustStockAsync(productId, sizeId, SelectedWarehouse.Id, stockQty);
            }
        }
    }
}
