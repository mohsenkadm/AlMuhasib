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

public partial class TopProductsReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalRevenue = "0";
    [ObservableProperty] private string _totalQuantity = "0";
    [ObservableProperty] private string _productCount = "0";
    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private int _topCount = 30;
    [ObservableProperty] private bool _showBestSellers = true;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<int> TopCountOptions { get; } = [10, 20, 30, 50];

    [ObservableProperty] private ISeries[] _chartSeries = [];
    [ObservableProperty] private Axis[] _chartXAxes = [];
    [ObservableProperty] private Axis[] _chartYAxes = [];

    private List<TopProductRow> _allRows = [];
    public ObservableCollection<TopProductRow> Rows { get; } = [];

    public TopProductsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "أفضل المنتجات مبيعاً";
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
            var result = await _reportService.GetTopProductsReportAsync(
                DateFrom, DateTo, SelectedWarehouseId, TopCount, ShowBestSellers);

            TotalRevenue = FormatCurrency(result.TotalRevenue);
            TotalQuantity = result.TotalQuantity.ToString("N0");
            ProductCount = result.ProductCount.ToString("N0");

            if (result.Chart.Count > 0)
            {
                ChartSeries = [ChartThemeConfig.Column(result.Chart.Select(c => c.Amount).ToArray(), "المبيعات", 0)];
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
            FileName = ShowBestSellers ? "أفضل_المنتجات.xlsx" : "أقل_المنتجات.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "الترتيب", "المنتج", "الكمية", "الإيراد", "الحصة %" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Rank, r.ProductName, r.QuantitySold.ToString("N0"), r.Revenue.ToString("N0"), r.SharePercent
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "المنتجات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var title = ShowBestSellers ? "أفضل المنتجات مبيعاً" : "أقل المنتجات مبيعاً";
        var cols = new[] { "الترتيب", "المنتج", "الكمية", "الإيراد", "الحصة %" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Rank, r.ProductName, r.QuantitySold.ToString("N0"), r.Revenue.ToString("N0"), r.SharePercent
        }).ToList();
        _exportService.PrintTable(title, cols, rows);
    }
}
