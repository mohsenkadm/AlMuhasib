using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Models;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly bool _pricingEnabled;

    private System.Timers.Timer? _debounceTimer;
    private bool _suppressReload;
    private int _loadRequestId;

    public ObservableCollection<BarcodePrintingRow> Rows { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];

    public PriceLabelPrintEditor LabelPrint { get; }

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private bool _onlyWithBarcode = true;
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private int _withBarcodeCount;
    [ObservableProperty] private int _withoutBarcodeCount;
    [ObservableProperty] private int _filteredCount;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool IsPrintDialogOpen => LabelPrint.IsOpen;

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
        _currentUserService = currentUserService;
        _pricingEnabled = userPreferences.Current.FeatureFlags.ProductPricingEnabled;
        LabelPrint = new PriceLabelPrintEditor(
            barcodeLabelService,
            productPriceService,
            userPreferences,
            _pricingEnabled,
            branchContext);
        LabelPrint.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PriceLabelPrintEditor.IsOpen))
                OnPropertyChanged(nameof(IsPrintDialogOpen));
        };
        LabelPrint.Notice += message => StatusMessage = message;
        PageTitle = "طباعة الباركودات";
        PageSize = 50;
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
            hasBarcode: hasBarcode,
            matchProductId: true);

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

        await LabelPrint.OpenAsync(row.ProductId, row.Name, row.BarcodeField, row.Price);
    }

}

public sealed record PriceLabelPaperOption(PriceLabelPaper Paper, string DisplayName)
{
    public static IReadOnlyList<PriceLabelPaperOption> CreateAll() =>
    [
        new(PriceLabelPaper.A4, "A4"),
        new(PriceLabelPaper.SingleLabel, "ملصق مفرد")
    ];
}

public sealed record PriceLabelSymbologyOption(PriceLabelSymbology Symbology, string DisplayName)
{
    public static IReadOnlyList<PriceLabelSymbologyOption> CreateAll() =>
    [
        new(PriceLabelSymbology.Ean13, "EAN-13"),
        new(PriceLabelSymbology.Ean8, "EAN-8"),
        new(PriceLabelSymbology.Code128, "Code 128 — أكثر من 13 رقم")
    ];
}
