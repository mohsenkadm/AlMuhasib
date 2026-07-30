using AlMuhasib.Core.Entities;
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
    private IProductSizeService? _productSizeService;

    [ObservableProperty] private bool _isReturnMode;
    [ObservableProperty] private bool _showUnitsOfMeasure;
    [ObservableProperty] private bool _showExpiryTracking;
    [ObservableProperty] private bool _showSerialNumbers;
    [ObservableProperty] private bool _showClothingSizes;
    [ObservableProperty] private bool _showTransportFee;
    [ObservableProperty] private decimal _transportFeeAmount;
    [ObservableProperty] private string _clothingSizeHeader = ClothingSizeInvoiceHelper.SizeLabel;

    public bool ShowCustomField1 => ShowClothingSizes;
    public bool ShowCustomField2 => false;

    public void ConfigureFeatureServices(
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService,
        IProductBatchService productBatchService,
        IProductSerialService productSerialService,
        IProductSizeService productSizeService)
    {
        if (_featureFlags is not null)
            _featureFlags.FlagsChanged -= OnFeatureFlagsChanged;

        _featureFlags = featureFlags;
        _productUnitService = productUnitService;
        _productBatchService = productBatchService;
        _productSerialService = productSerialService;
        _productSizeService = productSizeService;
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
        ShowTransportFee = _featureFlags.TransportFees;
        ClothingSizeHeader = ClothingSizeInvoiceHelper.SizeLabel;
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

        if (!ShowClothingSizes)
        {
            foreach (var row in Items)
            {
                row.ProductSizeId = null;
                row.SizeName = string.Empty;
                row.CustomField1 = string.Empty;
                row.CustomField1Label = string.Empty;
            }
        }
        else
        {
            foreach (var row in Items)
            {
                row.CustomField1Label = ClothingSizeHeader;
                if (!string.IsNullOrWhiteSpace(row.SizeName))
                    row.CustomField1 = row.SizeName;
            }
        }

        if (IsReturnMode && !_featureFlags.PurchaseReturns)
        {
            IsReturnMode = false;
            PageTitle = "فاتورة مشتريات";
        }

        RecalculateTotals();
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
