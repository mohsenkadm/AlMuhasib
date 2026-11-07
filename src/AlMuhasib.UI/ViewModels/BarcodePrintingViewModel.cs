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
    private readonly bool _pricingEnabled;

    private System.Timers.Timer? _debounceTimer;
    private BarcodePrintingRow? _printRow;
    private bool _suppressReload;
    private int _loadRequestId;

    public ObservableCollection<BarcodePrintingRow> Rows { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];

    public IReadOnlyList<BarcodeLabelSizeOption> LabelSizeOptions { get; } =
    [
        new(BarcodeLabelSize.Mm30x15, BarcodeLabelSizeHelper.GetDisplayName(BarcodeLabelSize.Mm30x15)),
        new(BarcodeLabelSize.Mm38x25, BarcodeLabelSizeHelper.GetDisplayName(BarcodeLabelSize.Mm38x25))
    ];

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
    [ObservableProperty] private string _printBarcode = string.Empty;
    [ObservableProperty] private string _printPriceText = "—";
    [ObservableProperty] private int _printCopies = 1;
    [ObservableProperty] private BarcodeLabelSizeOption? _selectedLabelSize;
    [ObservableProperty] private BitmapSource? _previewImage;

    public BarcodePrintingViewModel(
        IProductService productService,
        ICategoryService categoryService,
        IProductPriceService productPriceService,
        IBarcodeLabelService barcodeLabelService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences)
    {
        _productService = productService;
        _categoryService = categoryService;
        _productPriceService = productPriceService;
        _barcodeLabelService = barcodeLabelService;
        _currentUserService = currentUserService;
        _pricingEnabled = userPreferences.Current.FeatureFlags.ProductPricingEnabled;
        PageTitle = "طباعة الباركودات";
        PageSize = 50;
        _selectedLabelSize = LabelSizeOptions.FirstOrDefault(o => o.Size == BarcodeLabelSize.Mm38x25)
                             ?? LabelSizeOptions[0];
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
            var barcode = ProductBarcodeHelper.Primary(p.Barcode) ?? p.Barcode?.Trim() ?? string.Empty;
            rows.Add(new BarcodePrintingRow
            {
                ProductId = p.Id,
                Name = p.Name,
                Barcode = barcode,
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
    private void OpenPrintDialog(BarcodePrintingRow? row)
    {
        if (row is null) return;
        if (!row.HasBarcode)
        {
            BeautifulMessageDialog.ShowWarning("هذا المنتج بلا باركود — أنشئ باركوداً أولاً من المنتجات");
            return;
        }

        _printRow = row;
        PrintProductName = row.Name;
        PrintBarcode = row.Barcode;
        PrintPriceText = row.PriceText;
        PrintCopies = 1;
        SelectedLabelSize ??= LabelSizeOptions.FirstOrDefault(o => o.Size == BarcodeLabelSize.Mm38x25)
                              ?? LabelSizeOptions[0];
        RefreshPreview();
        IsPrintDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePrintDialog()
    {
        IsPrintDialogOpen = false;
        _printRow = null;
        PreviewImage = null;
    }

    [RelayCommand]
    private void ConfirmPrint()
    {
        if (_printRow is null || string.IsNullOrWhiteSpace(PrintBarcode))
            return;

        if (PrintCopies < 1) PrintCopies = 1;
        if (PrintCopies > 200) PrintCopies = 200;

        var companyName = PrintBrandingProvider.Current.CompanyName ?? string.Empty;
        var items = Enumerable.Range(0, PrintCopies)
            .Select(_ => new BarcodeLabelItem
            {
                CompanyName = companyName,
                ProductName = PrintProductName,
                Barcode = PrintBarcode,
                Price = _printRow.Price
            })
            .ToList();

        var size = SelectedLabelSize?.Size ?? BarcodeLabelSize.Mm38x25;
        _barcodeLabelService.PrintLabels(items, size);
        IsPrintDialogOpen = false;
        StatusMessage = $"تمت طباعة {PrintCopies} ملصق — {PrintProductName}";
    }

    partial void OnSelectedLabelSizeChanged(BarcodeLabelSizeOption? value) => RefreshPreview();
    partial void OnPrintCopiesChanged(int value) => RefreshPreview();

    private void RefreshPreview()
    {
        if (string.IsNullOrWhiteSpace(PrintBarcode))
        {
            PreviewImage = null;
            return;
        }

        var size = SelectedLabelSize?.Size ?? BarcodeLabelSize.Mm38x25;
        var layout = BarcodeLabelSizeHelper.GetLayout(size);
        var png = _barcodeLabelService.CreateBarcodePng(
            PrintBarcode,
            (int)Math.Ceiling(layout.BarcodeWidth * 1.4),
            (int)Math.Ceiling(layout.BarcodeHeight * 1.4));
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
}
