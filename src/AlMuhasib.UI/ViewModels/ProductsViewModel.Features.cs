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
    private IPackagingTypeService? _packagingTypeService;
    private IProductBatchService? _productBatchService;
    private IProductSerialService? _productSerialService;
    private IProductSizeService? _productSizeService;
    private IProductColorService? _productColorService;

    public ObservableCollection<ProductUnit> EditUnits { get; } = [];
    public ObservableCollection<ProductBatch> EditBatches { get; } = [];
    public ObservableCollection<ProductSerial> EditSerials { get; } = [];
    public ObservableCollection<ProductSize> EditSizes { get; } = [];
    public ObservableCollection<ProductColor> EditColors { get; } = [];
    public ObservableCollection<PackagingType> AvailablePackagingTypes { get; } = [];
    public ObservableCollection<string> SizeFilterOptions { get; } = [];
    public ObservableCollection<string> ColorFilterOptions { get; } = [];

    [ObservableProperty] private bool _showUnitsSection;
    [ObservableProperty] private bool _showWeightSection;
    [ObservableProperty] private bool _showDiscountSection;
    [ObservableProperty] private bool _showBatchesSection;
    [ObservableProperty] private bool _showSerialsSection;
    [ObservableProperty] private bool _showSizesSection;
    [ObservableProperty] private bool _showColorsSection;
    [ObservableProperty] private bool _showScientificName;

    [ObservableProperty] private PackagingType? _selectedPackagingTypeToAdd;
    [ObservableProperty] private decimal _newUnitFactor = 1m;
    [ObservableProperty] private string _newSerialText = string.Empty;
    [ObservableProperty] private string _newSizeName = string.Empty;
    [ObservableProperty] private string _newColorName = string.Empty;

    [ObservableProperty] private bool _isPackagingDialogOpen;
    [ObservableProperty] private string _packagingDialogTitle = string.Empty;
    [ObservableProperty] private string _packagingDialogProductName = string.Empty;
    private int? _packagingDialogProductId;

    [ObservableProperty] private bool _isClothingDialogOpen;
    [ObservableProperty] private string _clothingDialogTitle = string.Empty;
    private int? _clothingDialogProductId;

    [ObservableProperty] private string? _selectedSizeFilter;
    [ObservableProperty] private string? _selectedColorFilter;
    [ObservableProperty] private bool _filterHasBatchesOnly;

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

    partial void OnSelectedSizeFilterChanged(string? value)
    {
        if (_isInitializing) return;
        CurrentPage = 1;
        _ = LoadProductsAsync();
    }

    partial void OnSelectedColorFilterChanged(string? value)
    {
        if (_isInitializing) return;
        CurrentPage = 1;
        _ = LoadProductsAsync();
    }

    partial void OnFilterHasBatchesOnlyChanged(bool value)
    {
        if (_isInitializing) return;
        CurrentPage = 1;
        _ = LoadProductsAsync();
    }

    public void ConfigureFeatureServices(
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService,
        IPackagingTypeService packagingTypeService,
        IProductBatchService productBatchService,
        IProductSerialService productSerialService,
        IProductSizeService productSizeService,
        IProductColorService productColorService)
    {
        if (_featureFlags is not null)
            _featureFlags.FlagsChanged -= OnFeatureFlagsChanged;

        _featureFlags = featureFlags;
        _productUnitService = productUnitService;
        _packagingTypeService = packagingTypeService;
        _productBatchService = productBatchService;
        _productSerialService = productSerialService;
        _productSizeService = productSizeService;
        _productColorService = productColorService;
        RefreshProductFeatureVisibility();
        featureFlags.FlagsChanged += OnFeatureFlagsChanged;
    }

    private void OnFeatureFlagsChanged(object? sender, EventArgs e) =>
        FeatureUiRefresh.Invoke(() =>
        {
            RefreshProductFeatureVisibility();
            _ = ReloadAfterFeatureFlagsChangedAsync();
        });

    private async Task ReloadAfterFeatureFlagsChangedAsync()
    {
        await RefreshFeatureFilterOptionsAsync();
        CurrentPage = 1;
        await LoadProductsAsync();
    }

    private void RefreshProductFeatureVisibility()
    {
        if (_featureFlags is null) return;
        ShowUnitsSection = _featureFlags.UnitsOfMeasure;
        ShowWeightSection = _featureFlags.MenuWeight;
        ShowDiscountSection = _featureFlags.ProductDiscountEnabled;
        ShowBatchesSection = _featureFlags.ExpiryTracking;
        ShowSerialsSection = _featureFlags.SerialNumbers;
        ShowSizesSection = _featureFlags.TemplateClothing;
        ShowColorsSection = _featureFlags.TemplateClothing;
        ShowScientificName = _featureFlags.TemplatePharmacy;

        if (!ShowSizesSection)
        {
            SelectedSizeFilter = null;
            SelectedColorFilter = null;
        }

        if (!ShowBatchesSection)
            FilterHasBatchesOnly = false;

        if (!ShowUnitsSection && !ShowBatchesSection && !ShowSerialsSection && !ShowSizesSection)
            ClearFeatureEditCollections();
        else
        {
            if (!ShowUnitsSection) EditUnits.Clear();
            if (!ShowBatchesSection) EditBatches.Clear();
            if (!ShowSerialsSection) EditSerials.Clear();
            if (!ShowSizesSection) EditSizes.Clear();
            if (!ShowColorsSection) EditColors.Clear();
        }
    }

    private void ClearFeatureEditCollections()
    {
        EditUnits.Clear();
        EditBatches.Clear();
        EditSerials.Clear();
        EditSizes.Clear();
        EditColors.Clear();
        SelectedPackagingTypeToAdd = null;
        NewUnitFactor = 1m;
        NewSerialText = string.Empty;
        NewSizeName = string.Empty;
        NewColorName = string.Empty;
    }

    public const string AllFilterLabel = "— الكل —";

    private async Task RefreshFeatureFilterOptionsAsync()
    {
        var previousSize = SelectedSizeFilter;
        var previousColor = SelectedColorFilter;

        SizeFilterOptions.Clear();
        ColorFilterOptions.Clear();

        if (ShowSizesSection && _productSizeService is not null)
        {
            SizeFilterOptions.Add(AllFilterLabel);
            foreach (var name in await _productSizeService.GetDistinctSizeNamesAsync())
                SizeFilterOptions.Add(name);
            SelectedSizeFilter = SizeFilterOptions.Contains(previousSize ?? string.Empty)
                ? previousSize
                : AllFilterLabel;
        }

        if (ShowColorsSection && _productColorService is not null)
        {
            ColorFilterOptions.Add(AllFilterLabel);
            foreach (var name in await _productColorService.GetDistinctColorNamesAsync())
                ColorFilterOptions.Add(name);
            SelectedColorFilter = ColorFilterOptions.Contains(previousColor ?? string.Empty)
                ? previousColor
                : AllFilterLabel;
        }
    }

    private async Task EnsurePackagingTypesLoadedAsync()
    {
        if (_packagingTypeService is null || !ShowUnitsSection) return;
        await _packagingTypeService.EnsureDefaultExistsAsync();
        var types = await _packagingTypeService.GetActiveAsync();
        AvailablePackagingTypes.Clear();
        foreach (var t in types)
            AvailablePackagingTypes.Add(t);
        SelectedPackagingTypeToAdd ??= AvailablePackagingTypes.FirstOrDefault(t => t.IsDefault)
            ?? AvailablePackagingTypes.FirstOrDefault();
    }

    private async Task LoadFeatureDataForProductAsync(int productId)
    {
        ClearFeatureEditCollections();
        if (_productUnitService is not null && ShowUnitsSection)
        {
            await EnsurePackagingTypesLoadedAsync();
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

        if (_productColorService is not null && ShowColorsSection)
        {
            foreach (var color in await _productColorService.GetByProductAsync(productId))
                EditColors.Add(color);
        }
    }

    [RelayCommand]
    private async Task OpenProductPackagingDialogAsync(Product? product)
    {
        if (product is null || !ShowUnitsSection || _productUnitService is null)
            return;

        _packagingDialogProductId = product.Id;
        PackagingDialogProductName = product.Name;
        PackagingDialogTitle = $"أنواع التعبئة — {product.Name}";
        await LoadFeatureDataForProductAsync(product.Id);
        IsPackagingDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePackagingDialog()
    {
        IsPackagingDialogOpen = false;
        _packagingDialogProductId = null;
        PackagingDialogProductName = string.Empty;
        if (_editingProductId is null && !IsClothingDialogOpen)
            ClearFeatureEditCollections();
    }

    private int? ResolvePackagingProductId() =>
        _packagingDialogProductId ?? _editingProductId;

    private int? ResolveClothingProductId() =>
        _clothingDialogProductId ?? _editingProductId;

    [RelayCommand]
    private async Task OpenProductClothingDialogAsync(Product? product)
    {
        if (product is null || (!ShowSizesSection && !ShowColorsSection))
            return;

        _clothingDialogProductId = product.Id;
        ClothingDialogTitle = $"القياسات والألوان — {product.Name}";
        await LoadFeatureDataForProductAsync(product.Id);
        IsClothingDialogOpen = true;
    }

    [RelayCommand]
    private void CloseClothingDialog()
    {
        IsClothingDialogOpen = false;
        _clothingDialogProductId = null;
        ClothingDialogTitle = string.Empty;
        if (_editingProductId is null && !IsPackagingDialogOpen)
            ClearFeatureEditCollections();
        _ = RefreshFeatureFilterOptionsAsync();
    }

    [RelayCommand]
    private async Task AddProductUnitAsync()
    {
        if (ResolvePackagingProductId() is not int productId || _productUnitService is null)
        {
            BeautifulMessageDialog.ShowWarning("احفظ المنتج أولاً ثم أضف أنواع التعبئة");
            return;
        }

        if (SelectedPackagingTypeToAdd is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر نوع التعبئة");
            return;
        }

        try
        {
            await _productUnitService.SaveAsync(new ProductUnit
            {
                ProductId = productId,
                PackagingTypeId = SelectedPackagingTypeToAdd.Id,
                UnitName = SelectedPackagingTypeToAdd.Name,
                ConversionFactor = NewUnitFactor <= 0 ? 1m : NewUnitFactor,
                IsDefault = EditUnits.Count == 0
            });
            NewUnitFactor = 1m;
            SelectedPackagingTypeToAdd = AvailablePackagingTypes.FirstOrDefault(t =>
                EditUnits.All(u => u.PackagingTypeId != t.Id));
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
        if (unit is null || _productUnitService is null || ResolvePackagingProductId() is not int productId) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف التعبئة «{unit.UnitName}»؟")) return;
        await _productUnitService.DeleteAsync(unit.Id);
        await LoadFeatureDataForProductAsync(productId);
    }

    [RelayCommand]
    private async Task SetDefaultProductUnitAsync(ProductUnit? unit)
    {
        if (unit is null || _productUnitService is null || ResolvePackagingProductId() is not int productId) return;
        await _productUnitService.SetDefaultAsync(productId, unit.Id);
        await LoadFeatureDataForProductAsync(productId);
    }

    [RelayCommand]
    private async Task AddProductSizeAsync()
    {
        if (ResolveClothingProductId() is not int productId || _productSizeService is null)
        {
            BeautifulMessageDialog.ShowWarning("احفظ المنتج أولاً ثم أضف القياسات");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewSizeName))
        {
            BeautifulMessageDialog.ShowWarning("أدخل اسم القياس");
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
            await RefreshFeatureFilterOptionsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteProductSizeAsync(ProductSize? size)
    {
        if (size is null || _productSizeService is null || ResolveClothingProductId() is not int productId) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف القياس «{size.SizeName}»؟")) return;
        try
        {
            await _productSizeService.DeleteAsync(size.Id);
            await LoadFeatureDataForProductAsync(productId);
            await RefreshFeatureFilterOptionsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task AddProductColorAsync()
    {
        if (ResolveClothingProductId() is not int productId || _productColorService is null)
        {
            BeautifulMessageDialog.ShowWarning("احفظ المنتج أولاً ثم أضف الألوان");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewColorName))
        {
            BeautifulMessageDialog.ShowWarning("أدخل اسم اللون");
            return;
        }

        try
        {
            await _productColorService.SaveAsync(new ProductColor
            {
                ProductId = productId,
                ColorName = NewColorName.Trim()
            });
            NewColorName = string.Empty;
            await LoadFeatureDataForProductAsync(productId);
            await RefreshFeatureFilterOptionsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteProductColorAsync(ProductColor? color)
    {
        if (color is null || _productColorService is null || ResolveClothingProductId() is not int productId) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف اللون «{color.ColorName}»؟")) return;
        await _productColorService.DeleteAsync(color.Id);
        await LoadFeatureDataForProductAsync(productId);
        await RefreshFeatureFilterOptionsAsync();
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
