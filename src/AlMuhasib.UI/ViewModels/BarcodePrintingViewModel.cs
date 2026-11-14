using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Models;
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
    private readonly IUserPreferencesService _userPreferences;
    private readonly bool _pricingEnabled;

    private System.Timers.Timer? _debounceTimer;
    private System.Timers.Timer? _previewTimer;
    private BarcodePrintingRow? _printRow;
    private bool _suppressReload;
    private bool _suppressCustomSizeSync;
    private bool _suppressDesign;
    private int _loadRequestId;

    public ObservableCollection<BarcodePrintingRow> Rows { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<string> AvailableBarcodes { get; } = [];
    public ObservableCollection<BarcodePriceOption> PriceOptions { get; } = [];
    public ObservableCollection<BitmapSource> SheetPages { get; } = [];

    public IReadOnlyList<BarcodeLabelSizeOption> LabelSizeOptions { get; } = BarcodeLabelSizeOption.CreateAll();
    public IReadOnlyList<PriceLabelPaperOption> PaperOptions { get; } = PriceLabelPaperOption.CreateAll();
    public IReadOnlyList<PriceLabelSymbologyOption> SymbologyOptions { get; } = PriceLabelSymbologyOption.CreateAll();

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
    [ObservableProperty] private double _marginLeftMm;
    [ObservableProperty] private double _marginRightMm;
    [ObservableProperty] private double _marginTopMm;
    [ObservableProperty] private double _marginBottomMm;
    [ObservableProperty] private int _columns = 3;
    [ObservableProperty] private double _rowSpacingMm = 2;
    [ObservableProperty] private double _columnSpacingMm = 2;
    [ObservableProperty] private PriceLabelPaperOption? _selectedPaper;
    [ObservableProperty] private PriceLabelSymbologyOption? _selectedSymbology;
    [ObservableProperty] private bool _showSkuOnLabel = true;
    [ObservableProperty] private bool _showBarcodeGraphic = true;
    [ObservableProperty] private bool _showTaxInclusivePrice;
    [ObservableProperty] private bool _showBorder = true;
    [ObservableProperty] private double _productNameFontSize = 12;
    [ObservableProperty] private double _priceFontSize = 16;
    [ObservableProperty] private double _barcodeHeightMm = 12;
    [ObservableProperty] private double _taxPercent;
    [ObservableProperty] private string? _barcodeValidationMessage;
    [ObservableProperty] private string? _fitWarning;
    [ObservableProperty] private bool _canPrintSheet = true;
    [ObservableProperty] private string _previewSummary = string.Empty;

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
        _userPreferences = userPreferences;
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

        _printRow = row;
        PrintProductName = row.Name;
        LoadSavedDesign();
        _suppressDesign = true;
        try
        {
            PrintCopies = 1;
            AvailableBarcodes.Clear();
            foreach (var code in ProductBarcodeHelper.Split(row.BarcodeField))
                AvailableBarcodes.Add(code);
            if (AvailableBarcodes.Count == 0 && !string.IsNullOrWhiteSpace(row.Barcode))
                AvailableBarcodes.Add(row.Barcode);

            HasMultipleBarcodes = AvailableBarcodes.Count > 1;
            HasSingleBarcode = !HasMultipleBarcodes;
            SelectedBarcode = AvailableBarcodes.FirstOrDefault();
            await LoadPriceOptionsAsync(row.ProductId, row.Price);
        }
        finally
        {
            _suppressDesign = false;
        }

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
        SheetPages.Clear();
        AvailableBarcodes.Clear();
        PriceOptions.Clear();
    }

    [RelayCommand]
    private void ChangePrintProduct() => ClosePrintDialog();

    [RelayCommand]
    private void IncreaseCopies()
    {
        if (PrintCopies < 200)
            PrintCopies++;
    }

    [RelayCommand]
    private void DecreaseCopies()
    {
        if (PrintCopies > 1)
            PrintCopies--;
    }

    [RelayCommand]
    private void SaveLabelDesign()
    {
        var design = CaptureDesign();
        _userPreferences.Update(p => p.PriceLabelDesign = design);
        StatusMessage = "تم حفظ إعدادات بطاقة السعر";
    }

    [RelayCommand]
    private void RefreshPrintPreview() => RefreshPreview();

    [RelayCommand]
    private void RestorePrintDefaults()
    {
        _suppressDesign = true;
        try
        {
            ApplyDesign(new PriceLabelDesign());
            PrintCopies = 1;
        }
        finally
        {
            _suppressDesign = false;
        }

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

        var design = CaptureDesign();
        var ean = EanBarcode.Normalize(SelectedBarcode, design.Symbology);
        if (design.ShowBarcode && !ean.IsValid)
        {
            BarcodeValidationMessage = ean.Error;
            BeautifulMessageDialog.ShowWarning(ean.Error ?? "الباركود غير صالح للنوع المختار ولن يُطبع.");
            return;
        }

        var fit = design.Measure(PrintCopies);
        if (fit.PlacedCards == 0)
        {
            FitWarning = fit.Warning;
            BeautifulMessageDialog.ShowWarning(fit.Warning ?? "البطاقة لا تتسع في الورقة.");
            return;
        }

        var printed = _barcodeLabelService.PrintSheet(BuildLabelItem(design), design, PrintCopies);
        if (!printed)
            return;

        IsPrintDialogOpen = false;
        StatusMessage = $"تمت طباعة {PrintCopies} بطاقة للمنتج {PrintProductName}";
    }

    partial void OnSelectedLabelSizeChanged(BarcodeLabelSizeOption? value)
    {
        if (_suppressCustomSizeSync || _suppressDesign) return;
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

        RequestPreview();
    }

    partial void OnCustomWidthMmChanged(double value)
    {
        if (_suppressCustomSizeSync || _suppressDesign) return;
        EnsureCustomSizeSelected();
        RequestPreview();
    }

    partial void OnCustomHeightMmChanged(double value)
    {
        if (_suppressCustomSizeSync || _suppressDesign) return;
        EnsureCustomSizeSelected();
        RequestPreview();
    }

    private BarcodeLabelSizeOption MatchPreset(double width, double height)
    {
        foreach (var option in LabelSizeOptions)
        {
            if (option.IsCustom)
                continue;
            var (w, h) = BarcodeLabelSizeHelper.GetMillimeters(option.Size);
            if (Math.Abs(w - width) < 0.2 && Math.Abs(h - height) < 0.2)
                return option;
        }

        return LabelSizeOptions.First(o => o.IsCustom);
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

    partial void OnSelectedBarcodeChanged(string? value) => RequestPreview();
    partial void OnSelectedPriceOptionChanged(BarcodePriceOption? value)
    {
        PrintPriceText = value?.SalePrice is > 0 ? $"{value.SalePrice:N0} د.ع" : "—";
        RequestPreview();
    }

    partial void OnPrintCopiesChanged(int value) => RequestPreview();
    partial void OnGapMmChanged(double value) => RequestPreview();
    partial void OnShowProductNameOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowBranchNameOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowCompanyNameOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowPriceOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowBarcodeNumberOnLabelChanged(bool value) => RequestPreview();
    partial void OnMarginLeftMmChanged(double value) => RequestPreview();
    partial void OnMarginRightMmChanged(double value) => RequestPreview();
    partial void OnMarginTopMmChanged(double value) => RequestPreview();
    partial void OnMarginBottomMmChanged(double value) => RequestPreview();
    partial void OnColumnsChanged(int value) => RequestPreview();
    partial void OnRowSpacingMmChanged(double value) => RequestPreview();
    partial void OnColumnSpacingMmChanged(double value) => RequestPreview();
    partial void OnSelectedPaperChanged(PriceLabelPaperOption? value) => RequestPreview();
    partial void OnSelectedSymbologyChanged(PriceLabelSymbologyOption? value) => RequestPreview();
    partial void OnShowSkuOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowBarcodeGraphicChanged(bool value) => RequestPreview();
    partial void OnShowTaxInclusivePriceChanged(bool value) => RequestPreview();
    partial void OnShowBorderChanged(bool value) => RequestPreview();
    partial void OnProductNameFontSizeChanged(double value) => RequestPreview();
    partial void OnPriceFontSizeChanged(double value) => RequestPreview();
    partial void OnBarcodeHeightMmChanged(double value) => RequestPreview();
    partial void OnTaxPercentChanged(double value) => RequestPreview();

    private void RequestPreview()
    {
        if (_suppressDesign || !IsPrintDialogOpen)
            return;

        _previewTimer?.Stop();
        _previewTimer?.Dispose();
        _previewTimer = new System.Timers.Timer(90);
        _previewTimer.AutoReset = false;
        _previewTimer.Elapsed += (_, _) =>
        {
            _previewTimer?.Stop();
            Application.Current?.Dispatcher.Invoke(RefreshPreview);
        };
        _previewTimer.Start();
    }

    private void RefreshPreview()
    {
        if (string.IsNullOrWhiteSpace(SelectedBarcode))
        {
            PreviewImage = null;
            SheetPages.Clear();
            BarcodeValidationMessage = "لا يوجد باركود للمعاينة.";
            CanPrintSheet = false;
            return;
        }

        var design = CaptureDesign();
        var ean = EanBarcode.Normalize(SelectedBarcode, design.Symbology);
        BarcodeValidationMessage = design.ShowBarcode && !ean.IsValid ? ean.Error : null;
        var marginsFit = design.MarginLeftMm + design.MarginRightMm < design.LabelWidthMm - 4
                         && design.MarginTopMm + design.MarginBottomMm < design.LabelHeightMm - 4;
        FitWarning = marginsFit ? null : "الهوامش أكبر من مساحة البطاقة.";
        CanPrintSheet = marginsFit && string.IsNullOrWhiteSpace(BarcodeValidationMessage);
        PreviewSummary = "معاينة البطاقة";

        var png = _barcodeLabelService.CreateSingleLabelPreviewPng(BuildLabelItem(design), design);
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

    private void LoadSavedDesign()
    {
        var saved = _userPreferences.Current.PriceLabelDesign?.Clone() ?? new PriceLabelDesign();
        _suppressDesign = true;
        try
        {
            ApplyDesign(saved);
        }
        finally
        {
            _suppressDesign = false;
        }
    }

    private void ApplyDesign(PriceLabelDesign design)
    {
        design.Normalize();
        MarginLeftMm = design.MarginLeftMm;
        MarginRightMm = design.MarginRightMm;
        MarginTopMm = design.MarginTopMm;
        MarginBottomMm = design.MarginBottomMm;
        CustomWidthMm = design.LabelWidthMm;
        CustomHeightMm = design.LabelHeightMm;
        SelectedLabelSize = MatchPreset(design.LabelWidthMm, design.LabelHeightMm);
        IsCustomSizeSelected = SelectedLabelSize?.IsCustom == true;
        RowSpacingMm = design.RowSpacingMm;
        ColumnSpacingMm = design.ColumnSpacingMm;
        SelectedPaper = PaperOptions.FirstOrDefault(o => o.Paper == design.Paper) ?? PaperOptions[0];
        SelectedSymbology = SymbologyOptions.FirstOrDefault(o => o.Symbology == design.Symbology) ?? SymbologyOptions[0];
        ShowProductNameOnLabel = design.ShowProductName;
        ShowPriceOnLabel = design.ShowPrice;
        ShowSkuOnLabel = design.ShowSku;
        ShowBarcodeGraphic = design.ShowBarcode;
        ShowTaxInclusivePrice = design.ShowTaxInclusivePrice;
        ShowBorder = design.ShowBorder;
        ProductNameFontSize = design.ProductNameFontSize;
        PriceFontSize = design.PriceFontSize;
        BarcodeHeightMm = design.BarcodeHeightMm;
        TaxPercent = design.TaxPercent;
        ShowBarcodeNumberOnLabel = true;
    }

    private PriceLabelDesign CaptureDesign()
    {
        var design = new PriceLabelDesign
        {
            MarginLeftMm = MarginLeftMm,
            MarginRightMm = MarginRightMm,
            MarginTopMm = MarginTopMm,
            MarginBottomMm = MarginBottomMm,
            Columns = 8,
            LabelWidthMm = CustomWidthMm,
            LabelHeightMm = CustomHeightMm,
            RowSpacingMm = RowSpacingMm,
            ColumnSpacingMm = ColumnSpacingMm,
            Paper = SelectedPaper?.Paper ?? PriceLabelPaper.A4,
            Symbology = SelectedSymbology?.Symbology ?? PriceLabelSymbology.Ean13,
            ShowProductName = ShowProductNameOnLabel,
            ShowPrice = ShowPriceOnLabel,
            ShowSku = ShowSkuOnLabel,
            ShowBarcode = ShowBarcodeGraphic,
        ShowTaxInclusivePrice = false,
        ShowBorder = ShowBorder,
        ProductNameFontSize = ProductNameFontSize,
        PriceFontSize = PriceFontSize,
        BarcodeHeightMm = BarcodeHeightMm,
        TaxPercent = 0
        };
        design.Normalize();
        return design;
    }

    private BarcodeLabelItem BuildLabelItem(PriceLabelDesign design) => new()
    {
        ProductName = PrintProductName,
        Barcode = SelectedBarcode ?? string.Empty,
        Sku = _printRow is null ? string.Empty : _printRow.ProductId.ToString(),
        Price = SelectedPriceOption?.SalePrice ?? _printRow?.Price,
        ShowProductName = design.ShowProductName,
        ShowPrice = design.ShowPrice,
        ShowSku = design.ShowSku,
        ShowBarcodeGraphic = design.ShowBarcode,
        ShowTaxInclusivePrice = design.ShowTaxInclusivePrice,
        ShowBorder = design.ShowBorder,
        TaxPercent = design.TaxPercent,
        ProductNameFontSize = design.ProductNameFontSize,
        PriceFontSize = design.PriceFontSize,
        BarcodeHeightMm = design.BarcodeHeightMm,
        Symbology = design.Symbology,
        ShowBarcodeNumber = true
    };
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
        new(PriceLabelSymbology.Ean8, "EAN-8")
    ];
}
