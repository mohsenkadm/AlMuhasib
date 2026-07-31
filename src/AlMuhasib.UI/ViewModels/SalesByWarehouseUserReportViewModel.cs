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

public partial class SalesByWarehouseUserReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _warehouseCount = "0";
    [ObservableProperty] private string _userCount = "0";
    [ObservableProperty] private string _invoiceCount = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _secondPieSeries = [];

    private List<SalesByWarehouseUserRow> _allRows = [];
    public ObservableCollection<SalesByWarehouseUserRow> Rows { get; } = [];

    public SalesByWarehouseUserReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "مبيعات حسب المخزن / المستخدم";
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
            var result = await _reportService.GetSalesByWarehouseUserReportAsync(DateFrom, DateTo, SelectedWarehouseId);

            TotalSales = FormatCurrency(result.TotalSales);
            WarehouseCount = result.WarehouseCount.ToString("N0");
            UserCount = result.UserCount.ToString("N0");
            InvoiceCount = result.InvoiceCount.ToString("N0");
            if (result.WarehouseChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.WarehouseChart);
            if (result.UserChart.Count > 0)
                SecondPieSeries = ChartThemeConfig.PieFromNameAmount(result.UserChart);
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "مبيعات_مخزن_مستخدم.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التصنيف", "الاسم", "الفواتير", "المبلغ", "النسبة %" };
        var rows = _allRows.Select(r => new object[] { r.GroupType, r.Name, r.InvoiceCount, r.Amount, r.SharePercent }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "مبيعات حسب المخزن / المستخدم", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التصنيف", "الاسم", "الفواتير", "المبلغ", "النسبة %" };
        var rows = _allRows.Select(r => new object[] { r.GroupType, r.Name, r.InvoiceCount, r.Amount, r.SharePercent }).ToList();
        _exportService.PrintTable("مبيعات حسب المخزن / المستخدم", cols, rows);
    }
}
