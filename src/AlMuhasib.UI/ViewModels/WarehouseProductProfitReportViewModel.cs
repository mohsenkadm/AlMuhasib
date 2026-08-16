using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;

namespace AlMuhasib.UI.ViewModels;

public partial class WarehouseProductProfitReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalPotentialProfit = "0";
    [ObservableProperty] private string _totalSaleValue = "0";
    [ObservableProperty] private string _totalCostValue = "0";
    [ObservableProperty] private string _productCount = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    [ObservableProperty] private bool _includeZero;

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _secondPieSeries = [];

    private List<WarehouseProductProfitRow> _allRows = [];
    public ObservableCollection<WarehouseProductProfitRow> Rows { get; } = [];

    public WarehouseProductProfitReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "ربح المنتجات في المخزن";
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
            var result = await _reportService.GetWarehouseProductProfitReportAsync(SelectedWarehouseId, IncludeZero);

            TotalPotentialProfit = FormatCurrency(result.TotalPotentialProfit);
            TotalSaleValue = FormatCurrency(result.TotalSaleValue);
            TotalCostValue = FormatCurrency(result.TotalCostValue);
            ProductCount = result.ProductCount.ToString("N0");
            if (result.WarehouseChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.WarehouseChart);
            if (result.TopProductsChart.Count > 0)
                SecondPieSeries = ChartThemeConfig.PieFromNameAmount(result.TopProductsChart);
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "ربح_المنتجات_في_المخزن.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "المنتج", "الصنف", "المخزن", "الكمية", "التكلفة", "سعر البيع", "ربح المادة في المخزن" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName, r.CategoryName, r.WarehouseName, r.Quantity, r.AverageCost, r.SalePrice, r.PotentialProfit
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "ربح المنتجات في المخزن", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المنتج", "الصنف", "المخزن", "الكمية", "التكلفة", "سعر البيع", "ربح المادة في المخزن" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName, r.CategoryName, r.WarehouseName, r.Quantity, r.AverageCost, r.SalePrice, r.PotentialProfit
        }).ToList();
        _exportService.PrintTable("ربح المنتجات في المخزن", cols, rows);
    }
}
