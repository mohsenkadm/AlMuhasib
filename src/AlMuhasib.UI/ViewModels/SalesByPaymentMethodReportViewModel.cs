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

public partial class SalesByPaymentMethodReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _cashSales = "0";
    [ObservableProperty] private string _creditSales = "0";
    [ObservableProperty] private string _installmentSales = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<SalesByPaymentMethodRow> _allRows = [];
    public ObservableCollection<SalesByPaymentMethodRow> Rows { get; } = [];

    public SalesByPaymentMethodReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "مبيعات حسب طريقة الدفع";
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
            var result = await _reportService.GetSalesByPaymentMethodReportAsync(DateFrom, DateTo, SelectedWarehouseId);

            TotalSales = FormatCurrency(result.TotalSales);
            CashSales = FormatCurrency(result.CashSales);
            CreditSales = FormatCurrency(result.CreditSales);
            InstallmentSales = FormatCurrency(result.InstallmentSales);
            if (result.MethodChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.MethodChart);
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "مبيعات_حسب_الدفع.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "طريقة الدفع", "عدد الفواتير", "المبلغ", "النسبة %" };
        var rows = _allRows.Select(r => new object[] { r.PaymentMethod, r.InvoiceCount, r.Amount, r.SharePercent }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "مبيعات حسب طريقة الدفع", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "طريقة الدفع", "عدد الفواتير", "المبلغ", "النسبة %" };
        var rows = _allRows.Select(r => new object[] { r.PaymentMethod, r.InvoiceCount, r.Amount, r.SharePercent }).ToList();
        _exportService.PrintTable("مبيعات حسب طريقة الدفع", cols, rows);
    }
}
