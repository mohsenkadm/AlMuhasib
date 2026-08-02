using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesInvoiceViewModel
{
    private IFeatureFlagService? _featureFlags;
    private IProductUnitService? _productUnitService;
    private IProductBatchService? _productBatchService;
    private IProductSerialService? _productSerialService;
    private IProductPriceService? _productPriceService;
    private IProductSizeService? _productSizeService;
    private IProductColorService? _productColorService;

    private readonly List<string> _activeCustomFieldLabels = [];
    private string? _appliedIndustryTag;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCustomField1))]
    [NotifyPropertyChangedFor(nameof(ShowCustomField2))]
    private string _customField1Header = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCustomField1))]
    [NotifyPropertyChangedFor(nameof(ShowCustomField2))]
    private string _customField2Header = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCustomField1))]
    [NotifyPropertyChangedFor(nameof(ShowCustomField2))]
    private bool _marketTemplateFieldsEnabled;

    [ObservableProperty] private bool _showUnitsOfMeasure;
    [ObservableProperty] private bool _showMenuWeight;
    [ObservableProperty] private bool _showProductDiscount;
    [ObservableProperty] private bool _showExpiryTracking;
    [ObservableProperty] private bool _showSerialNumbers;
    [ObservableProperty] private bool _showProductPricing;
    [ObservableProperty] private bool _showClothingSizes;
    [ObservableProperty] private bool _showTransportFee;
    [ObservableProperty] private bool _showDriverSelection;
    [ObservableProperty] private bool _showPharmacyUsage;
    [ObservableProperty] private decimal _transportFeeAmount;

    public ObservableCollection<Driver> Drivers { get; } = [];

    [ObservableProperty] private Driver? _selectedDriver;

    public bool ShowCustomField1 =>
        (MarketTemplateFieldsEnabled && !string.IsNullOrWhiteSpace(CustomField1Header))
        || (ShowClothingSizes && !string.IsNullOrWhiteSpace(CustomField1Header));

    public bool ShowCustomField2 =>
        (MarketTemplateFieldsEnabled && !string.IsNullOrWhiteSpace(CustomField2Header))
        || (ShowClothingSizes && !string.IsNullOrWhiteSpace(CustomField2Header));

    public IReadOnlyList<string> ActiveCustomFieldLabels => _activeCustomFieldLabels;

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
        ShowMenuWeight = _featureFlags.MenuWeight;
        ShowProductDiscount = _featureFlags.ProductDiscountEnabled;
        ShowExpiryTracking = _featureFlags.ExpiryTracking;
        ShowSerialNumbers = _featureFlags.SerialNumbers;
        ShowProductPricing = _featureFlags.ProductPricingEnabled;
        ShowClothingSizes = _featureFlags.TemplateClothing;
        ShowTransportFee = _featureFlags.TransportFees;
        ShowDriverSelection = _featureFlags.WarehouseInvoiceAndDriver;
        ShowPharmacyUsage = _featureFlags.TemplatePharmacy;

        foreach (var row in Items)
        {
            row.ProductDiscountFeatureEnabled = ShowProductDiscount;
            row.RefreshProductDiscount();
        }

        if (!ShowProductDiscount)
        {
            InvoiceDiscountType = DiscountType.None;
            InvoiceDiscountValue = 0m;
            InvoiceDiscountAmount = 0m;
        }

        if (!ShowTransportFee)
            TransportFeeAmount = 0m;

        if (!ShowDriverSelection)
            SelectedDriver = null;

        RecalculateTotals();

        var industryStillEnabled = IsIndustryEnabled(_appliedIndustryTag);
        MarketTemplateFieldsEnabled = _featureFlags.AnyMarketTemplateEnabled && industryStillEnabled;

        if (ShowClothingSizes && string.IsNullOrWhiteSpace(CustomField1Header))
            ApplyCustomFieldLabels([ClothingSizeInvoiceHelper.SizeLabel, ClothingSizeInvoiceHelper.ColorLabel], "clothing");
        else if (!MarketTemplateFieldsEnabled && !ShowClothingSizes)
            ClearCustomFieldLabels();
        else
        {
            OnPropertyChanged(nameof(ShowCustomField1));
            OnPropertyChanged(nameof(ShowCustomField2));
        }

        if (!ShowUnitsOfMeasure)
            ClearRowUnits();
        if (!ShowExpiryTracking)
            ClearRowBatches();
        if (!ShowSerialNumbers)
            ClearRowSerials();
        if (!ShowProductPricing)
            ClearRowPricing();
        if (!ShowClothingSizes)
            ClearRowSizes();

        InvoiceWeightSummaryText = InvoiceWeightHelper.BuildSummaryText(Items);
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

    private void ClearRowSizes()
    {
        foreach (var row in Items)
        {
            row.ProductSizeId = null;
            row.SizeName = string.Empty;
            row.ProductColorId = null;
            row.ColorName = string.Empty;
            row.SelectedColor = null;
            row.AvailableColors.Clear();
            if (string.Equals(row.CustomField2Label, ClothingSizeInvoiceHelper.ColorLabel, StringComparison.Ordinal))
                row.CustomField2 = string.Empty;
        }
    }

    private bool IsIndustryEnabled(string? industryTag) => industryTag switch
    {
        null or "" => false,
        "mobile" => _featureFlags?.TemplateMobileShop == true,
        "clothing" => _featureFlags?.TemplateClothing == true,
        "construction" => _featureFlags?.TemplateConstruction == true,
        "pharmacy" => _featureFlags?.TemplatePharmacy == true,
        _ => false
    };

    private void ClearCustomFieldLabels()
    {
        _activeCustomFieldLabels.Clear();
        _appliedIndustryTag = null;
        CustomField1Header = string.Empty;
        CustomField2Header = string.Empty;
        foreach (var row in Items)
        {
            row.CustomField1Label = string.Empty;
            row.CustomField2Label = string.Empty;
        }

        OnPropertyChanged(nameof(ShowCustomField1));
        OnPropertyChanged(nameof(ShowCustomField2));
    }

    private void ClearRowUnits()
    {
        foreach (var row in Items)
        {
            row.SelectedUnit = null;
            row.AvailableUnits.Clear();
            row.SelectedUnitName = string.Empty;
            row.UnitConversionFactor = 1m;
        }
    }

    private void ClearRowBatches()
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

    private void ClearRowSerials()
    {
        foreach (var row in Items)
            row.SerialNumber = string.Empty;
    }

    private void ApplyCustomFieldLabels(IEnumerable<string>? labels, string? industryTag = null)
    {
        if (!string.IsNullOrWhiteSpace(industryTag))
            _appliedIndustryTag = industryTag;

        _activeCustomFieldLabels.Clear();
        if (labels is not null)
            _activeCustomFieldLabels.AddRange(labels.Where(l => !string.IsNullOrWhiteSpace(l)).Take(2));

        CustomField1Header = _activeCustomFieldLabels.ElementAtOrDefault(0) ?? string.Empty;
        CustomField2Header = _activeCustomFieldLabels.ElementAtOrDefault(1) ?? string.Empty;

        MarketTemplateFieldsEnabled = ShowClothingSizes
            || _featureFlags is null
            || (IsIndustryEnabled(_appliedIndustryTag) && _featureFlags.AnyMarketTemplateEnabled);

        foreach (var row in Items)
            ApplyActiveLabelsToRow(row);

        OnPropertyChanged(nameof(ShowCustomField1));
        OnPropertyChanged(nameof(ShowCustomField2));
    }

    private void RestoreCustomFieldHeadersFromJson(string json)
    {
        var labels = InvoiceCustomFieldsHelper.ExtractPublicLabels(json);
        if (labels.Count == 0) return;
        ApplyCustomFieldLabels(labels, _appliedIndustryTag);
    }

    private void ApplyActiveLabelsToRow(InvoiceItemRow row)
    {
        row.CustomField1Label = CustomField1Header;
        row.CustomField2Label = CustomField2Header;
        if (!string.IsNullOrWhiteSpace(row.SizeName) && string.IsNullOrWhiteSpace(row.CustomField1))
            row.CustomField1 = row.SizeName;
        if (!string.IsNullOrWhiteSpace(row.ColorName) && string.IsNullOrWhiteSpace(row.CustomField2))
            row.CustomField2 = row.ColorName;
    }

    private async Task LoadRowFeatureDataAsync(InvoiceItemRow row)
    {
        if (row.ProductId is not int productId || productId <= 0)
            return;

        if (ShowUnitsOfMeasure && _productUnitService is not null)
        {
            var units = await _productUnitService.GetByProductAsync(productId);
            row.AvailableUnits.Clear();
            foreach (var u in units)
                row.AvailableUnits.Add(u);
            row.SelectedUnit ??= units.FirstOrDefault(u => u.IsDefault) ?? units.FirstOrDefault();
        }
        else
        {
            row.AvailableUnits.Clear();
            row.SelectedUnit = null;
            row.UnitConversionFactor = 1m;
        }

        if (ShowExpiryTracking && _productBatchService is not null && SelectedWarehouse is not null)
        {
            var batches = await _productBatchService.GetByProductAsync(productId, SelectedWarehouse.Id, inStockOnly: true);
            row.AvailableBatches.Clear();
            foreach (var b in batches)
                row.AvailableBatches.Add(b);
            if (row.BatchId is int existingBatchId)
                row.SelectedBatch = batches.FirstOrDefault(b => b.Id == existingBatchId) ?? row.SelectedBatch;
            if (row.SelectedBatch is null && batches.Count > 0)
                row.SelectedBatch = batches[0];
        }
        else
        {
            row.AvailableBatches.Clear();
            row.SelectedBatch = null;
        }

        if (ShowSerialNumbers && _productSerialService is not null)
        {
            var available = await _productSerialService.GetAvailableAsync(productId, SelectedWarehouse?.Id);
            if (string.IsNullOrWhiteSpace(row.SerialNumber) && available.Count == 1)
                row.SerialNumber = available[0].SerialNumber;
        }
        else
        {
            row.SerialNumber = string.Empty;
        }

        await LoadRowColorsAsync(row, productId);
        await LoadRowPricingOptionsAsync(row, productId);
    }

    private async Task LoadRowColorsAsync(InvoiceItemRow row, int productId)
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

    private async Task LoadRowPricingOptionsAsync(InvoiceItemRow row, int productId)
    {
        if (!ShowProductPricing || _productPriceService is null)
        {
            row.AvailablePricingOptions.Clear();
            row.SetSelectedPricingOptionWithoutPrice(null);
            return;
        }

        var prices = await _productPriceService.GetByProductIdAsync(productId);
        var options = prices
            .Select(p => new ProductPricingOption
            {
                PricingTypeId = p.PricingTypeId,
                Name = p.PricingType?.Name ?? $"نوع {p.PricingTypeId}",
                Price = p.SalePrice,
                IsDefault = p.PricingType?.IsDefault == true
            })
            .ToList();

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

        var preferred = options.FirstOrDefault(o => o.PricingTypeId == row.PricingTypeId)
                        ?? options.FirstOrDefault(o => o.IsDefault)
                        ?? options[0];

        var keepExistingPrice = row.PricingTypeId == preferred.PricingTypeId && row.UnitPrice > 0;
        if (keepExistingPrice)
            row.SetSelectedPricingOptionWithoutPrice(preferred);
        else
            row.SelectedPricingOption = preferred;
    }

    private async Task<bool> TryPromptClothingSizesAsync(
        Product product,
        decimal unitPrice,
        int? pricingTypeId,
        string? pricingTypeName,
        InvoiceItemRow? replaceRow = null,
        IReadOnlyDictionary<int, decimal>? seedQuantities = null)
    {
        if (!ShowClothingSizes || _productSizeService is null)
            return false;

        if (!await _productSizeService.HasSizesAsync(product.Id))
            return false;

        var selection = await ClothingSizeInvoiceHelper.PromptAsync(
            _productSizeService,
            product,
            SelectedWarehouse?.Id,
            isSale: true,
            unitPrice,
            pricingTypeId,
            pricingTypeName,
            seedQuantities);

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
            ApplyActiveLabelsToRow,
            replaceRow);

        foreach (var row in Items.Where(i => i.ProductId == product.Id && i.ProductSizeId is not null))
            await LoadRowFeatureDataAsync(row);

        RecalculateTotals();
        return true;
    }

    private async Task ApplyFeatureSideEffectsOnSaveAsync(IReadOnlyList<InvoiceItemRow> rows, IReadOnlyList<Core.Entities.InvoiceItem> savedItems)
    {
        if (_featureFlags is null) return;

        for (var i = 0; i < rows.Count && i < savedItems.Count; i++)
        {
            var row = rows[i];
            var item = savedItems[i];
            if (item.ProductId is not int productId) continue;

            if (_featureFlags.ExpiryTracking && _productBatchService is not null && SelectedWarehouse is not null)
            {
                var stockQty = Math.Abs(InvoiceCustomFieldsHelper.ToStockQuantity(row));
                if (stockQty <= 0)
                    continue;

                if (IsReturnMode)
                    continue;

                if (row.BatchId is int batchId)
                {
                    var selected = row.AvailableBatches.FirstOrDefault(b => b.Id == batchId)
                                   ?? (await _productBatchService.GetByProductAsync(productId, SelectedWarehouse.Id, inStockOnly: true))
                                       .FirstOrDefault(b => b.Id == batchId);
                    if (selected is not null && selected.Quantity >= stockQty)
                    {
                        await _productBatchService.DeductAsync(batchId, stockQty);
                        continue;
                    }
                }

                var allocations = await _productBatchService.AllocateFefoAsync(
                    productId, SelectedWarehouse.Id, stockQty);
                await _productBatchService.DeductAllocationsAsync(allocations);

                var primary = allocations.FirstOrDefault();
                if (primary is not null && row.BatchId is null)
                {
                    row.BatchId = primary.BatchId;
                    var batch = (await _productBatchService.GetByProductAsync(productId, SelectedWarehouse.Id))
                        .FirstOrDefault(b => b.Id == primary.BatchId);
                    if (batch is not null)
                    {
                        row.BatchNumber = batch.BatchNumber ?? string.Empty;
                        row.ExpiryDate = batch.ExpiryDate;
                    }
                }
            }

            if (_featureFlags.SerialNumbers && _productSerialService is not null
                && !string.IsNullOrWhiteSpace(row.SerialNumber) && !IsReturnMode)
            {
                await _productSerialService.MarkSoldAsync(row.SerialNumber, productId, item.Id);
            }

            if (ShowClothingSizes && _productSizeService is not null
                && row.ProductSizeId is int sizeId
                && SelectedWarehouse is not null
                && !IsReturnMode)
            {
                var stockQty = Math.Abs(InvoiceCustomFieldsHelper.ToStockQuantity(row));
                if (stockQty > 0)
                    await _productSizeService.DeductStockAsync(productId, sizeId, SelectedWarehouse.Id, stockQty);
            }
        }
    }
}
