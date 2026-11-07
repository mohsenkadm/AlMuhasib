using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public sealed class BarcodePrintingRow
{
    public required Product Product { get; init; }
    public string Name => Product.Name;
    public string Barcode => ProductBarcodeHelper.Primary(Product.Barcode) ?? Product.Barcode ?? string.Empty;
    public string CategoryName { get; init; } = "—";
    public string PriceText { get; init; } = "—";
    public decimal? Price { get; init; }
    public bool HasBarcode => !string.IsNullOrWhiteSpace(Barcode);
}

public partial class BarcodePrintingViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IProductPriceService _productPriceService;
    private readonly IBarcodeLabelService _barcodeLabelService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;

    private List<BarcodePrintingRow> _allRows = [];
    private System.Timers.Timer? _debounceTimer;
    private BarcodePrintingRow? _printRow;

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

    private readonly bool _pricingEnabled;

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
        _userPreferences = userPreferences;
        _pricingEnabled = userPreferences.Current.FeatureFlags.ProductPricingEnabled;
        PageTitle = "طباعة الباركودات";
        SelectedLabelSize = LabelSizeOptions.FirstOrDefault(o => o.Size == BarcodeLabelSize.Mm38x25)
                            ?? LabelSizeOptions[0];
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, ScreenPermissionRegistry.BarcodePrinting);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        Categories.Clear();
        foreach (var c in (await _categoryService.GetVisibleInCurrentBranchAsync()).OrderBy(x => x.Name))
            Categories.Add(c);

        var products = (await _productService.GetVisibleInCurrentBranchAsync()).OrderBy(p => p.Name).ToList();
        var categoryNames = Categories.ToDictionary(c => c.Id, c => c.Name);

        Dictionary<int, decimal?> prices = [];
        if (_pricingEnabled && products.Count > 0)
        {
            var priceRows = await _productPriceService.GetByProductIdsAsync(products.Select(p => p.Id));
            prices = priceRows
                .GroupBy(p => p.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (decimal?)(g.FirstOrDefault(x => x.PricingType?.IsDefault == true)?.SalePrice
                                    ?? g.FirstOrDefault()?.SalePrice));
        }

        _allRows = products.Select(p =>
        {
            prices.TryGetValue(p.Id, out var price);
            categoryNames.TryGetValue(p.CategoryId, out var catName);
            return new BarcodePrintingRow
            {
                Product = p,
                CategoryName = string.IsNullOrWhiteSpace(catName) ? (p.Category?.Name ?? "—") : catName!,
                Price = price is > 0 ? price : null,
                PriceText = price is > 0 ? $"{price:N0} د.ع" : "—"
            };
        }).ToList();

        TotalProducts = _allRows.Count;
        WithBarcodeCount = _allRows.Count(r => r.HasBarcode);
        WithoutBarcodeCount = TotalProducts - WithBarcodeCount;
        ApplyFilters();
        StatusMessage = $"منتجات الفرع الحالي: {TotalProducts:N0}";
    }

    private void ApplyFilters()
    {
        IEnumerable<BarcodePrintingRow> query = _allRows;

        if (OnlyWithBarcode)
            query = query.Where(r => r.HasBarcode);

        if (SelectedCategory is not null)
            query = query.Where(r => r.Product.CategoryId == SelectedCategory.Id);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(r =>
                r.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();
        FilteredCount = list.Count;
        Rows.Clear();
        foreach (var row in list)
            Rows.Add(row);
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(250);
        _debounceTimer.Elapsed += (_, _) =>
        {
            _debounceTimer?.Stop();
            Application.Current.Dispatcher.Invoke(ApplyFilters);
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    partial void OnSelectedCategoryChanged(Category? value) => ApplyFilters();
    partial void OnOnlyWithBarcodeChanged(bool value) => ApplyFilters();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            SearchText = string.Empty;
            SelectedCategory = null;
            OnlyWithBarcode = true;
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
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
