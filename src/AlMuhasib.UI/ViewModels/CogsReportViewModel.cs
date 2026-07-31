using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class CogsReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalCogs = "0";
    [ObservableProperty] private string _totalRevenue = "0";
    [ObservableProperty] private string _grossProfit = "0";
    [ObservableProperty] private string _productCount = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<CogsReportRow> _allRows = [];
    public ObservableCollection<CogsReportRow> Rows { get; } = [];

    public CogsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تكلفة البضاعة المباعة";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var x in await _unitOfWork.Warehouses.GetAllAsync()) Warehouses.Add(x);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetCogsReportAsync(DateFrom, DateTo, SelectedWarehouseId);

            TotalCogs = FormatCurrency(result.TotalCogs);
            TotalRevenue = FormatCurrency(result.TotalRevenue);
            GrossProfit = FormatCurrency(result.GrossProfit);
            ProductCount = result.ProductCount.ToString("N0");
            if (result.TopProductsChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.TopProductsChart);
            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تكلفة_البضاعة_المباعة.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "المنتج", "الكمية", "متوسط التكلفة", "التكلفة", "الإيراد", "الربح" };
        var rows = _allRows.Select(r => new object[] { r.ProductName, r.QuantitySold, r.AverageCost, r.CogsAmount, r.Revenue, r.GrossProfit }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "تكلفة البضاعة المباعة", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المنتج", "الكمية", "متوسط التكلفة", "التكلفة", "الإيراد", "الربح" };
        var rows = _allRows.Select(r => new object[] { r.ProductName, r.QuantitySold, r.AverageCost, r.CogsAmount, r.Revenue, r.GrossProfit }).ToList();
        _exportService.PrintTable("تكلفة البضاعة المباعة", cols, rows);
    }
}
