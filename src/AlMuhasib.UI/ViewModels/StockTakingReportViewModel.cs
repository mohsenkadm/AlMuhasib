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

public partial class StockTakingReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalQuantity = "0";
    [ObservableProperty] private string _productCount = "0";
    [ObservableProperty] private string _warehouseCount = "0";
    [ObservableProperty] private string _itemCount = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    [ObservableProperty] private bool _includeZero;

    [ObservableProperty] private ISeries[] _pieSeries = [];

    private List<StockTakingRow> _allRows = [];
    public ObservableCollection<StockTakingRow> Rows { get; } = [];

    public StockTakingReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "جرد المخزون";
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
            var result = await _reportService.GetStockTakingReportAsync(SelectedWarehouseId, IncludeZero);

            TotalQuantity = result.TotalQuantity.ToString("N0");
            ProductCount = result.ProductCount.ToString("N0");
            WarehouseCount = result.WarehouseCount.ToString("N0");
            ItemCount = result.ProductCount.ToString("N0");
            if (result.WarehouseChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.WarehouseChart);
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "جرد_المخزون.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "المنتج", "الباركود", "المخزن", "التصنيف", "كمية النظام" };
        var rows = _allRows.Select(r => new object[] { r.ProductName, r.Barcode ?? "—", r.WarehouseName, r.CategoryName, r.SystemQuantity }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "جرد المخزون", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المنتج", "الباركود", "المخزن", "التصنيف", "كمية النظام" };
        var rows = _allRows.Select(r => new object[] { r.ProductName, r.Barcode ?? "—", r.WarehouseName, r.CategoryName, r.SystemQuantity }).ToList();
        _exportService.PrintTable("جرد المخزون", cols, rows);
    }
}
