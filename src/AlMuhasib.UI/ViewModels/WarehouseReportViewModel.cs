using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class WarehouseReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalItems = "0";
    [ObservableProperty] private string _totalValue = "0";
    [ObservableProperty] private string _warehouseCount = "0";
    [ObservableProperty] private string _averageCost = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private bool _includeZeroStock;
    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    [ObservableProperty] private ISeries[] _warehouseSeries = [];

    private List<WarehouseStockRow> _allRows = [];
    public ObservableCollection<WarehouseStockRow> Rows { get; } = [];

    public WarehouseReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    { PageTitle = "تقرير المخازن"; }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var w in await _unitOfWork.Warehouses.GetAllAsync()) Warehouses.Add(w);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetWarehouseReportAsync(_selectedWarehouseId, _includeZeroStock);

            TotalItems = result.Count.ToString("N0");
            TotalValue = FormatCurrency(result.Sum(r => r.TotalValue));
            WarehouseCount = result.Select(r => r.WarehouseName).Distinct().Count().ToString("N0");
            AverageCost = FormatCurrency(result.Count > 0 ? result.Average(r => r.AverageCost) : 0);

            if (result.Count > 0)
            {
                var byWarehouse = result.GroupBy(r => r.WarehouseName).Select(g => new { Name = g.Key, Value = g.Sum(r => r.TotalValue) }).ToList();
                WarehouseSeries = byWarehouse.Select((w, i) => (ISeries)ChartThemeConfig.Pie(w.Value, w.Name, i)).ToArray();
            }

            _allRows = result;
            CurrentPage = 1;
            UpdatePagination(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePagination(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_المخازن.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "المنتج", "المخزن", "الكمية", "متوسط التكلفة", "القيمة الإجمالية" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName,
            r.WarehouseName,
            r.Quantity.ToString("N0"),
            r.AverageCost.ToString("N0"),
            r.TotalValue.ToString("N0")
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "المخازن", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المنتج", "المخزن", "الكمية", "متوسط التكلفة", "القيمة الإجمالية" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName,
            r.WarehouseName,
            r.Quantity.ToString("N0"),
            r.AverageCost.ToString("N0"),
            r.TotalValue.ToString("N0")
        }).ToList();
        _exportService.PrintTable("تقرير المخازن", cols, rows);
    }
}
