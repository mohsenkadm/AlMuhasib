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
    [ObservableProperty] private bool _showExpiryTracking;
    [ObservableProperty] private bool _showSerialNumbers;
    [ObservableProperty] private bool _showProductPricing;

    public bool ShowCustomField1 =>
        MarketTemplateFieldsEnabled && !string.IsNullOrWhiteSpace(CustomField1Header);

    public bool ShowCustomField2 =>
        MarketTemplateFieldsEnabled && !string.IsNullOrWhiteSpace(CustomField2Header);

    public IReadOnlyList<string> ActiveCustomFieldLabels => _activeCustomFieldLabels;

    public void ConfigureFeatureServices(
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService,
        IProductBatchService productBatchService,
        IProductSerialService productSerialService)
    {
        if (_featureFlags is not null)
            _featureFlags.FlagsChanged -= OnFeatureFlagsChanged;

        _featureFlags = featureFlags;
        _productUnitService = productUnitService;
        _productBatchService = productBatchService;
        _productSerialService = productSerialService;
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
        ShowProductPricing = _featureFlags.ProductPricingEnabled;

        var industryStillEnabled = IsIndustryEnabled(_appliedIndustryTag);
        MarketTemplateFieldsEnabled = _featureFlags.AnyMarketTemplateEnabled && industryStillEnabled;

        if (!MarketTemplateFieldsEnabled)
            ClearCustomFieldLabels();
        else
        {
            // إعادة إشعار خصائص الظهور حتى تتحدّث أعمدة DataGrid فوراً
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

    private bool IsIndustryEnabled(string? industryTag) => industryTag switch
    {
        // بدون قالب مطبّق: لا تُظهر حقولاً مخصصة حتى يُختار قالب سوق مفعّل
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

        MarketTemplateFieldsEnabled = _featureFlags is null
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

        await LoadRowPricingOptionsAsync(row, productId);
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

        // سطر من نافذة الاختيار أو نسخة فاتورة: احتفظ بالسعر إن كان مضبوطاً مسبقاً
        var keepExistingPrice = row.PricingTypeId == preferred.PricingTypeId && row.UnitPrice > 0;
        if (keepExistingPrice)
            row.SetSelectedPricingOptionWithoutPrice(preferred);
        else
            row.SelectedPricingOption = preferred;
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

                // مرتجع البيع: لا نخصم دفعات هنا (يُعالج لاحقاً عند إرجاع الدفعات)
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
        }
    }
}
