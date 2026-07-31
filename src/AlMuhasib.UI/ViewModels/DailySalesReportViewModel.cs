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

public partial class DailySalesReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _averageDaily = "0";
    [ObservableProperty] private string _dayCount = "0";
    [ObservableProperty] private string _invoiceCount = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    [ObservableProperty] private PaymentMethodItem? _selectedPaymentMethodItem;

    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<DailySalesRow> _allRows = [];
    public ObservableCollection<DailySalesRow> Rows { get; } = [];

    public DailySalesReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "يومية المبيعات";
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
            var result = await _reportService.GetDailySalesReportAsync(DateFrom, DateTo, SelectedWarehouseId, SelectedPaymentMethodItem?.Value);

            TotalSales = FormatCurrency(result.TotalSales);
            AverageDaily = FormatCurrency(result.AverageDaily);
            DayCount = result.DayCount.ToString("N0");
            InvoiceCount = result.InvoiceCount.ToString("N0");
            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Line(result.DailyChart.Select(d => d.Amount).ToArray(), "المبيعات", 0)];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "يومية_المبيعات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "الفواتير", "نقدي", "آجل", "أقساط", "الإجمالي", "عمولة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.InvoiceCount, r.CashSales, r.CreditSales, r.InstallmentSales, r.TotalSales, r.CompanyFees }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "يومية المبيعات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "الفواتير", "نقدي", "آجل", "أقساط", "الإجمالي", "عمولة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.InvoiceCount, r.CashSales, r.CreditSales, r.InstallmentSales, r.TotalSales, r.CompanyFees }).ToList();
        _exportService.PrintTable("يومية المبيعات", cols, rows);
    }
}
