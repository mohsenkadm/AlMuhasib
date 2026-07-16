using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class PurchaseInvoiceViewModel
{
    private IFeatureFlagService? _featureFlags;
    private IProductUnitService? _productUnitService;
    private IProductBatchService? _productBatchService;
    private IProductSerialService? _productSerialService;

    [ObservableProperty] private bool _isReturnMode;
    [ObservableProperty] private bool _showUnitsOfMeasure;
    [ObservableProperty] private bool _showExpiryTracking;
    [ObservableProperty] private bool _showSerialNumbers;

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

        // إن أُطفئ مرتجع المشتريات أثناء وضع المرتجع: ألغِ الوضع فوراً
        if (IsReturnMode && !_featureFlags.PurchaseReturns)
        {
            IsReturnMode = false;
            PageTitle = "فاتورة مشتريات";
        }
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
            row.SelectedUnit ??= units.FirstOrDefault(u => u.IsDefault) ?? units.FirstOrDefault();
        }
        else
        {
            row.AvailableUnits.Clear();
            row.SelectedUnit = null;
            row.UnitConversionFactor = 1m;
        }
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
        }
    }
}
