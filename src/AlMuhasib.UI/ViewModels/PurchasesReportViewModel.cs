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

public partial class PurchasesReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalPurchases = "0";
    [ObservableProperty] private string _invoiceCount = "0";
    [ObservableProperty] private string _averageInvoice = "0";
    [ObservableProperty] private string _todayPurchases = "0";

    [ObservableProperty] private int? _selectedSupplierId;
    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private PaymentMethodItem? _selectedPaymentMethodItem;
    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];
    [ObservableProperty] private ISeries[] _supplierSeries = [];

    private List<PurchasesReportRow> _allRows = [];
    public ObservableCollection<PurchasesReportRow> Rows { get; } = [];

    public PurchasesReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تقرير المشتريات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        await LoadFiltersAsync();
        await LoadDataAsync();
    }

    private async Task LoadFiltersAsync()
    {
        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        foreach (var s in suppliers) Suppliers.Add(s);
        var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
        foreach (var w in warehouses) Warehouses.Add(w);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetPurchasesReportAsync(DateFrom, DateTo, _selectedSupplierId, _selectedWarehouseId, _selectedPaymentMethodItem?.Value);

            TotalPurchases = FormatCurrency(result.TotalPurchases);
            InvoiceCount = result.InvoiceCount.ToString("N0");
            AverageInvoice = FormatCurrency(result.AverageInvoice);
            TodayPurchases = FormatCurrency(result.TodayPurchases);

            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Line(result.DailyChart.Select(d => d.Amount).ToArray(), "المشتريات", 3)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }

            if (result.BySupplierChart.Count > 0)
            {
                SupplierSeries = ChartThemeConfig.PieFromNameAmount(result.BySupplierChart);
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_المشتريات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "رقم الفاتورة", "التاريخ", "المورد", "المخزن", "طريقة الدفع", "المبلغ", "الخصم", "الصافي" };
        var rows = _allRows.Select(r => new object[] { r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.SupplierName, r.WarehouseName, r.PaymentMethod, r.TotalAmount, r.Discount, r.NetAmount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "المشتريات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم الفاتورة", "التاريخ", "المورد", "المخزن", "طريقة الدفع", "المبلغ", "الخصم", "الصافي" };
        var rows = _allRows.Select(r => new object[] { r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.SupplierName, r.WarehouseName, r.PaymentMethod, r.TotalAmount, r.Discount, r.NetAmount }).ToList();
        _exportService.PrintTable("تقرير المشتريات", cols, rows);
    }
}
