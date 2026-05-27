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
    private readonly IInvoiceService _invoiceService;

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

    // Payment dialog
    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private CashBox? _paymentCashBox;
    [ObservableProperty] private PurchasesReportRow? _paymentTargetRow;
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    public PurchasesReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService,
        IInvoiceService invoiceService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _invoiceService = invoiceService;
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
        var cashBoxes = await _unitOfWork.CashBoxes.GetAllAsync();
        foreach (var cb in cashBoxes) CashBoxes.Add(cb);
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

    // ── Action Commands ─────────────────────────────────────

    [RelayCommand]
    private async Task ViewDetails(PurchasesReportRow? row)
    {
        if (row is null) return;
        try
        {
            var invoice = await _invoiceService.GetByIdWithDetailsAsync(row.InvoiceId);
            if (invoice is null) { BeautifulMessageDialog.ShowWarning("الفاتورة غير موجودة"); return; }

            InvoiceDetailDialog.Show(invoice, row.PaymentMethod);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
    }

    [RelayCommand]
    private async Task PrintRow(PurchasesReportRow? row)
    {
        if (row is null) return;
        try
        {
            var invoice = await _invoiceService.GetByIdWithDetailsAsync(row.InvoiceId);
            if (invoice is null) { BeautifulMessageDialog.ShowWarning("الفاتورة غير موجودة"); return; }

            var model = new InvoicePrintModel
            {
                Title = "فاتورة مشتريات",
                InvoiceNumber = invoice.InvoiceNumber,
                Date = invoice.Date,
                PartyLabel = "المورد",
                PartyName = invoice.Supplier?.Name ?? "—",
                WarehouseName = invoice.Warehouse?.Name ?? "—",
                PaymentMethod = row.PaymentMethod,
                Notes = invoice.Notes,
                Subtotal = invoice.TotalAmount,
                RoundingAmount = invoice.RoundingAmount,
                GrandTotal = invoice.NetAmount,
                Items = invoice.Items.Select((item, i) => new InvoicePrintItem
                {
                    Number = i + 1,
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            };
            _exportService.PrintInvoice(model);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
    }

    [RelayCommand]
    private async Task DeleteRow(PurchasesReportRow? row)
    {
        if (row is null) return;
        if (!BeautifulMessageDialog.ShowConfirm($"هل تريد حذف الفاتورة {row.InvoiceNumber}؟")) return;
        try
        {
            IsBusy = true;
            await _invoiceService.DeleteInvoiceAsync(row.InvoiceId);
            BeautifulMessageDialog.ShowSuccess("تم حذف الفاتورة بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenPaymentDialog(PurchasesReportRow? row)
    {
        if (row is null || !row.IsCredit || row.IsCreditPaid) return;
        PaymentTargetRow = row;
        PaymentAmount = row.RemainingAmount;
        PaymentCashBox = CashBoxes.FirstOrDefault();
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmPayment()
    {
        if (PaymentTargetRow is null || PaymentCashBox is null) return;
        try
        {
            IsBusy = true;
            await _invoiceService.PayCreditInvoiceAsync(PaymentTargetRow.InvoiceId, PaymentAmount, PaymentCashBox.Id);
            IsPaymentDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess($"تم تسديد {PaymentAmount:N0} د.ع بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelPayment()
    {
        IsPaymentDialogOpen = false;
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_المشتريات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "رقم الفاتورة", "التاريخ", "المورد", "المخزن", "طريقة الدفع", "المبلغ", "الخصم", "الصافي", "المدفوع", "المتبقي" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.SupplierName, r.WarehouseName, r.PaymentMethod,
            r.TotalAmount, r.Discount, r.NetAmount, r.PaidAmount, r.RemainingAmount
        }).ToList();
        rows.Add(new object[] { "الإجمالي", "", "", "", "", _allRows.Sum(r => r.TotalAmount), _allRows.Sum(r => r.Discount), _allRows.Sum(r => r.NetAmount), _allRows.Sum(r => r.PaidAmount), _allRows.Sum(r => r.RemainingAmount) });
        _exportService.ExportToExcel(dlg.FileName, "المشتريات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم الفاتورة", "التاريخ", "المورد", "المخزن", "طريقة الدفع", "المبلغ", "الخصم", "الصافي", "المدفوع", "المتبقي" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.SupplierName, r.WarehouseName, r.PaymentMethod,
            r.TotalAmount.ToString("N0"), r.Discount.ToString("N0"), r.NetAmount.ToString("N0"),
            r.PaidAmount.ToString("N0"), r.RemainingAmount.ToString("N0")
        }).ToList();
        rows.Add(new object[] { "الإجمالي", "", "", "", "", _allRows.Sum(r => r.TotalAmount).ToString("N0"), _allRows.Sum(r => r.Discount).ToString("N0"), _allRows.Sum(r => r.NetAmount).ToString("N0"), _allRows.Sum(r => r.PaidAmount).ToString("N0"), _allRows.Sum(r => r.RemainingAmount).ToString("N0") });
        _exportService.PrintTable("تقرير المشتريات", cols, rows);
    }
}
