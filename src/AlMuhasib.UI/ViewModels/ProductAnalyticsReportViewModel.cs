using System.Collections.ObjectModel;
using System.Windows.Threading;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.Shared.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductAnalyticsReportViewModel : ReportViewModelBase
{
    private readonly IProductAnalyticsService _analytics;
    private ProductAnalyticsReportResult _result = new();
    private List<ProductAnalyticsRow> _allRows = [];

    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private int? _selectedCategoryId;
    [ObservableProperty] private int? _selectedProductId;
    [ObservableProperty] private int? _selectedSupplierId;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ActivityOption _selectedActivity;
    [ObservableProperty] private SortOption _selectedSort;
    [ObservableProperty] private int _chartTopCount = 10;
    [ObservableProperty] private bool _trendByQuantity;
    [ObservableProperty] private int _topMetric;
    [ObservableProperty] private bool _categoryByQuantity;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private string _currencyNote = string.Empty;
    [ObservableProperty] private string _profitNote = string.Empty;
    [ObservableProperty] private string _stockAgeNote = string.Empty;
    [ObservableProperty] private string _tableTotals = string.Empty;

    [ObservableProperty] private string _productCountText = "0";
    [ObservableProperty] private string _soldQuantityText = "0";
    [ObservableProperty] private string _salesText = "0";
    [ObservableProperty] private string _purchasesText = "0";
    [ObservableProperty] private string _profitText = "0";
    [ObservableProperty] private string _marginText = "0%";
    [ObservableProperty] private string _returnsText = "0";
    [ObservableProperty] private string _stockValueText = "0";
    [ObservableProperty] private string _topSellerText = "—";
    [ObservableProperty] private string _stagnantText = "0";
    [ObservableProperty] private string _profitHint = "مجمل الربح المحاسبي";
    [ObservableProperty] private decimal? _salesChange;
    [ObservableProperty] private decimal? _quantityChange;
    [ObservableProperty] private decimal? _purchaseChange;
    [ObservableProperty] private decimal? _profitChange;
    [ObservableProperty] private decimal? _returnChange;
    [ObservableProperty] private decimal? _stagnantChange;
    [ObservableProperty] private string? _comparisonLabel;

    [ObservableProperty] private ISeries[] _trendSeries = [];
    [ObservableProperty] private Axis[] _trendXAxes = [];
    [ObservableProperty] private Axis[] _trendYAxes = [];
    [ObservableProperty] private ISeries[] _topSeries = [];
    [ObservableProperty] private Axis[] _topXAxes = [];
    [ObservableProperty] private Axis[] _topYAxes = [];
    [ObservableProperty] private ISeries[] _categorySeries = [];
    [ObservableProperty] private ISeries[] _profitSeries = [];
    [ObservableProperty] private Axis[] _profitXAxes = [];
    [ObservableProperty] private Axis[] _profitYAxes = [];
    [ObservableProperty] private ISeries[] _stockSeries = [];
    [ObservableProperty] private ISeries[] _compareSeries = [];
    [ObservableProperty] private Axis[] _compareXAxes = [];
    [ObservableProperty] private Axis[] _compareYAxes = [];
    [ObservableProperty] private ISeries[] _categoryProfitSeries = [];
    [ObservableProperty] private Axis[] _categoryProfitXAxes = [];
    [ObservableProperty] private Axis[] _categoryProfitYAxes = [];
    [ObservableProperty] private ISeries[] _categoryStockSeries = [];
    [ObservableProperty] private Axis[] _categoryStockXAxes = [];
    [ObservableProperty] private Axis[] _categoryStockYAxes = [];
    [ObservableProperty] private ISeries[] _categoryCountSeries = [];
    [ObservableProperty] private Axis[] _categoryCountXAxes = [];
    [ObservableProperty] private Axis[] _categoryCountYAxes = [];
    [ObservableProperty] private ISeries[] _categoryPurchaseSeries = [];
    [ObservableProperty] private Axis[] _categoryPurchaseXAxes = [];
    [ObservableProperty] private Axis[] _categoryPurchaseYAxes = [];
    [ObservableProperty] private string _categoryCountText = "0";
    [ObservableProperty] private string _topCategorySalesText = "—";
    [ObservableProperty] private string _topCategorySalesHint = "لا مبيعات في الفترة";
    [ObservableProperty] private string _topCategoryProfitText = "—";
    [ObservableProperty] private string _topCategoryProfitHint = "لا ربح محسوب";
    [ObservableProperty] private string _topCategoryStockText = "—";
    [ObservableProperty] private string _topCategoryStockHint = "لا قيمة مخزون";

    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<ProductAnalyticsRow> Rows { get; } = [];
    public ObservableCollection<OperationalInsightCard> Insights { get; } = [];
    public ObservableCollection<ProductAnalyticsCategoryStat> CategoryStats { get; } = [];
    public ActivityOption[] Activities { get; } =
    [
        new(ProductAnalyticsActivity.All, "كل المنتجات"),
        new(ProductAnalyticsActivity.WithMovement, "تحركت خلال الفترة"),
        new(ProductAnalyticsActivity.NoSales, "بدون مبيعات"),
        new(ProductAnalyticsActivity.Stagnant, "راكدة وفيها رصيد")
    ];
    public SortOption[] Sorts { get; } =
    [
        new("المبيعات", nameof(ProductAnalyticsRow.SalesAmount), true),
        new("صافي الكمية", nameof(ProductAnalyticsRow.NetQuantitySold), true),
        new("الربح", nameof(ProductAnalyticsRow.GrossProfit), true),
        new("هامش الربح", nameof(ProductAnalyticsRow.MarginPercent), true),
        new("اسم المنتج", nameof(ProductAnalyticsRow.ProductName), false)
    ];
    public int[] TopCounts { get; } = [10, 15];

    public ProductAnalyticsReportViewModel(
        IProductAnalyticsService analytics,
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _analytics = analytics;
        PageTitle = "تقرير المنتجات";
        _selectedActivity = Activities[0];
        _selectedSort = Sorts[0];
        RegisterThemeChartReload(async () =>
        {
            RebuildCharts();
            await Task.CompletedTask;
        });
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        Warehouses.Clear();
        Categories.Clear();
        Suppliers.Clear();
        foreach (var warehouse in await _unitOfWork.Warehouses.GetAllAsync())
            Warehouses.Add(warehouse);
        foreach (var category in await _unitOfWork.Categories.GetAllAsync())
            Categories.Add(category);
        foreach (var supplier in await _unitOfWork.Suppliers.GetAllAsync())
            Suppliers.Add(supplier);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var query = BuildQuery();
            _result = await Task.Run(() => _analytics.GetReportAsync(query));
            _allRows = _result.Rows.ToList();
            ApplySort();
            BindSummary();
            RebuildCharts();
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

    [RelayCommand]
    private async Task ResetFiltersAsync()
    {
        DateFrom = DateTime.Today.AddMonths(-1);
        DateTo = DateTime.Today;
        SelectedWarehouseId = null;
        SelectedCategoryId = null;
        SelectedProductId = null;
        SelectedSupplierId = null;
        SearchText = string.Empty;
        SelectedActivity = Activities[0];
        SelectedSort = Sorts[0];
        ChartTopCount = 10;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task OpenProductAsync(ProductAnalyticsRow? row)
    {
        if (row is null)
            return;
        try
        {
            IsBusy = true;
            var detail = await _analytics.GetDetailAsync(row.ProductId, DateFrom, DateTo, SelectedWarehouseId);
            var window = new AlMuhasib.UI.Views.ProductAnalyticsDetailWindow(detail)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
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

    [RelayCommand]
    private void ExportToExcel() => Export(fullReport: false);

    [RelayCommand]
    private void ExportFullToExcel() => Export(fullReport: true);

    [RelayCommand]
    private void ExportToPdf()
    {
        var model = new StatementPrintModel
        {
            Title = "تقرير المنتجات",
            PartyName = "تحليل المنتجات",
            FromDate = DateFrom,
            ToDate = DateTo,
            Columns = TableColumns(),
            Rows = TableRows(),
            SummaryLines = SummaryLines()
        };
        var path = _exportService.ExportStatementToPdf(model);
        BeautifulMessageDialog.ShowSuccess($"تم إنشاء ملف PDF:\n{path}");
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        try
        {
            IsBusy = true;
            await Dispatcher.Yield(DispatcherPriority.Render);
            var document = ProductAnalyticsReportDocumentBuilder.Build(BuildPrintModel());
            IsBusy = false;
            DocumentPrintHelper.PrintWithPreview(
                document,
                "تقرير المنتجات",
                InvoicePaperSizes.GetPageSize(InvoicePaperSizes.A4Landscape));
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

    partial void OnSelectedSortChanged(SortOption value) => ApplySort();
    partial void OnTrendByQuantityChanged(bool value) => RebuildCharts();
    partial void OnTopMetricChanged(int value) => RebuildCharts();
    partial void OnCategoryByQuantityChanged(bool value) => RebuildCharts();
    partial void OnChartTopCountChanged(int value) => RebuildCharts();

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    private ProductAnalyticsPrintModel BuildPrintModel() => new()
    {
        From = DateFrom ?? DateTime.Today,
        To = DateTo ?? DateTime.Today,
        WarehouseName = LookupName(Warehouses, SelectedWarehouseId, "كل المستودعات"),
        CategoryName = LookupName(Categories, SelectedCategoryId, "كل التصنيفات"),
        SupplierName = LookupName(Suppliers, SelectedSupplierId, "كل الموردين"),
        ActivityName = SelectedActivity.Label,
        SearchText = SearchText?.Trim() ?? string.Empty,
        CurrencyNote = _result.CurrencyNote,
        ProfitNote = _result.ProfitNote,
        StockAgeNote = _result.StockAgeNote,
        Result = _result
    };

    private static string LookupName<T>(IEnumerable<T> items, int? id, string fallback)
        where T : class
    {
        if (id is null)
            return fallback;
        foreach (var item in items)
        {
            var itemId = item switch
            {
                Warehouse warehouse => warehouse.Id,
                Category category => category.Id,
                Supplier supplier => supplier.Id,
                _ => (int?)null
            };
            if (itemId != id)
                continue;
            var name = item switch
            {
                Warehouse warehouse => warehouse.Name,
                Category category => category.Name,
                Supplier supplier => supplier.Name,
                _ => null
            };
            return string.IsNullOrWhiteSpace(name) ? fallback : name;
        }
        return fallback;
    }

    private ProductAnalyticsQuery BuildQuery() => new()
    {
        From = DateFrom,
        To = DateTo,
        WarehouseId = SelectedWarehouseId,
        CategoryId = SelectedCategoryId,
        ProductId = SelectedProductId,
        SupplierId = SelectedSupplierId,
        Search = SearchText,
        Activity = SelectedActivity.Value,
        TopCount = ChartTopCount
    };

    private void BindSummary()
    {
        ProductCountText = _result.ProductCount.ToString("N0");
        SoldQuantityText = _result.GrossQuantitySold.ToString("N0");
        SalesText = FormatCurrency(_result.NetSales);
        PurchasesText = FormatCurrency(_result.PurchaseValue);
        ProfitText = FormatCurrency(_result.GrossProfit);
        MarginText = $"{_result.MarginPercent:N1}%";
        ReturnsText = FormatCurrency(_result.ReturnValue);
        StockValueText = FormatCurrency(_result.StockValue);
        TopSellerText = _result.TopSellerName;
        StagnantText = _result.StagnantCount.ToString("N0");
        ProfitHint = _result.MissingCostCount > 0
            ? $"مجمل الربح. {_result.MissingCostCount} منتجاً بلا تكلفة مسجلة، وربحها يساوي صافي المبيعات."
            : "مجمل الربح = صافي المبيعات − تكلفة البضاعة المباعة";
        CurrencyNote = _result.CurrencyNote;
        ProfitNote = _result.ProfitNote;
        StockAgeNote = _result.StockAgeNote ?? string.Empty;
        ComparisonLabel = _result.HasPreviousPeriod ? "مقارنة بالفترة السابقة" : null;
        SalesChange = _result.SalesChangePercent;
        QuantityChange = _result.QuantityChangePercent;
        PurchaseChange = _result.PurchaseChangePercent;
        ProfitChange = _result.ProfitChangePercent;
        ReturnChange = _result.ReturnChangePercent;
        StagnantChange = _result.StagnantChangePercent;
        TableTotals =
            $"إجمالي الجدول: مبيعات {_result.NetSales:N0} · تكلفة {_result.Cogs:N0} · ربح {_result.GrossProfit:N0} · مخزون {_result.StockValue:N0}";
        HasData = _result.Rows.Count > 0 || _result.Trend.Count > 0;
        Insights.Clear();
        foreach (var group in _result.Insights)
            Insights.Add(OperationalInsightCard.From(group));
        CategoryStats.Clear();
        foreach (var category in _result.Categories)
            CategoryStats.Add(category);
        BindCategorySummary();
    }

    private void BindCategorySummary()
    {
        CategoryCountText = _result.Categories.Count.ToString("N0");
        var bySales = _result.Categories.OrderByDescending(c => c.SalesAmount).FirstOrDefault();
        var byProfit = _result.Categories.OrderByDescending(c => c.GrossProfit).FirstOrDefault();
        var byStock = _result.Categories.OrderByDescending(c => c.StockValue).FirstOrDefault();
        TopCategorySalesText = bySales is null || bySales.SalesAmount == 0 ? "—" : bySales.Name;
        TopCategorySalesHint = bySales is null || bySales.SalesAmount == 0
            ? "لا مبيعات في الفترة"
            : $"صافي المبيعات {FormatCurrency(bySales.SalesAmount)}";
        TopCategoryProfitText = byProfit is null || byProfit.GrossProfit == 0 ? "—" : byProfit.Name;
        TopCategoryProfitHint = byProfit is null || byProfit.GrossProfit == 0
            ? "لا ربح محسوب"
            : $"مجمل الربح {FormatCurrency(byProfit.GrossProfit)} · الهامش {byProfit.MarginPercent:N1}%";
        TopCategoryStockText = byStock is null || byStock.StockValue == 0 ? "—" : byStock.Name;
        TopCategoryStockHint = byStock is null || byStock.StockValue == 0
            ? "لا قيمة مخزون"
            : $"قيمة المخزون {FormatCurrency(byStock.StockValue)}";
    }

    private void ApplySort()
    {
        if (_allRows.Count == 0)
        {
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
            return;
        }

        _allRows = SelectedSort.Descending
            ? _allRows.OrderByDescending(ValueOf).ThenBy(r => r.ProductName).ToList()
            : _allRows.OrderBy(ValueOf).ThenBy(r => r.ProductName).ToList();
        CurrentPage = 1;
        UpdatePaginationWithFilters(_allRows, Rows);
    }

    private object ValueOf(ProductAnalyticsRow row) => SelectedSort.Property switch
    {
        nameof(ProductAnalyticsRow.NetQuantitySold) => row.NetQuantitySold,
        nameof(ProductAnalyticsRow.GrossProfit) => row.GrossProfit,
        nameof(ProductAnalyticsRow.MarginPercent) => row.MarginPercent,
        nameof(ProductAnalyticsRow.ProductName) => row.ProductName,
        _ => row.SalesAmount
    };

    private void RebuildCharts()
    {
        var trend = _result.Trend;
        if (TrendByQuantity)
        {
            TrendSeries =
            [
                ChartThemeConfig.Line(trend.Select(p => p.SalesQuantity).ToArray(), "صافي الكمية", 0),
                ChartThemeConfig.Line(trend.Select(p => p.PreviousSalesQuantity).ToArray(), "الفترة السابقة", 3)
            ];
            TrendYAxes = [ChartThemeConfig.CreateYAxis(suffix: "", forceNonNegative: false)];
        }
        else
        {
            TrendSeries =
            [
                ChartThemeConfig.Line(trend.Select(p => p.SalesAmount).ToArray(), "صافي المبيعات", 0),
                ChartThemeConfig.Line(trend.Select(p => p.PreviousSalesAmount).ToArray(), "الفترة السابقة", 3)
            ];
            TrendYAxes = [ChartThemeConfig.CreateYAxis(forceNonNegative: false)];
        }
        TrendXAxes = [ChartThemeConfig.CreateXAxis(trend.Select(p => p.Label).ToArray(), trend.Count > 12 ? -35 : 0)];

        var ranked = TopMetric switch
        {
            1 => _allRows.OrderByDescending(r => r.SalesAmount),
            2 => _allRows.OrderByDescending(r => r.GrossProfit),
            _ => _allRows.OrderByDescending(r => r.NetQuantitySold)
        };
        var top = ranked.Take(Math.Max(1, ChartTopCount)).ToList();
        var values = TopMetric switch
        {
            1 => top.Select(r => r.SalesAmount).ToArray(),
            2 => top.Select(r => r.GrossProfit).ToArray(),
            _ => top.Select(r => r.NetQuantitySold).ToArray()
        };
        var names = top.Select(r => Trim(r.ProductName)).ToArray();
        TopSeries =
        [
            new RowSeries<decimal>
            {
                Values = values,
                Name = TopMetric switch { 1 => "المبيعات", 2 => "الربح", _ => "الكمية" },
                Fill = new SolidColorPaint(ChartThemeConfig.PrimaryBlue),
                MaxBarWidth = 18,
                Padding = 4
            }
        ];
        TopXAxes = [ChartThemeConfig.CreateYAxis(TopMetric == 0 ? "" : "د.ع", forceNonNegative: false)];
        TopYAxes =
        [
            new Axis
            {
                Labels = names,
                IsInverted = true,
                TextSize = ChartThemeConfig.LabelSize,
                LabelsPaint = new SolidColorPaint(ChartThemeConfig.LabelColor)
                {
                    SKTypeface = ChartThemeConfig.ArabicTypeface
                },
                SeparatorsPaint = ChartThemeConfig.CreateGridPaint()
            }
        ];

        CategorySeries = ChartThemeConfig.PieFromNameAmount(
            CategoryByQuantity ? _result.CategoryByQuantity : _result.CategoryByAmount);

        var profitable = _allRows.OrderByDescending(r => r.GrossProfit).Take(10).ToList();
        ProfitSeries = [ChartThemeConfig.Column(profitable.Select(r => r.GrossProfit).ToArray(), "الربح", 2)];
        ProfitXAxes = [ChartThemeConfig.CreateXAxis(profitable.Select(r => Trim(r.ProductName)).ToArray(), -35)];
        ProfitYAxes = [ChartThemeConfig.CreateYAxis(forceNonNegative: false)];

        StockSeries = ChartThemeConfig.PieFromNameAmount(_result.StockStatus.Where(p => p.Amount > 0).ToList());
        CompareSeries =
        [
            ChartThemeConfig.Column(trend.Select(p => p.SalesAmount).ToArray(), "المبيعات", 0),
            ChartThemeConfig.Column(trend.Select(p => p.PurchaseAmount).ToArray(), "المشتريات", 3)
        ];
        CompareXAxes = [ChartThemeConfig.CreateXAxis(trend.Select(p => p.Label).ToArray(), trend.Count > 12 ? -35 : 0)];
        CompareYAxes = [ChartThemeConfig.CreateYAxis(forceNonNegative: false)];

        var categories = _result.Categories;
        var profit = TopCategories(categories, c => c.GrossProfit);
        CategoryProfitSeries = [ChartThemeConfig.Column(profit.Values, "مجمل الربح", 2)];
        CategoryProfitXAxes = [ChartThemeConfig.CreateXAxis(profit.Names, -35)];
        CategoryProfitYAxes = [ChartThemeConfig.CreateYAxis(forceNonNegative: false)];

        var stock = TopCategories(categories, c => c.StockValue);
        CategoryStockSeries = [ChartThemeConfig.Column(stock.Values, "قيمة المخزون", 3)];
        CategoryStockXAxes = [ChartThemeConfig.CreateXAxis(stock.Names, -35)];
        CategoryStockYAxes = [ChartThemeConfig.CreateYAxis()];

        var counts = TopCategories(categories, c => c.ProductCount);
        CategoryCountSeries = [ChartThemeConfig.Column(counts.Values, "عدد المنتجات", 0)];
        CategoryCountXAxes = [ChartThemeConfig.CreateXAxis(counts.Names, -35)];
        CategoryCountYAxes = [ChartThemeConfig.CreateYAxis(suffix: "", forceNonNegative: true)];

        var purchases = TopCategories(categories, c => c.PurchaseValue);
        CategoryPurchaseSeries = [ChartThemeConfig.Column(purchases.Values, "المشتريات", 4)];
        CategoryPurchaseXAxes = [ChartThemeConfig.CreateXAxis(purchases.Names, -35)];
        CategoryPurchaseYAxes = [ChartThemeConfig.CreateYAxis(forceNonNegative: false)];
    }

    private static (string[] Names, decimal[] Values) TopCategories(
        IReadOnlyList<ProductAnalyticsCategoryStat> categories,
        Func<ProductAnalyticsCategoryStat, decimal> value)
    {
        var ordered = categories.Where(c => value(c) != 0).OrderByDescending(value).ToList();
        if (ordered.Count <= 8)
            return (ordered.Select(c => Trim(c.Name)).ToArray(), ordered.Select(value).ToArray());

        var head = ordered.Take(7).ToList();
        var names = head.Select(c => Trim(c.Name)).Append("أخرى").ToArray();
        var values = head.Select(value).Append(ordered.Skip(7).Sum(value)).ToArray();
        return (names, values);
    }

    private void Export(bool fullReport)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = fullReport ? "تقرير_المنتجات_كامل.xlsx" : "تقرير_المنتجات.xlsx"
        };
        if (dialog.ShowDialog() != true)
            return;

        if (!fullReport)
        {
            _exportService.ExportToExcel(dialog.FileName, "المنتجات", TableColumns(), TableRows());
        }
        else
        {
            var summaryRows = SummaryLines().Select(line => new object[] { line }).ToList();
            _exportService.ExportToExcel(dialog.FileName,
            [
                ("الملخص", ["البيان"], summaryRows),
                ("المنتجات", TableColumns(), TableRows()),
                ("التصنيفات", CategoryColumns(), CategoryRows())
            ]);
        }
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    private static string[] CategoryColumns() =>
    [
        "التصنيف", "عدد المنتجات", "صافي الكمية", "المبيعات", "المشتريات",
        "المرتجعات", "الربح", "الهامش %", "قيمة المخزون", "منتجات راكدة"
    ];

    private List<object[]> CategoryRows() => _result.Categories.Select(c => new object[]
    {
        c.Name, c.ProductCount.ToString("N0"), c.NetQuantity.ToString("N2"),
        c.SalesAmount.ToString("N0"), c.PurchaseValue.ToString("N0"), c.ReturnValue.ToString("N0"),
        c.GrossProfit.ToString("N0"), c.MarginPercent.ToString("N1"), c.StockValue.ToString("N0"),
        c.StagnantCount.ToString("N0")
    }).ToList();

    private static string[] TableColumns() =>
    [
        "الرمز", "الباركود", "المنتج", "التصنيف", "الوحدة",
        "كمية الشراء", "كمية البيع", "المرتجعات", "صافي الكمية",
        "متوسط الشراء", "متوسط البيع", "المبيعات", "التكلفة", "الربح", "الهامش %",
        "الرصيد", "قيمة المخزون"
    ];

    private List<object[]> TableRows() => _allRows.Select(r => new object[]
    {
        r.Code, r.Barcode, r.ProductName, r.CategoryName, r.UnitName,
        r.PurchasedQuantity.ToString("N2"), r.SoldQuantity.ToString("N2"), r.ReturnQuantity.ToString("N2"),
        r.NetQuantitySold.ToString("N2"), r.AveragePurchasePriceDisplay, r.AverageSalePriceDisplay,
        r.SalesAmount.ToString("N0"), r.Cogs.ToString("N0"), r.GrossProfit.ToString("N0"),
        r.MarginPercent.ToString("N1"), r.StockQuantity.ToString("N2"), r.StockValue.ToString("N0")
    }).ToList();

    private List<string> SummaryLines()
    {
        var lines = new List<string>
        {
            $"من {DateFrom:yyyy/MM/dd} إلى {DateTo:yyyy/MM/dd}",
            $"المنتجات {_result.ProductCount:N0} · التصنيفات {_result.Categories.Count:N0} · الكمية المباعة {_result.GrossQuantitySold:N0} · صافي المبيعات {_result.NetSales:N0}",
            $"المشتريات {_result.PurchaseValue:N0} · المرتجعات {_result.ReturnValue:N0} · التكلفة {_result.Cogs:N0}",
            $"مجمل الربح {_result.GrossProfit:N0} · الهامش {_result.MarginPercent:N1}% · قيمة المخزون الحالي {_result.StockValue:N0}",
            _result.CurrencyNote,
            _result.ProfitNote
        };
        if (!string.IsNullOrWhiteSpace(_result.StockAgeNote))
            lines.Add(_result.StockAgeNote);
        return lines;
    }

    private static string Trim(string name) => name.Length <= 18 ? name : name[..18] + "…";

    public sealed class OperationalInsightCard
    {
        public string Title { get; init; } = string.Empty;
        public PackIconKind Icon { get; init; }
        public string Accent { get; init; } = "#1565C0";
        public string AccentSoft { get; init; } = "#E3F2FD";
        public IReadOnlyList<OperationalInsightLine> Lines { get; init; } = [];

        public static OperationalInsightCard From(ProductAnalyticsInsightGroup group)
        {
            var (icon, accent, soft) = Theme(group.Title);
            var lines = new List<OperationalInsightLine>();
            var rank = 1;
            foreach (var raw in group.Lines)
            {
                var parts = raw.Split(" — ", 2, StringSplitOptions.TrimEntries);
                var note = parts.Length == 1 && group.Lines.Count == 1 && raw.Length > 24;
                lines.Add(new OperationalInsightLine
                {
                    Rank = note ? string.Empty : rank.ToString(),
                    Name = parts[0],
                    Value = parts.Length > 1 ? parts[1] : string.Empty
                });
                if (!note)
                    rank++;
            }

            return new OperationalInsightCard
            {
                Title = group.Title,
                Icon = icon,
                Accent = accent,
                AccentSoft = soft,
                Lines = lines
            };
        }

        private static (PackIconKind Icon, string Accent, string Soft) Theme(string title) => title switch
        {
            "الأعلى كمية" => (PackIconKind.PackageVariant, "#00796B", "#E0F2F1"),
            "الأعلى مبيعاً" => (PackIconKind.CashRegister, "#2E7D32", "#E8F5E9"),
            "الأقل مبيعاً" => (PackIconKind.ChartLineVariant, "#EF6C00", "#FFF3E0"),
            "بدون مبيعات خلال الفترة" => (PackIconKind.PackageVariantClosed, "#C62828", "#FFEBEE"),
            "الأعلى ربحاً" => (PackIconKind.TrendingUp, "#1B5E20", "#E8F5E9"),
            "الأعلى هامشاً" => (PackIconKind.Percent, "#1565C0", "#E3F2FD"),
            "مبيعات مرتفعة وهامش منخفض" => (PackIconKind.AlertOutline, "#EF6C00", "#FFF3E0"),
            "ربح سالب" => (PackIconKind.TrendingDown, "#C62828", "#FFEBEE"),
            "وصلت حد إعادة الطلب" => (PackIconKind.BellRingOutline, "#EF6C00", "#FFF3E0"),
            "نافدة" => (PackIconKind.CloseCircleOutline, "#C62828", "#FFEBEE"),
            "مخزون مرتفع" => (PackIconKind.Warehouse, "#6A1B9A", "#F3E5F5"),
            "راكدة خلال الفترة" => (PackIconKind.TimerSand, "#C62828", "#FFEBEE"),
            "قيمة المخزون حسب التصنيف" => (PackIconKind.ShapeOutline, "#1565C0", "#E3F2FD"),
            "مبيعاتها ترتفع" => (PackIconKind.ArrowUpBold, "#2E7D32", "#E8F5E9"),
            "مبيعاتها تنخفض" => (PackIconKind.ArrowDownBold, "#C62828", "#FFEBEE"),
            _ => (PackIconKind.ChartBoxOutline, "#1565C0", "#E3F2FD")
        };
    }

    public sealed class OperationalInsightLine
    {
        public string Rank { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    }

    public sealed record ActivityOption(ProductAnalyticsActivity Value, string Label)
    {
        public override string ToString() => Label;
    }

    public sealed record SortOption(string Label, string Property, bool Descending)
    {
        public override string ToString() => Label;
    }
}
