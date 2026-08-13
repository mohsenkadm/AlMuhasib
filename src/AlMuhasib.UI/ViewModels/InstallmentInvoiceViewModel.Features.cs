using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentInvoiceViewModel
{
    private readonly IProductUnitService _productUnitService;
    private bool _suppressBulkPricingApply;

    [ObservableProperty] private bool _showUnitsOfMeasure;
    [ObservableProperty] private bool _showTransportFee;
    [ObservableProperty] private bool _showDriverSelection;
    [ObservableProperty] private bool _showProductPricing;
    [ObservableProperty] private decimal _transportFeeAmount;
    [ObservableProperty] private PricingType? _selectedBulkPricingType;

    public ObservableCollection<Driver> Drivers { get; } = [];
    public ObservableCollection<PricingType> BulkPricingTypes { get; } = [];

    [ObservableProperty] private Driver? _selectedDriver;

    private void RefreshFeatureVisibility()
    {
        ShowMenuWeight = _featureFlags.MenuWeight;
        ShowProductDiscount = _featureFlags.ProductDiscountEnabled;
        ShowUnitsOfMeasure = _featureFlags.UnitsOfMeasure;
        ShowTransportFee = _featureFlags.TransportFees;
        ShowDriverSelection = _featureFlags.WarehouseInvoiceAndDriver;
        ShowProductPricing = _featureFlags.ProductPricingEnabled;

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

        if (!ShowUnitsOfMeasure)
            ClearRowUnits();

        if (!ShowTransportFee)
            TransportFeeAmount = 0m;

        if (!ShowDriverSelection)
            SelectedDriver = null;

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

        InvoiceWeightSummaryText = InvoiceWeightHelper.BuildSummaryText(Items);
        RecalculateTotals();
    }

    private void ClearRowUnits()
    {
        foreach (var row in Items)
            ClearRowUnitsFor(row);
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

    private async Task LoadRowUnitsAsync(InvoiceItemRow row)
    {
        if (!ShowUnitsOfMeasure || row.ProductId is not int productId || productId <= 0)
        {
            ClearRowUnitsFor(row);
            return;
        }

        var units = await _productUnitService.GetByProductAsync(productId);
        row.AvailableUnits.Clear();
        foreach (var u in units)
            row.AvailableUnits.Add(u);

        if (!string.IsNullOrWhiteSpace(row.SelectedUnitName))
        {
            row.SelectedUnit = units.FirstOrDefault(u =>
                u.UnitName.Equals(row.SelectedUnitName, StringComparison.OrdinalIgnoreCase))
                ?? row.SelectedUnit;
        }

        row.SelectedUnit ??= units.FirstOrDefault(u => u.IsDefault) ?? units.FirstOrDefault();
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
        var options = InvoiceBulkPricingHelper.ToOptions(prices, usePurchasePrice: false);

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

    partial void OnTransportFeeAmountChanged(decimal value) => RecalculateTotals();
}
