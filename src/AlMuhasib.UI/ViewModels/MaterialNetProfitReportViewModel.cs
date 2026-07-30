using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class MaterialNetProfitReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalNetProfit = "0";
    [ObservableProperty] private string _totalStockValue = "0";
    [ObservableProperty] private string _productCount = "0";
    [ObservableProperty] private string _averageMarginPercent = "0%";
    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private int _topCount = 30;
    [ObservableProperty] private ISeries[] _chartSeries = [];
    [ObservableProperty] private Axis[] _chartXAxes = [];
    [ObservableProperty] private Axis[] _chartYAxes = [];

    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<int> TopCountOptions { get; } = [10, 20, 30, 50];

    /// <summary>true = المواد الأقل ربحاً (ترتيب تصاعدي + قص القائمة).</summary>
    public virtual bool IsLeastProfitableMode => false;

    public string ReportTitle => IsLeastProfitableMode ? "المواد الأقل ربحاً" : "صافي أرباح المواد";
    public string ChartTitle => IsLeastProfitableMode ? "أقل 10 مواد ربحاً" : "أعلى 10 مواد ربحاً";
    public Visibility TopCountVisibility => IsLeastProfitableMode ? Visibility.Visible : Visibility.Collapsed;

    private List<MaterialNetProfitRow> _allRows = [];
    public ObservableCollection<MaterialNetProfitRow> Rows { get; } = [];

    public MaterialNetProfitReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = ReportTitle;
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
            int? topN = IsLeastProfitableMode ? TopCount : null;
            var result = await _reportService.GetMaterialNetProfitReportAsync(
                DateFrom, DateTo, SelectedWarehouseId, IsLeastProfitableMode, topN);

            TotalNetProfit = FormatCurrency(result.TotalNetProfit);
            TotalStockValue = FormatCurrency(result.TotalStockValue);
            ProductCount = result.ProductCount.ToString("N0");
            AverageMarginPercent = $"{result.AverageMarginPercent}%";

            if (result.Chart.Count > 0)
            {
                var colorIndex = IsLeastProfitableMode ? 3 : 2;
                ChartSeries = [ChartThemeConfig.Column(result.Chart.Select(c => c.Amount).ToArray(), "صافي الربح", colorIndex)];
                ChartXAxes = [ChartThemeConfig.CreateXAxis(result.Chart.Select(c => c.Name).ToArray(), -45)];
                ChartYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                ChartSeries = [];
            }

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = IsLeastProfitableMode ? "المواد_الأقل_ربحا.xlsx" : "صافي_أرباح_المواد.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "#", "المنتج", "كمية المخزن", "قيمة المخزن", "الكمية المباعة", "الإيراد", "التكلفة", "صافي الربح", "الهامش %" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Rank, r.ProductName, r.StockQuantity.ToString("N0"), r.StockValue.ToString("N0"),
            r.QuantitySold.ToString("N0"), r.Revenue.ToString("N0"), r.Cost.ToString("N0"),
            r.NetProfit.ToString("N0"), r.MarginPercent
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, ReportTitle, cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "#", "المنتج", "كمية المخزن", "قيمة المخزن", "المباعة", "الإيراد", "التكلفة", "صافي الربح", "الهامش %" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Rank, r.ProductName, r.StockQuantity.ToString("N0"), r.StockValue.ToString("N0"),
            r.QuantitySold.ToString("N0"), r.Revenue.ToString("N0"), r.Cost.ToString("N0"),
            r.NetProfit.ToString("N0"), r.MarginPercent
        }).ToList();
        _exportService.PrintTable(ReportTitle, cols, rows);
    }
}

public partial class LeastProfitMaterialsReportViewModel : MaterialNetProfitReportViewModel
{
    public override bool IsLeastProfitableMode => true;

    public LeastProfitMaterialsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "المواد الأقل ربحاً";
    }
}
