using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class StockHealthReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _lowStockCount = "0";
    [ObservableProperty] private string _deadStockCount = "0";
    [ObservableProperty] private string _totalDeadValue = "0";
    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private decimal _lowStockThreshold = 5;
    [ObservableProperty] private int _deadStockDays = 90;
    [ObservableProperty] private StockHealthFilter _selectedFilter = StockHealthFilter.All;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public List<StockHealthFilterItem> Filters { get; } =
    [
        new("الكل", StockHealthFilter.All),
        new("منخفض فقط", StockHealthFilter.LowStockOnly),
        new("راكد فقط", StockHealthFilter.DeadStockOnly)
    ];

    private List<StockHealthRow> _allRows = [];
    public ObservableCollection<StockHealthRow> Rows { get; } = [];

    public StockHealthReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "صحة المخزون";
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
            var result = await _reportService.GetStockHealthReportAsync(
                SelectedWarehouseId, LowStockThreshold, DeadStockDays, SelectedFilter);

            LowStockCount = result.LowStockCount.ToString("N0");
            DeadStockCount = result.DeadStockCount.ToString("N0");
            TotalDeadValue = FormatCurrency(result.TotalDeadStockValue);

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
            FileName = "صحة_المخزون.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "المنتج", "المخزن", "الكمية", "التكلفة", "القيمة", "الحالة", "آخر بيع", "أيام" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName,
            r.WarehouseName,
            r.Quantity,
            r.AverageCost,
            r.StockValue,
            r.StatusDisplay,
            r.LastSaleDate?.ToString("yyyy/MM/dd") ?? "—",
            r.DaysSinceLastSale?.ToString() ?? "—"
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "صحة المخزون", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المنتج", "المخزن", "الكمية", "القيمة", "الحالة", "آخر بيع" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName, r.WarehouseName, r.Quantity, r.StockValue, r.StatusDisplay,
            r.LastSaleDate?.ToString("yyyy/MM/dd") ?? "—"
        }).ToList();
        _exportService.PrintTable("صحة المخزون", cols, rows);
    }

    public record StockHealthFilterItem(string Label, StockHealthFilter Value);
}
