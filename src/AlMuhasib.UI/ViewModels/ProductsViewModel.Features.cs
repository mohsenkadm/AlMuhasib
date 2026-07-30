using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
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
    private IProductSizeService? _productSizeService;

    public ObservableCollection<ProductUnit> EditUnits { get; } = [];
    public ObservableCollection<ProductBatch> EditBatches { get; } = [];
    public ObservableCollection<ProductSerial> EditSerials { get; } = [];
    public ObservableCollection<ProductSize> EditSizes { get; } = [];

    [ObservableProperty] private bool _showUnitsSection;
    [ObservableProperty] private bool _showWeightSection;
    [ObservableProperty] private bool _showDiscountSection;
    [ObservableProperty] private bool _showBatchesSection;
    [ObservableProperty] private bool _showSerialsSection;
    [ObservableProperty] private bool _showSizesSection;

    [ObservableProperty] private string _newUnitName = string.Empty;
    [ObservableProperty] private decimal _newUnitFactor = 1m;
    [ObservableProperty] private string _newSerialText = string.Empty;
    [ObservableProperty] private string _newSizeName = string.Empty;

    public IReadOnlyList<string> WeightUnitOptions { get; } =
        ["كغ", "غرام", "لتر", "مل", "متر", "سم"];

    public IReadOnlyList<DiscountTypeOption> ProductDiscountTypeOptions { get; } =
    [
        new(DiscountType.None, "بدون خصم"),
        new(DiscountType.Percentage, "نسبة مئوية (%)"),
        new(DiscountType.FixedAmount, "قيمة ثابتة (د.ع لكل وحدة)")
    ];

    [ObservableProperty] private DiscountTypeOption? _editDiscountTypeOption;

    partial void OnEditDiscountTypeChanged(DiscountType value)
    {
        var match = ProductDiscountTypeOptions.FirstOrDefault(o => o.Type == value);
        if (!Equals(EditDiscountTypeOption, match))
            EditDiscountTypeOption = match;
    }

    partial void OnEditDiscountTypeOptionChanged(DiscountTypeOption? value)
    {
        if (value is not null && EditDiscountType != value.Type)
            EditDiscountType = value.Type;
    }

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
        RefreshProductFeatureVisibility();
        featureFlags.FlagsChanged += OnFeatureFlagsChanged;
    }

    private void OnFeatureFlagsChanged(object? sender, EventArgs e) =>
        FeatureUiRefresh.Invoke(RefreshProductFeatureVisibility);

    private void RefreshProductFeatureVisibility()
    {
        if (_featureFlags is null) return;
        ShowUnitsSection = _featureFlags.UnitsOfMeasure;
        ShowWeightSection = _featureFlags.MenuWeight;
        ShowDiscountSection = _featureFlags.ProductDiscountEnabled;
        ShowBatchesSection = _featureFlags.ExpiryTracking;
        ShowSerialsSection = _featureFlags.SerialNumbers;
        ShowSizesSection = _featureFlags.TemplateClothing;

        if (!ShowUnitsSection && !ShowBatchesSection && !ShowSerialsSection && !ShowSizesSection)
            ClearFeatureEditCollections();
        else
        {
            if (!ShowUnitsSection) EditUnits.Clear();
            if (!ShowBatchesSection) EditBatches.Clear();
            if (!ShowSerialsSection) EditSerials.Clear();
            if (!ShowSizesSection) EditSizes.Clear();
        }
    }

    private void ClearFeatureEditCollections()
    {
        EditUnits.Clear();
        EditBatches.Clear();
        EditSerials.Clear();
        EditSizes.Clear();
        NewUnitName = string.Empty;
        NewUnitFactor = 1m;
        NewSerialText = string.Empty;
        NewSizeName = string.Empty;
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

        if (_productSizeService is not null && ShowSizesSection)
        {
            foreach (var size in await _productSizeService.GetByProductAsync(productId))
                EditSizes.Add(size);
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
    private async Task AddProductSizeAsync()
    {
        if (_editingProductId is not int productId || _productSizeService is null)
        {
            BeautifulMessageDialog.ShowWarning("احفظ المنتج أولاً ثم أضف القياسات");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewSizeName))
        {
            BeautifulMessageDialog.ShowWarning("أدخل اسم القياس (مثل L أو XL)");
            return;
        }

        try
        {
            await _productSizeService.SaveAsync(new ProductSize
            {
                ProductId = productId,
                SizeName = NewSizeName.Trim()
            });
            NewSizeName = string.Empty;
            await LoadFeatureDataForProductAsync(productId);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteProductSizeAsync(ProductSize? size)
    {
        if (size is null || _productSizeService is null || _editingProductId is not int productId) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف القياس «{size.SizeName}»؟")) return;
        try
        {
            await _productSizeService.DeleteAsync(size.Id);
            await LoadFeatureDataForProductAsync(productId);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
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
