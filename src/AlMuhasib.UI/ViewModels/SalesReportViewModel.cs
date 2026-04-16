using System.Collections.ObjectModel;
using System.Windows;
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

public partial class SalesReportViewModel : ReportViewModelBase
{
    // Stats
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _cashSales = "0";
    [ObservableProperty] private string _creditSales = "0";
    [ObservableProperty] private string _installmentSales = "0";
    [ObservableProperty] private string _invoiceCount = "0";
    [ObservableProperty] private string _averageInvoice = "0";
    [ObservableProperty] private string _todaySales = "0";

    // Filters
    [ObservableProperty] private int? _selectedCustomerId;
    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private PaymentMethodItem? _selectedPaymentMethodItem;
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    // Chart
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    // Data
    private List<SalesReportRow> _allRows = [];
    public ObservableCollection<SalesReportRow> Rows { get; } = [];

    public SalesReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تقرير المبيعات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        await LoadFiltersAsync();
        await LoadDataAsync();
    }

    private async Task LoadFiltersAsync()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();
        foreach (var c in customers) Customers.Add(c);
        var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
        foreach (var w in warehouses) Warehouses.Add(w);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetSalesReportAsync(DateFrom, DateTo, _selectedCustomerId, _selectedPaymentMethodItem?.Value, _selectedWarehouseId);
            
            TotalSales = FormatCurrency(result.TotalSales);
            CashSales = FormatCurrency(result.CashSales);
            CreditSales = FormatCurrency(result.CreditSales);
            InstallmentSales = FormatCurrency(result.InstallmentSales);
            InvoiceCount = result.InvoiceCount.ToString("N0");
            AverageInvoice = FormatCurrency(result.AverageInvoice);
            TodaySales = FormatCurrency(result.TodaySales);

            // Chart
            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Line(result.DailyChart.Select(d => d.Amount).ToArray(), "المبيعات", 0)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }

            _allRows = result.Rows;
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_المبيعات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "رقم الفاتورة", "التاريخ", "العميل", "المخزن", "طريقة الدفع", "المبلغ", "الخصم", "الصافي" };
        var rows = _allRows.Select(r => new object[] { r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.WarehouseName, r.PaymentMethod, r.TotalAmount, r.Discount, r.NetAmount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "المبيعات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم الفاتورة", "التاريخ", "العميل", "المخزن", "طريقة الدفع", "المبلغ", "الخصم", "الصافي" };
        var rows = _allRows.Select(r => new object[] { r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.WarehouseName, r.PaymentMethod, r.TotalAmount, r.Discount, r.NetAmount }).ToList();
        _exportService.PrintTable("تقرير المبيعات", cols, rows);
    }
}
