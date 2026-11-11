using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public sealed class BarcodePrintingRow
{
    public required int ProductId { get; init; }
    public required string Name { get; init; }
    public required string Barcode { get; init; }
    public required string BarcodeField { get; init; }
    public string CategoryName { get; init; } = "—";
    public string PriceText { get; init; } = "—";
    public decimal? Price { get; init; }
    public bool HasBarcode { get; init; }
}

public partial class BarcodePrintingViewModel : PagedViewModelBase
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IProductPriceService _productPriceService;
    private readonly IBarcodeLabelService _barcodeLabelService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBranchContext _branchContext;
    private readonly bool _pricingEnabled;

    private System.Timers.Timer? _debounceTimer;
    private BarcodePrintingRow? _printRow;
    private bool _suppressReload;
    private bool _suppressCustomSizeSync;
    private int _loadRequestId;

    public ObservableCollection<BarcodePrintingRow> Rows { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<string> AvailableBarcodes { get; } = [];
    public ObservableCollection<BarcodePriceOption> PriceOptions { get; } = [];

    public IReadOnlyList<BarcodeLabelSizeOption> LabelSizeOptions { get; } = BarcodeLabelSizeOption.CreateAll();

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private bool _onlyWithBarcode = true;
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private int _withBarcodeCount;
    [ObservableProperty] private int _withoutBarcodeCount;
    [ObservableProperty] private int _filteredCount;
    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty] private bool _isPrintDialogOpen;
    [ObservableProperty] private string _printProductName = string.Empty;
    [ObservableProperty] private string? _selectedBarcode;
    [ObservableProperty] private string _printPriceText = "—";
    [ObservableProperty] private BarcodePriceOption? _selectedPriceOption;
    [ObservableProperty] private int _printCopies = 1;
    [ObservableProperty] private BarcodeLabelSizeOption? _selectedLabelSize;
    [ObservableProperty] private double _customWidthMm = BarcodeLabelSizeHelper.DefaultCustomWidthMm;
    [ObservableProperty] private double _customHeightMm = BarcodeLabelSizeHelper.DefaultCustomHeightMm;
    [ObservableProperty] private double _gapMm = BarcodeLabelSizeHelper.DefaultGapMm;
    [ObservableProperty] private BitmapSource? _previewImage;
    [ObservableProperty] private bool _showProductNameOnLabel = true;
    [ObservableProperty] private bool _showBranchNameOnLabel;
    [ObservableProperty] private bool _showCompanyNameOnLabel;
    [ObservableProperty] private bool _showPriceOnLabel = true;
    [ObservableProperty] private bool _showBarcodeNumberOnLabel = true;
    [ObservableProperty] private bool _hasMultipleBarcodes;
    [ObservableProperty] private bool _hasSingleBarcode = true;
    [ObservableProperty] private bool _hasPriceOptions;
    [ObservableProperty] private bool _hasNoPriceOptions = true;
    [ObservableProperty] private bool _isCustomSizeSelected;

    public BarcodePrintingViewModel(
        IProductService productService,
        ICategoryService categoryService,
        IProductPriceService productPriceService,
        IBarcodeLabelService barcodeLabelService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences,
        IBranchContext branchContext)
    {
        _productService = productService;
        _categoryService = categoryService;
        _productPriceService = productPriceService;
        _barcodeLabelService = barcodeLabelService;
        _currentUserService = currentUserService;
        _branchContext = branchContext;
        _pricingEnabled = userPreferences.Current.FeatureFlags.ProductPricingEnabled;
        PageTitle = "طباعة الباركودات";
        PageSize = 50;
        _selectedLabelSize = LabelSizeOptions.FirstOrDefault(o => o.Size == BarcodeLabelSizeHelper.DefaultSize)
                             ?? LabelSizeOptions[0];
        _isCustomSizeSelected = _selectedLabelSize.IsCustom;
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "جاري التحميل...";
        try
        {
            LoadPermissions(_currentUserService, ScreenPermissionRegistry.BarcodePrinting);
            await LoadCategoriesAsync();
            await LoadPageAsync(resetPage: true, refreshStats: true);
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل المنتجات";
            BeautifulMessageDialog.ShowError(
                $"خطأ في تحميل طباعة الباركودات:\n\n{ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categoryService.GetVisibleInCurrentBranchAsync();
        Categories.Clear();
        foreach (var c in categories.OrderBy(x => x.Name))
            Categories.Add(c);
    }

    private async Task LoadPageAsync(bool resetPage, bool refreshStats)
    {
        var requestId = ++_loadRequestId;
        if (resetPage)
            CurrentPage = 1;

        var categoryId = SelectedCategory?.Id;
        var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        bool? hasBarcode = OnlyWithBarcode ? true : null;

        Task<(int Total, int WithBarcode, int WithoutBarcode)>? statsTask = null;
        if (refreshStats)
            statsTask = _productService.GetBarcodePrintingStatsAsync(categoryId);

        var pageTask = _productService.GetPagedAsync(
            CurrentPage,
            PageSize,
            categoryId,
            search,
            hasBarcode: hasBarcode);

        if (statsTask is not null)
        {
            var stats = await statsTask;
            if (requestId != _loadRequestId) return;
            TotalProducts = stats.Total;
            WithBarcodeCount = stats.WithBarcode;
            WithoutBarcodeCount = stats.WithoutBarcode;
        }

        var (items, totalCount) = await pageTask;
        if (requestId != _loadRequestId) return;

        var pageItems = items as IList<Product> ?? items.ToList();
        Dictionary<int, decimal?> prices = [];
        if (_pricingEnabled && pageItems.Count > 0)
        {
            var priceRows = await _productPriceService.GetByProductIdsAsync(pageItems.Select(p => p.Id));
            if (requestId != _loadRequestId) return;
            prices = priceRows
                .GroupBy(p => p.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (decimal?)(g.FirstOrDefault(x => x.PricingType?.IsDefault == true)?.SalePrice
                                    ?? g.FirstOrDefault()?.SalePrice));
        }

        var rows = new List<BarcodePrintingRow>(pageItems.Count);
        foreach (var p in pageItems)
        {
            prices.TryGetValue(p.Id, out var price);
            var field = p.Barcode?.Trim() ?? string.Empty;
            var barcode = ProductBarcodeHelper.Primary(field) ?? field;
            rows.Add(new BarcodePrintingRow
            {
                ProductId = p.Id,
                Name = p.Name,
                Barcode = barcode,
                BarcodeField = field,
                CategoryName = p.Category?.Name ?? "—",
                Price = price is > 0 ? price : null,
                PriceText = price is > 0 ? $"{price:N0} د.ع" : "—",
                HasBarcode = !string.IsNullOrWhiteSpace(barcode)
            });
        }

        ApplyPaginationStats(totalCount);
        FilteredCount = totalCount;

        Rows.Clear();
        foreach (var row in rows)
            Rows.Add(row);

        StatusMessage = $"صفحة {CurrentPage} من {TotalPages} — المعروض {Rows.Count:N0} / الإجمالي {TotalProducts:N0}";
    }

    protected override Task OnPageChangedAsync() => ReloadQuietAsync(resetPage: false, refreshStats: false);

    private async Task ReloadQuietAsync(bool resetPage, bool refreshStats)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await LoadPageAsync(resetPage, refreshStats);
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر التحميل";
            BeautifulMessageDialog.ShowError(
                $"خطأ في تحميل طباعة الباركودات:\n\n{ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ScheduleReload()
    {
        if (_suppressReload) return;
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(280);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            await Application.Current.Dispatcher.InvokeAsync(async () =>
                await ReloadQuietAsync(resetPage: true, refreshStats: false));
        };
        _debounceTimer.Start();
    }

    partial void OnSearchTextChanged(string value) => ScheduleReload();
    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (_suppressReload) return;
        _ = ReloadQuietAsync(resetPage: true, refreshStats: true);
    }

    partial void OnOnlyWithBarcodeChanged(bool value)
    {
        if (_suppressReload) return;
        _ = ReloadQuietAsync(resetPage: true, refreshStats: false);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _suppressReload = true;
        try
        {
            SearchText = string.Empty;
            SelectedCategory = null;
            OnlyWithBarcode = true;
        }
        finally
        {
            _suppressReload = false;
        }

        await ReloadQuietAsync(resetPage: true, refreshStats: true);
    }

    [RelayCommand]
    private void ClearCategoryFilter() => SelectedCategory = null;

    [RelayCommand]
    private async Task OpenPrintDialog(BarcodePrintingRow? row)
    {
        if (row is null) return;
        if (!row.HasBarcode)
        {
            BeautifulMessageDialog.ShowWarning("هذا المنتج بلا باركود — أنشئ باركوداً أولاً من المنتجات");
            return;
        }

        _printRow = row;
        PrintProductName = row.Name;
        ApplyDefaultContentFlags();
        PrintCopies = 1;
        GapMm = BarcodeLabelSizeHelper.DefaultGapMm;
        SelectDefaultLabelSize();

        AvailableBarcodes.Clear();
        foreach (var code in ProductBarcodeHelper.Split(row.BarcodeField))
            AvailableBarcodes.Add(code);
        if (AvailableBarcodes.Count == 0 && !string.IsNullOrWhiteSpace(row.Barcode))
            AvailableBarcodes.Add(row.Barcode);

        HasMultipleBarcodes = AvailableBarcodes.Count > 1;
        HasSingleBarcode = !HasMultipleBarcodes;
        SelectedBarcode = AvailableBarcodes.FirstOrDefault();

        await LoadPriceOptionsAsync(row.ProductId, row.Price);

        RefreshPreview();
        IsPrintDialogOpen = true;
    }

    private async Task LoadPriceOptionsAsync(int productId, decimal? fallbackPrice)
    {
        PriceOptions.Clear();
        if (_pricingEnabled)
        {
            var prices = await _productPriceService.GetByProductIdAsync(productId);
            foreach (var p in prices.OrderByDescending(x => x.PricingType?.IsDefault == true)
                                    .ThenBy(x => x.PricingType?.Name))
            {
                var name = p.PricingType?.Name?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = "سعر";
                PriceOptions.Add(new BarcodePriceOption(
                    p.PricingTypeId,
                    name,
                    p.SalePrice > 0 ? p.SalePrice : null,
                    p.PricingType?.IsDefault == true));
            }
        }

        if (PriceOptions.Count == 0 && fallbackPrice is > 0)
        {
            PriceOptions.Add(new BarcodePriceOption(null, "سعر مفرد", fallbackPrice, true));
        }

        HasPriceOptions = PriceOptions.Count > 0;
        HasNoPriceOptions = !HasPriceOptions;
        SelectedPriceOption = PriceOptions.FirstOrDefault(o => o.IsDefault) ?? PriceOptions.FirstOrDefault();
        PrintPriceText = SelectedPriceOption?.SalePrice is > 0
            ? $"{SelectedPriceOption.SalePrice:N0} د.ع"
            : "—";
    }

    [RelayCommand]
    private void ClosePrintDialog()
    {
        IsPrintDialogOpen = false;
        _printRow = null;
        PreviewImage = null;
        AvailableBarcodes.Clear();
        PriceOptions.Clear();
    }

    [RelayCommand]
    private void RestorePrintDefaults()
    {
        ApplyDefaultContentFlags();
        PrintCopies = 1;
        GapMm = BarcodeLabelSizeHelper.DefaultGapMm;
        SelectDefaultLabelSize();
        CustomWidthMm = BarcodeLabelSizeHelper.DefaultCustomWidthMm;
        CustomHeightMm = BarcodeLabelSizeHelper.DefaultCustomHeightMm;

        if (AvailableBarcodes.Count > 0)
            SelectedBarcode = AvailableBarcodes[0];

        SelectedPriceOption = PriceOptions.FirstOrDefault(o => o.IsDefault) ?? PriceOptions.FirstOrDefault();
        RefreshPreview();
    }

    [RelayCommand]
    private void ConfirmPrint()
    {
        if (_printRow is null || string.IsNullOrWhiteSpace(SelectedBarcode))
            return;

        if (PrintCopies < 1) PrintCopies = 1;
        if (PrintCopies > 200) PrintCopies = 200;

        var item = BuildLabelItem();
        var items = Enumerable.Range(0, PrintCopies).Select(_ => CloneLabelItem(item)).ToList();
        var size = SelectedLabelSize?.Size ?? BarcodeLabelSizeHelper.DefaultSize;
        var (customW, customH) = GetCustomDimensionsOrNull(size);

        _barcodeLabelService.PrintLabels(items, size, customW, customH, GapMm);
        IsPrintDialogOpen = false;
        StatusMessage = $"تمت طباعة {PrintCopies} ملصق — {PrintProductName}";
    }

    private void ApplyDefaultContentFlags()
    {
        ShowProductNameOnLabel = true;
        ShowBranchNameOnLabel = false;
        ShowCompanyNameOnLabel = false;
        ShowPriceOnLabel = true;
        ShowBarcodeNumberOnLabel = true;
    }

    private void SelectDefaultLabelSize()
    {
        _suppressCustomSizeSync = true;
        try
        {
            SelectedLabelSize = LabelSizeOptions.FirstOrDefault(o => o.Size == BarcodeLabelSizeHelper.DefaultSize)
                                ?? LabelSizeOptions[0];
            IsCustomSizeSelected = SelectedLabelSize?.IsCustom == true;
        }
        finally
        {
            _suppressCustomSizeSync = false;
        }
    }

    partial void OnSelectedLabelSizeChanged(BarcodeLabelSizeOption? value)
    {
        if (_suppressCustomSizeSync) return;
        IsCustomSizeSelected = value?.IsCustom == true;
        if (value is { IsCustom: true })
        {
            if (CustomWidthMm <= 0) CustomWidthMm = BarcodeLabelSizeHelper.DefaultCustomWidthMm;
            if (CustomHeightMm <= 0) CustomHeightMm = BarcodeLabelSizeHelper.DefaultCustomHeightMm;
        }
        else if (value is not null)
        {
            var (w, h) = BarcodeLabelSizeHelper.GetMillimeters(value.Size);
            _suppressCustomSizeSync = true;
            try
            {
                CustomWidthMm = w;
                CustomHeightMm = h;
            }
            finally
            {
                _suppressCustomSizeSync = false;
            }
        }

        RefreshPreview();
    }

    partial void OnCustomWidthMmChanged(double value)
    {
        if (_suppressCustomSizeSync) return;
        EnsureCustomSizeSelected();
        RefreshPreview();
    }

    partial void OnCustomHeightMmChanged(double value)
    {
        if (_suppressCustomSizeSync) return;
        EnsureCustomSizeSelected();
        RefreshPreview();
    }

    private void EnsureCustomSizeSelected()
    {
        if (SelectedLabelSize?.IsCustom == true) return;
        _suppressCustomSizeSync = true;
        try
        {
            SelectedLabelSize = LabelSizeOptions.FirstOrDefault(o => o.IsCustom);
            IsCustomSizeSelected = true;
        }
        finally
        {
            _suppressCustomSizeSync = false;
        }
    }

    partial void OnSelectedBarcodeChanged(string? value) => RefreshPreview();
    partial void OnSelectedPriceOptionChanged(BarcodePriceOption? value)
    {
        PrintPriceText = value?.SalePrice is > 0 ? $"{value.SalePrice:N0} د.ع" : "—";
        RefreshPreview();
    }

    partial void OnPrintCopiesChanged(int value) => RefreshPreview();
    partial void OnGapMmChanged(double value) => RefreshPreview();
    partial void OnShowProductNameOnLabelChanged(bool value) => RefreshPreview();
    partial void OnShowBranchNameOnLabelChanged(bool value) => RefreshPreview();
    partial void OnShowCompanyNameOnLabelChanged(bool value) => RefreshPreview();
    partial void OnShowPriceOnLabelChanged(bool value) => RefreshPreview();
    partial void OnShowBarcodeNumberOnLabelChanged(bool value) => RefreshPreview();

    private void RefreshPreview()
    {
        if (string.IsNullOrWhiteSpace(SelectedBarcode))
        {
            PreviewImage = null;
            return;
        }

        var size = SelectedLabelSize?.Size ?? BarcodeLabelSizeHelper.DefaultSize;
        var (customW, customH) = GetCustomDimensionsOrNull(size);
        var png = _barcodeLabelService.CreateLabelPreviewPng(
            BuildLabelItem(),
            size,
            customW,
            customH,
            scale: 3.0);

        if (png is null || png.Length == 0)
        {
            PreviewImage = null;
            return;
        }

        var image = new BitmapImage();
        using var ms = new MemoryStream(png);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        PreviewImage = image;
    }

    private BarcodeLabelItem BuildLabelItem() => new()
    {
        CompanyName = PrintBrandingProvider.Current.CompanyName ?? string.Empty,
        BranchName = _branchContext.CurrentBranchName ?? string.Empty,
        ProductName = PrintProductName,
        Barcode = SelectedBarcode ?? string.Empty,
        Price = SelectedPriceOption?.SalePrice ?? _printRow?.Price,
        ShowCompanyName = ShowCompanyNameOnLabel,
        ShowBranchName = ShowBranchNameOnLabel,
        ShowProductName = ShowProductNameOnLabel,
        ShowPrice = ShowPriceOnLabel,
        ShowBarcodeNumber = ShowBarcodeNumberOnLabel
    };

    private static BarcodeLabelItem CloneLabelItem(BarcodeLabelItem source) => new()
    {
        CompanyName = source.CompanyName,
        BranchName = source.BranchName,
        ProductName = source.ProductName,
        Barcode = source.Barcode,
        Price = source.Price,
        KaratValue = source.KaratValue,
        WeightGrams = source.WeightGrams,
        ShowCompanyName = source.ShowCompanyName,
        ShowBranchName = source.ShowBranchName,
        ShowProductName = source.ShowProductName,
        ShowPrice = source.ShowPrice,
        ShowBarcodeNumber = source.ShowBarcodeNumber
    };

    private (double? Width, double? Height) GetCustomDimensionsOrNull(BarcodeLabelSize size)
    {
        if (size != BarcodeLabelSize.Custom)
            return (null, null);
        return (
            BarcodeLabelSizeHelper.ClampDimension(CustomWidthMm),
            BarcodeLabelSizeHelper.ClampDimension(CustomHeightMm));
    }
}
