using System.Collections.ObjectModel;
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

    private readonly List<string> _activeCustomFieldLabels = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCustomField1))]
    [NotifyPropertyChangedFor(nameof(ShowCustomField2))]
    private string _customField1Header = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCustomField2))]
    private string _customField2Header = string.Empty;

    [ObservableProperty] private bool _showUnitsOfMeasure;
    [ObservableProperty] private bool _showExpiryTracking;
    [ObservableProperty] private bool _showSerialNumbers;

    public bool ShowCustomField1 => !string.IsNullOrWhiteSpace(CustomField1Header);
    public bool ShowCustomField2 => !string.IsNullOrWhiteSpace(CustomField2Header);
    public IReadOnlyList<string> ActiveCustomFieldLabels => _activeCustomFieldLabels;

    public void ConfigureFeatureServices(
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService,
        IProductBatchService productBatchService,
        IProductSerialService productSerialService)
    {
        _featureFlags = featureFlags;
        _productUnitService = productUnitService;
        _productBatchService = productBatchService;
        _productSerialService = productSerialService;
        RefreshFeatureVisibility();
        featureFlags.FlagsChanged += (_, _) => RefreshFeatureVisibility();
    }

    private void RefreshFeatureVisibility()
    {
        if (_featureFlags is null) return;
        ShowUnitsOfMeasure = _featureFlags.UnitsOfMeasure;
        ShowExpiryTracking = _featureFlags.ExpiryTracking;
        ShowSerialNumbers = _featureFlags.SerialNumbers;
    }

    private void ApplyCustomFieldLabels(IEnumerable<string>? labels)
    {
        _activeCustomFieldLabels.Clear();
        if (labels is not null)
            _activeCustomFieldLabels.AddRange(labels.Where(l => !string.IsNullOrWhiteSpace(l)).Take(2));

        CustomField1Header = _activeCustomFieldLabels.ElementAtOrDefault(0) ?? string.Empty;
        CustomField2Header = _activeCustomFieldLabels.ElementAtOrDefault(1) ?? string.Empty;

        foreach (var row in Items)
            ApplyActiveLabelsToRow(row);
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

        if (ShowExpiryTracking && _productBatchService is not null && SelectedWarehouse is not null)
        {
            var batches = await _productBatchService.GetByProductAsync(productId, SelectedWarehouse.Id, inStockOnly: true);
            row.AvailableBatches.Clear();
            foreach (var b in batches)
                row.AvailableBatches.Add(b);
            if (row.SelectedBatch is null && batches.Count > 0)
                row.SelectedBatch = batches[0];
        }
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
                if (row.BatchId is int batchId)
                {
                    await _productBatchService.DeductAsync(batchId, stockQty);
                }
                else
                {
                    var fifo = await _productBatchService.FindFifoAsync(productId, SelectedWarehouse.Id, stockQty);
                    if (fifo is not null)
                        await _productBatchService.DeductAsync(fifo.Id, stockQty);
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
