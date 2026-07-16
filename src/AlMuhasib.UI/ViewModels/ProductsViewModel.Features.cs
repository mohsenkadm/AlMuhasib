using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel
{
    private IFeatureFlagService? _featureFlags;
    private IProductUnitService? _productUnitService;
    private IProductBatchService? _productBatchService;
    private IProductSerialService? _productSerialService;

    public ObservableCollection<ProductUnit> EditUnits { get; } = [];
    public ObservableCollection<ProductBatch> EditBatches { get; } = [];
    public ObservableCollection<ProductSerial> EditSerials { get; } = [];

    [ObservableProperty] private bool _showUnitsSection;
    [ObservableProperty] private bool _showBatchesSection;
    [ObservableProperty] private bool _showSerialsSection;

    [ObservableProperty] private string _newUnitName = string.Empty;
    [ObservableProperty] private decimal _newUnitFactor = 1m;
    [ObservableProperty] private string _newSerialText = string.Empty;

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
        RefreshProductFeatureVisibility();
        featureFlags.FlagsChanged += OnFeatureFlagsChanged;
    }

    private void OnFeatureFlagsChanged(object? sender, EventArgs e) =>
        FeatureUiRefresh.Invoke(RefreshProductFeatureVisibility);

    private void RefreshProductFeatureVisibility()
    {
        if (_featureFlags is null) return;
        ShowUnitsSection = _featureFlags.UnitsOfMeasure;
        ShowBatchesSection = _featureFlags.ExpiryTracking;
        ShowSerialsSection = _featureFlags.SerialNumbers;

        if (!ShowUnitsSection && !ShowBatchesSection && !ShowSerialsSection)
            ClearFeatureEditCollections();
        else
        {
            if (!ShowUnitsSection) EditUnits.Clear();
            if (!ShowBatchesSection) EditBatches.Clear();
            if (!ShowSerialsSection) EditSerials.Clear();
        }
    }

    private void ClearFeatureEditCollections()
    {
        EditUnits.Clear();
        EditBatches.Clear();
        EditSerials.Clear();
        NewUnitName = string.Empty;
        NewUnitFactor = 1m;
        NewSerialText = string.Empty;
    }

    private async Task LoadFeatureDataForProductAsync(int productId)
    {
        ClearFeatureEditCollections();
        if (_productUnitService is not null && ShowUnitsSection)
        {
            foreach (var u in await _productUnitService.GetByProductAsync(productId))
                EditUnits.Add(u);
        }

        if (_productBatchService is not null && ShowBatchesSection)
        {
            foreach (var b in await _productBatchService.GetByProductAsync(productId))
                EditBatches.Add(b);
        }

        if (_productSerialService is not null && ShowSerialsSection)
        {
            foreach (var s in await _productSerialService.GetByProductAsync(productId))
                EditSerials.Add(s);
        }
    }

    [RelayCommand]
    private async Task AddProductUnitAsync()
    {
        if (_editingProductId is not int productId || _productUnitService is null)
        {
            BeautifulMessageDialog.ShowWarning("احفظ المنتج أولاً ثم أضف الوحدات");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewUnitName))
        {
            BeautifulMessageDialog.ShowWarning("أدخل اسم الوحدة");
            return;
        }

        try
        {
            await _productUnitService.SaveAsync(new ProductUnit
            {
                ProductId = productId,
                UnitName = NewUnitName.Trim(),
                ConversionFactor = NewUnitFactor <= 0 ? 1m : NewUnitFactor,
                IsDefault = EditUnits.Count == 0
            });
            NewUnitName = string.Empty;
            NewUnitFactor = 1m;
            await LoadFeatureDataForProductAsync(productId);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteProductUnitAsync(ProductUnit? unit)
    {
        if (unit is null || _productUnitService is null || _editingProductId is not int productId) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف الوحدة «{unit.UnitName}»؟")) return;
        await _productUnitService.DeleteAsync(unit.Id);
        await LoadFeatureDataForProductAsync(productId);
    }

    [RelayCommand]
    private async Task SetDefaultProductUnitAsync(ProductUnit? unit)
    {
        if (unit is null || _productUnitService is null || _editingProductId is not int productId) return;
        await _productUnitService.SetDefaultAsync(productId, unit.Id);
        await LoadFeatureDataForProductAsync(productId);
    }

    [RelayCommand]
    private async Task AddProductSerialsAsync()
    {
        if (_editingProductId is not int productId || _productSerialService is null)
        {
            BeautifulMessageDialog.ShowWarning("احفظ المنتج أولاً ثم أضف السيريالات");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewSerialText))
        {
            BeautifulMessageDialog.ShowWarning("أدخل رقماً تسلسلياً واحداً أو أكثر (مفصولة بفاصلة)");
            return;
        }

        try
        {
            var serials = NewSerialText.Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            await _productSerialService.AddRangeAsync(productId, null, serials);
            NewSerialText = string.Empty;
            await LoadFeatureDataForProductAsync(productId);
            BeautifulMessageDialog.ShowSuccess($"تم إضافة {serials.Length} سيريال");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }
}
