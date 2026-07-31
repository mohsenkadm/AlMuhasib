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

    [ObservableProperty] private bool _showUnitsOfMeasure;
    [ObservableProperty] private bool _showTransportFee;
    [ObservableProperty] private bool _showDriverSelection;
    [ObservableProperty] private decimal _transportFeeAmount;

    public ObservableCollection<Driver> Drivers { get; } = [];

    [ObservableProperty] private Driver? _selectedDriver;

    private void RefreshFeatureVisibility()
    {
        ShowMenuWeight = _featureFlags.MenuWeight;
        ShowProductDiscount = _featureFlags.ProductDiscountEnabled;
        ShowUnitsOfMeasure = _featureFlags.UnitsOfMeasure;
        ShowTransportFee = _featureFlags.TransportFees;
        ShowDriverSelection = _featureFlags.WarehouseInvoiceAndDriver;

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

        InvoiceWeightSummaryText = InvoiceWeightHelper.BuildSummaryText(Items);
        RecalculateTotals();
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

    private async Task LoadRowUnitsAsync(InvoiceItemRow row)
    {
        if (!ShowUnitsOfMeasure || row.ProductId is not int productId || productId <= 0)
        {
            row.AvailableUnits.Clear();
            row.SelectedUnit = null;
            row.UnitConversionFactor = 1m;
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

    partial void OnTransportFeeAmountChanged(decimal value) => RecalculateTotals();
}
