using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace AlMuhasib.UI.ViewModels;

public partial class InventoryReplenishmentReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalProducts = "0";
    [ObservableProperty] private string _totalCurrentQuantity = "0";
    [ObservableProperty] private string _totalSoldQuantity = "0";
    [ObservableProperty] private string _totalSuggestedOrder = "0";
    [ObservableProperty] private string _itemsNeedingReplenishment = "0";
    [ObservableProperty] private string _estimatedOrderValue = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private decimal _minimumStock = 5;
    [ObservableProperty] private InventoryReplenishmentFilter _selectedFilter = InventoryReplenishmentFilter.All;

    [ObservableProperty] private ISeries[] _statusSeries = [];
    [ObservableProperty] private ISeries[] _reorderSeries = [];
    [ObservableProperty] private Axis[] _reorderXAxes = [];
    [ObservableProperty] private Axis[] _reorderYAxes = [];

    [ObservableProperty] private ISeries[] _comparisonSeries = [];
    [ObservableProperty] private Axis[] _comparisonXAxes = [];
    [ObservableProperty] private Axis[] _comparisonYAxes = [];

    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public List<ReplenishmentFilterItem> Filters { get; } =
    [
        new("جميع الأصناف", InventoryReplenishmentFilter.All),
        new("يحتاج توريد فقط", InventoryReplenishmentFilter.NeedsReplenishmentOnly)
    ];

    private List<InventoryReplenishmentRow> _allRows = [];
    public ObservableCollection<InventoryReplenishmentRow> Rows { get; } = [];

    public InventoryReplenishmentReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تقرير احتياج المخزون";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var w in await _unitOfWork.Warehouses.GetAllAsync())
            Warehouses.Add(w);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetInventoryReplenishmentReportAsync(
                DateFrom, DateTo, SelectedWarehouseId, MinimumStock, SelectedFilter);

            TotalProducts = result.TotalProducts.ToString("N0");
            TotalCurrentQuantity = result.TotalCurrentQuantity.ToString("N0");
            TotalSoldQuantity = result.TotalSoldQuantity.ToString("N0");
            TotalSuggestedOrder = result.TotalSuggestedOrderQuantity.ToString("N0");
            ItemsNeedingReplenishment = result.ItemsNeedingReplenishment.ToString("N0");
            EstimatedOrderValue = FormatCurrency(result.EstimatedOrderValue);

            StatusSeries = result.StatusChart.Count > 0
                ? ChartThemeConfig.PieFromNameAmount(result.StatusChart)
                : [];

            if (result.ReorderChart.Count > 0)
            {
                ReorderSeries = [ChartThemeConfig.Column(
                    result.ReorderChart.Select(c => c.Amount).ToArray(), "الكمية المقترحة", 3)];
                ReorderXAxes = [ChartThemeConfig.CreateXAxis(result.ReorderChart.Select(c => c.Name).ToArray(), -35)];
                ReorderYAxes = CreateQuantityYAxis();
            }
            else
            {
                ReorderSeries = [];
                ReorderXAxes = [];
                ReorderYAxes = CreateQuantityYAxis();
            }

            if (result.StockVsSoldChart.Count > 0)
            {
                var labels = result.StockVsSoldChart.Select(r => TruncateLabel(r.ProductName)).ToArray();
                ComparisonSeries =
                [
                    ChartThemeConfig.Column(result.StockVsSoldChart.Select(r => r.CurrentQuantity).ToArray(), "المخزون الحالي", 0),
                    ChartThemeConfig.Column(result.StockVsSoldChart.Select(r => r.QuantitySold).ToArray(), "المباع", 2)
                ];
                ComparisonXAxes = [ChartThemeConfig.CreateXAxis(labels, -35)];
                ComparisonYAxes = CreateQuantityYAxis();
            }
            else
            {
                ComparisonSeries = [];
                ComparisonXAxes = [];
                ComparisonYAxes = CreateQuantityYAxis();
            }

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
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

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "تقرير_احتياج_المخزون.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[]
        {
            "المنتج", "التصنيف", "المخزن", "المخزون الحالي", "المباع", "الحد الأدنى",
            "كمية التوريد المقترحة", "متوسط التكلفة", "قيمة المخزون", "قيمة التوريد", "الحالة"
        };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName,
            r.CategoryName,
            r.WarehouseName,
            r.CurrentQuantity,
            r.QuantitySold,
            r.MinimumStock,
            r.SuggestedOrderQuantity,
            r.AverageCost,
            r.StockValue,
            r.EstimatedOrderValue,
            r.StatusDisplay
        }).ToList();

        _exportService.ExportToExcel(dlg.FileName, "احتياج المخزون", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[]
        {
            "المنتج", "المخزن", "المخزون", "المباع", "توريد مقترح", "الحالة"
        };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName,
            r.WarehouseName,
            r.CurrentQuantity,
            r.QuantitySold,
            r.SuggestedOrderQuantity,
            r.StatusDisplay
        }).ToList();

        _exportService.PrintTable("تقرير احتياج المخزون", cols, rows);
    }

    private static Axis[] CreateQuantityYAxis() =>
    [
        new Axis
        {
            Labeler = v => v.ToString("N0"),
            TextSize = ChartThemeConfig.LabelSize,
            LabelsPaint = ChartThemeConfig.CreateLabelPaint(),
            SeparatorsPaint = new SolidColorPaint { Color = ChartThemeConfig.GridLineColor, StrokeThickness = 1 },
            MinLimit = 0
        }
    ];

    private static string TruncateLabel(string name) =>
        name.Length <= 16 ? name : string.Concat(name.AsSpan(0, 13), "...");

    public record ReplenishmentFilterItem(string Label, InventoryReplenishmentFilter Value);
}
