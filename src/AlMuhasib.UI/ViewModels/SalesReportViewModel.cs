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
    private readonly IInvoiceService _invoiceService;

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

    // Payment dialog
    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private CashBox? _paymentCashBox;
    [ObservableProperty] private SalesReportRow? _paymentTargetRow;
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    public SalesReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService,
        IInvoiceService invoiceService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _invoiceService = invoiceService;
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
        var cashBoxes = await _unitOfWork.CashBoxes.GetAllAsync();
        foreach (var cb in cashBoxes) CashBoxes.Add(cb);
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

    // ── Action Commands ─────────────────────────────────────

    [RelayCommand]
    private async Task ViewDetails(SalesReportRow? row)
    {
        if (row is null) return;
        try
        {
            var invoice = await _invoiceService.GetByIdWithDetailsAsync(row.InvoiceId);
            if (invoice is null) { BeautifulMessageDialog.ShowWarning("الفاتورة غير موجودة"); return; }

            var details = $"رقم الفاتورة: {invoice.InvoiceNumber}\n" +
                          $"التاريخ: {invoice.Date:yyyy/MM/dd}\n" +
                          $"العميل: {invoice.Customer?.Name ?? "—"}\n" +
                          $"المخزن: {invoice.Warehouse?.Name ?? "—"}\n" +
                          $"طريقة الدفع: {row.PaymentMethod}\n" +
                          $"المبلغ الكلي: {invoice.NetAmount:N0} د.ع\n";

            if (invoice.PaymentMethod == PaymentMethod.Credit)
            {
                details += $"المدفوع: {invoice.PaidAmount:N0} د.ع\n" +
                           $"المتبقي: {invoice.RemainingAmount:N0} د.ع\n" +
                           $"الحالة: {(invoice.IsCreditPaid ? "مسددة" : "غير مسددة")}\n";
            }

            if (invoice.Items.Count > 0)
            {
                details += "\n── المواد ──\n";
                int n = 1;
                foreach (var item in invoice.Items)
                    details += $"{n++}. {item.ItemName} × {item.Quantity:N0} = {item.TotalPrice:N0} د.ع\n";
            }

            BeautifulMessageDialog.ShowInfo(details);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
    }

    [RelayCommand]
    private async Task PrintRow(SalesReportRow? row)
    {
        if (row is null) return;
        try
        {
            var invoice = await _invoiceService.GetByIdWithDetailsAsync(row.InvoiceId);
            if (invoice is null) { BeautifulMessageDialog.ShowWarning("الفاتورة غير موجودة"); return; }

            var model = new InvoicePrintModel
            {
                Title = invoice.InvoiceType == InvoiceType.Installment ? "فاتورة أقساط" : "فاتورة مبيعات",
                InvoiceNumber = invoice.InvoiceNumber,
                Date = invoice.Date,
                CreditDueDate = invoice.CreditDueDate,
                PartyLabel = "العميل",
                PartyName = invoice.Customer?.Name ?? "—",
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

            // Add installment schedule if applicable
            if (invoice.InstallmentPlans.Count > 0)
            {
                var plan = invoice.InstallmentPlans.First();
                model.NumberOfInstallments = plan.NumberOfInstallments;
                model.InstallmentAmount = plan.InstallmentAmount;
                model.FileNumber = plan.FileNumber;
                model.Schedule = plan.Installments.OrderBy(ins => ins.DueDate).Select((ins, idx) => new InstallmentPrintRow
                {
                    Number = idx + 1,
                    DueDate = ins.DueDate,
                    Amount = ins.Amount
                }).ToList();
            }

            _exportService.PrintInvoice(model);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
    }

    [RelayCommand]
    private async Task DeleteRow(SalesReportRow? row)
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
    private void OpenPaymentDialog(SalesReportRow? row)
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
