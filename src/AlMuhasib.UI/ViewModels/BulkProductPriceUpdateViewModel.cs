using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class BulkProductPriceUpdateViewModel : ViewModelBase
{
    private static readonly decimal[] PresetPercents = [20m, 25m, 30m, 40m, 50m];

    private readonly IProductPriceService _productPriceService;
    private readonly IPricingTypeService _pricingTypeService;
    private readonly IProductService _productService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFeatureFlagService _featureFlags;

    private List<Product> _allProducts = [];
    private bool _suppressRecalc;
    private bool _suppressCategorySync;
    private bool _hasPendingChanges;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<BulkPriceUpdateRow> Rows { get; } = [];
    public ObservableCollection<PricingType> PricingTypes { get; } = [];
    public ObservableCollection<CategoryFilterItem> CategoryFilters { get; } = [];
    public ObservableCollection<PercentPresetChip> PercentPresetChips { get; } = [];
    public IReadOnlyList<PriceRoundingOption> RoundingOptions { get; } = PriceRoundingOption.All;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private PricingType? _selectedPricingType;
    [ObservableProperty] private AccountingCurrency _selectedCurrency = AccountingCurrency.IQD;
    [ObservableProperty] private bool _isIncrease = true;
    [ObservableProperty] private decimal _percentValue = 20m;
    [ObservableProperty] private PriceRoundingOption? _selectedRounding;
    [ObservableProperty] private bool _showMultiCurrency;
    [ObservableProperty] private bool _isCategoryPopupOpen;
    [ObservableProperty] private bool _selectAllCategories = true;
    [ObservableProperty] private bool _selectAllRows = true;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _categoryFilterSummary = "كل الأصناف";
    [ObservableProperty] private string _formulaSummary = string.Empty;
    [ObservableProperty] private string _exampleSummary = string.Empty;

    [ObservableProperty] private int _displayedCount;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private int _changingCount;
    [ObservableProperty] private string _averageChangePercentText = "0%";
    [ObservableProperty] private string _maxChangeText = "0";

    public bool IsDecrease
    {
        get => !IsIncrease;
        set
        {
            if (value)
                IsIncrease = false;
        }
    }

    public string CurrencyLabel => AccountingCurrencyHelper.GetDisplayName(SelectedCurrency);

    public override bool HasUnsavedChanges => _hasPendingChanges;
    public override bool SupportsSaveBeforeLeave => true;

    public BulkProductPriceUpdateViewModel(
        IProductPriceService productPriceService,
        IPricingTypeService pricingTypeService,
        IProductService productService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFeatureFlagService featureFlags)
    {
        _productPriceService = productPriceService;
        _pricingTypeService = pricingTypeService;
        _productService = productService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _featureFlags = featureFlags;

        PageTitle = "تعديل أسعار المنتجات";
        foreach (var p in PresetPercents)
            PercentPresetChips.Add(new PercentPresetChip(p));
        SyncPercentPresetSelection();
        SelectedRounding = RoundingOptions.FirstOrDefault(o => o.Mode == PriceRoundingMode.NearestOne)
                           ?? RoundingOptions[0];
        ShowMultiCurrency = _featureFlags.MultiCurrency;
        _featureFlags.FlagsChanged += (_, _) =>
            FeatureUiRefresh.Invoke(() =>
            {
                ShowMultiCurrency = _featureFlags.MultiCurrency;
                if (!ShowMultiCurrency && SelectedCurrency != AccountingCurrency.IQD)
                    SelectedCurrency = AccountingCurrency.IQD;
            });
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, ScreenPermissionRegistry.BulkProductPriceUpdate);
            // شاشة أسعار: صلاحية «تعديل السعر» كافية للحفظ حتى بدون CanEdit العام.
            if (!CanEdit
                && !_currentUserService.IsViewOnly(ScreenPermissionRegistry.BulkProductPriceUpdate)
                && _currentUserService.CanEditPrice(ScreenPermissionRegistry.BulkProductPriceUpdate))
            {
                CanEdit = true;
            }

            await _pricingTypeService.EnsureDefaultExistsAsync();
            await LoadLookupsAsync();
            await ReloadRowsAsync();
            UpdateFormulaSummary();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public override async Task<bool> SavePendingChangesAsync()
    {
        await SaveAsync();
        return !_hasPendingChanges;
    }

    private async Task LoadLookupsAsync()
    {
        PricingTypes.Clear();
        foreach (var t in await _pricingTypeService.GetActiveAsync())
            PricingTypes.Add(t);

        SelectedPricingType = PricingTypes.FirstOrDefault(t => t.IsDefault)
                              ?? PricingTypes.FirstOrDefault();

        _suppressCategorySync = true;
        CategoryFilters.Clear();
        foreach (var c in (await _unitOfWork.Categories.GetAllAsync()).OrderBy(c => c.Name))
        {
            var item = new CategoryFilterItem { Id = c.Id, Name = c.Name, IsChecked = true };
            item.Changed += OnCategoryFilterChanged;
            CategoryFilters.Add(item);
        }
        SelectAllCategories = true;
        _suppressCategorySync = false;
        UpdateCategoryFilterSummary();

        _allProducts = (await _productService.GetVisibleInCurrentBranchAsync())
            .OrderBy(p => p.Name)
            .ToList();
    }

    private async Task ReloadRowsAsync()
    {
        if (SelectedPricingType is null)
        {
            ClearRows();
            return;
        }

        var selectedCategoryIds = CategoryFilters
            .Where(c => c.IsChecked)
            .Select(c => c.Id)
            .ToHashSet();

        var allCategoriesSelected = CategoryFilters.Count == 0
            || selectedCategoryIds.Count == CategoryFilters.Count;

        if (CategoryFilters.Count > 0 && selectedCategoryIds.Count == 0)
        {
            ClearRows();
            StatusMessage = "اختر صنفاً واحداً على الأقل";
            return;
        }

        var search = SearchText?.Trim() ?? string.Empty;
        var products = _allProducts.Where(p =>
        {
            if (!allCategoriesSelected && !selectedCategoryIds.Contains(p.CategoryId))
                return false;
            if (string.IsNullOrWhiteSpace(search))
                return true;
            return p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                   || (p.Barcode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
        }).ToList();

        var prices = await _productPriceService.GetByProductIdsAsync(products.Select(p => p.Id));
        var priceByProduct = prices
            .Where(p => p.PricingTypeId == SelectedPricingType.Id)
            .GroupBy(p => p.ProductId)
            .ToDictionary(g => g.Key, g => g.First());

        _suppressRecalc = true;
        ClearRows();

        foreach (var product in products)
        {
            priceByProduct.TryGetValue(product.Id, out var price);
            var current = ResolveCurrentSale(price, SelectedCurrency);
            var otherSale = SelectedCurrency == AccountingCurrency.USD
                ? price?.SalePrice ?? 0m
                : price?.SalePriceUsd ?? 0m;

            var row = new BulkPriceUpdateRow
            {
                ProductId = product.Id,
                ProductPriceId = price?.Id,
                ProductName = product.Name,
                Barcode = product.Barcode,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? "—",
                PurchasePriceIqd = price?.PurchasePrice ?? 0m,
                PurchasePriceUsd = price?.PurchasePriceUsd ?? 0m,
                OtherCurrencySalePrice = otherSale,
                CurrentPrice = current,
                Currency = SelectedCurrency,
                IsSelected = true
            };
            row.SetCalculatedNewPrice(current);
            row.SelectionOrPriceChanged += OnRowChanged;
            Rows.Add(row);
        }

        _suppressRecalc = false;
        SelectAllRows = true;
        RecalculateAll(forceManual: false);
        RefreshStats();
        StatusMessage = $"عرض {Rows.Count} منتجاً";
    }

    private void ClearRows()
    {
        foreach (var row in Rows)
            row.SelectionOrPriceChanged -= OnRowChanged;
        Rows.Clear();
        _hasPendingChanges = false;
        RefreshStats();
    }

    private static decimal ResolveCurrentSale(ProductPrice? price, AccountingCurrency currency)
    {
        if (price is null) return 0m;
        return currency == AccountingCurrency.USD ? price.SalePriceUsd : price.SalePrice;
    }

    private void OnRowChanged()
    {
        if (_suppressRecalc) return;
        _hasPendingChanges = Rows.Any(r => r.IsSelected && r.HasChange);
        RefreshStats();
        UpdateFormulaSummary();
    }

    private void RecalculateAll(bool forceManual)
    {
        if (_suppressRecalc) return;

        var percent = Math.Max(0m, PercentValue);
        var mode = SelectedRounding?.Mode ?? PriceRoundingMode.None;

        foreach (var row in Rows)
        {
            if (!forceManual && row.IsManualOverride)
                continue;

            var next = BulkSalePriceAdjuster.Apply(row.CurrentPrice, percent, IsIncrease, mode);
            row.SetCalculatedNewPrice(next);
        }

        _hasPendingChanges = Rows.Any(r => r.IsSelected && r.HasChange);
        RefreshStats();
        UpdateFormulaSummary();
    }

    private void RefreshStats()
    {
        DisplayedCount = Rows.Count;
        SelectedCount = Rows.Count(r => r.IsSelected);
        var changing = Rows.Where(r => r.IsSelected && r.HasChange).ToList();
        ChangingCount = changing.Count;

        if (changing.Count == 0)
        {
            AverageChangePercentText = "0%";
            MaxChangeText = "0";
            return;
        }

        var percents = changing
            .Where(r => r.CurrentPrice > 0)
            .Select(r => (r.NewPrice - r.CurrentPrice) / r.CurrentPrice * 100m)
            .ToList();
        AverageChangePercentText = percents.Count == 0
            ? "—"
            : $"{percents.Average():N1}%";

        var maxAbs = changing.OrderByDescending(r => Math.Abs(r.Difference)).First();
        var fmt = SelectedCurrency == AccountingCurrency.USD ? "N2" : "N0";
        MaxChangeText = $"{(maxAbs.Difference > 0 ? "+" : "")}{maxAbs.Difference.ToString(fmt)}";
    }

    private void UpdateFormulaSummary()
    {
        var sign = IsIncrease ? "+" : "−";
        var pricingName = SelectedPricingType?.Name ?? "سعر البيع";
        FormulaSummary = $"{pricingName} {sign} {PercentValue:N0}% · {CurrencyLabel}";

        var sample = Rows.FirstOrDefault(r => r.IsSelected && r.CurrentPrice > 0)
                     ?? Rows.FirstOrDefault(r => r.CurrentPrice > 0);
        if (sample is null)
        {
            ExampleSummary = "لا يوجد مثال سعري";
            return;
        }

        var fmt = SelectedCurrency == AccountingCurrency.USD ? "N2" : "N0";
        ExampleSummary = $"{sample.CurrentPrice.ToString(fmt)} ← {sample.NewPrice.ToString(fmt)}";
    }

    private void UpdateCategoryFilterSummary()
    {
        var checkedCount = CategoryFilters.Count(c => c.IsChecked);
        if (CategoryFilters.Count == 0 || checkedCount == CategoryFilters.Count)
            CategoryFilterSummary = "كل الأصناف";
        else if (checkedCount == 0)
            CategoryFilterSummary = "لا أصناف محددة";
        else if (checkedCount <= 2)
            CategoryFilterSummary = string.Join("، ", CategoryFilters.Where(c => c.IsChecked).Select(c => c.Name));
        else
            CategoryFilterSummary = $"{checkedCount} أصناف محددة";
    }

    private void OnCategoryFilterChanged()
    {
        if (_suppressCategorySync) return;
        _suppressCategorySync = true;
        SelectAllCategories = CategoryFilters.Count > 0 && CategoryFilters.All(c => c.IsChecked);
        _suppressCategorySync = false;
        UpdateCategoryFilterSummary();
        DebounceReload();
    }

    private void DebounceReload()
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(350);
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                IsBusy = true;
                try { await ReloadRowsAsync(); }
                finally { IsBusy = false; }
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    partial void OnSearchTextChanged(string value) => DebounceReload();

    partial void OnSelectedPricingTypeChanged(PricingType? value)
    {
        if (_suppressRecalc) return;
        _ = ReloadAfterPricingChangeAsync();
    }

    private async Task ReloadAfterPricingChangeAsync()
    {
        IsBusy = true;
        try { await ReloadRowsAsync(); }
        finally { IsBusy = false; }
    }

    partial void OnSelectedCurrencyChanged(AccountingCurrency value)
    {
        OnPropertyChanged(nameof(CurrencyLabel));
        _ = ReloadAfterPricingChangeAsync();
    }

    partial void OnIsIncreaseChanged(bool value)
    {
        OnPropertyChanged(nameof(IsDecrease));
        RecalculateAll(forceManual: false);
    }

    partial void OnPercentValueChanged(decimal value)
    {
        SyncPercentPresetSelection();
        RecalculateAll(forceManual: false);
    }

    private void SyncPercentPresetSelection()
    {
        foreach (var chip in PercentPresetChips)
            chip.IsSelected = chip.Value == PercentValue;
    }

    partial void OnSelectedRoundingChanged(PriceRoundingOption? value) => RecalculateAll(forceManual: false);

    partial void OnSelectAllCategoriesChanged(bool value)
    {
        if (_suppressCategorySync) return;
        _suppressCategorySync = true;
        foreach (var c in CategoryFilters)
            c.IsChecked = value;
        _suppressCategorySync = false;
        UpdateCategoryFilterSummary();
        DebounceReload();
    }

    partial void OnSelectAllRowsChanged(bool value)
    {
        if (_suppressRecalc) return;
        _suppressRecalc = true;
        foreach (var row in Rows)
            row.IsSelected = value;
        _suppressRecalc = false;
        OnRowChanged();
    }

    [RelayCommand]
    private void SetIncrease() => IsIncrease = true;

    [RelayCommand]
    private void SetDecrease() => IsIncrease = false;

    [RelayCommand]
    private void ApplyPreset(object? parameter)
    {
        if (parameter is null) return;
        if (parameter is PercentPresetChip chip)
            PercentValue = chip.Value;
        else if (parameter is decimal d)
            PercentValue = d;
        else if (decimal.TryParse(parameter.ToString(), out var parsed))
            PercentValue = parsed;
    }

    [RelayCommand]
    private void ApplyPreview() => RecalculateAll(forceManual: true);

    [RelayCommand]
    private void ResetOverrides()
    {
        foreach (var row in Rows)
            row.IsManualOverride = false;
        RecalculateAll(forceManual: true);
    }

    [RelayCommand]
    private void ResetRow(BulkPriceUpdateRow? row)
    {
        if (row is null) return;
        row.IsManualOverride = false;
        var next = BulkSalePriceAdjuster.Apply(
            row.CurrentPrice,
            Math.Max(0m, PercentValue),
            IsIncrease,
            SelectedRounding?.Mode ?? PriceRoundingMode.None);
        row.SetCalculatedNewPrice(next);
    }

    [RelayCommand]
    private void ToggleCategoryPopup() => IsCategoryPopupOpen = !IsCategoryPopupOpen;

    [RelayCommand]
    private async Task Refresh()
    {
        IsBusy = true;
        try
        {
            await LoadLookupsAsync();
            await ReloadRowsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعديل الأسعار");
            return;
        }

        if (SelectedPricingType is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر نوع التسعير");
            return;
        }

        if (!ShowMultiCurrency && SelectedCurrency != AccountingCurrency.IQD)
            SelectedCurrency = AccountingCurrency.IQD;

        var toSave = Rows.Where(r => r.IsSelected && r.HasChange).ToList();
        if (toSave.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning(
                "لا توجد تغييرات للحفظ.\nطبّق نسبة على منتجات لها سعر حالي، أو عدّل «سعر البيع الجديد» يدوياً للمنتجات المحددة.");
            return;
        }

        var confirm = BeautifulMessageDialog.ShowConfirm(
            $"سيتم تحديث سعر البيع لـ {toSave.Count} منتجاً ({CurrencyLabel} · {SelectedPricingType.Name}). متابعة؟",
            "تأكيد حفظ الأسعار");
        if (!confirm) return;

        IsBusy = true;
        try
        {
            var entities = toSave.Select(r =>
            {
                var saleIqd = SelectedCurrency == AccountingCurrency.IQD ? r.NewPrice : r.OtherCurrencySalePrice;
                var saleUsd = SelectedCurrency == AccountingCurrency.USD ? r.NewPrice : r.OtherCurrencySalePrice;
                return new ProductPrice
                {
                    Id = r.ProductPriceId ?? 0,
                    ProductId = r.ProductId,
                    PricingTypeId = SelectedPricingType.Id,
                    SalePrice = Math.Max(0m, saleIqd),
                    SalePriceUsd = Math.Max(0m, saleUsd),
                    PurchasePrice = Math.Max(0m, r.PurchasePriceIqd),
                    PurchasePriceUsd = Math.Max(0m, r.PurchasePriceUsd)
                };
            }).ToList();

            await _productPriceService.UpsertManyAsync(entities);
            _hasPendingChanges = false;
            StatusMessage = $"تم حفظ {entities.Count} سعراً بنجاح";
            BeautifulMessageDialog.ShowSuccess($"تم تحديث أسعار {entities.Count} منتجاً");
            await ReloadRowsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>زر نسبة جاهزة في لوحة تعديل الأسعار.</summary>
public partial class PercentPresetChip : ObservableObject
{
    public PercentPresetChip(decimal value)
    {
        Value = value;
        Label = $"{value:0}%";
    }

    public decimal Value { get; }
    public string Label { get; }

    [ObservableProperty] private bool _isSelected;
}
