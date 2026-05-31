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
using AlMuhasib.UI.Services;

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
    [ObservableProperty] private string _totalCompanyFees = "0";

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
        InitReportActionServices(invoiceService);
        RegisterThemeChartReload(LoadDataAsync);
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
            TotalCompanyFees = FormatCurrency(result.TotalCompanyFees);

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

            InvoiceDetailDialog.Show(invoice, row.PaymentMethod, row.CompanyFeeAmount > 0 ? row.CompanyFeeAmount : null);
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
                CompanyFeeAmount = row.CompanyFeeAmount > 0 ? row.CompanyFeeAmount : null,
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
                if (plan.CompanyFeeAmount > 0)
                    model.CompanyFeeAmount = plan.CompanyFeeAmount;
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
    private async Task ReturnInvoice(SalesReportRow? row)
    {
        if (row is null) return;
        if (!BeautifulMessageDialog.ShowConfirm(
                $"إنشاء فاتورة مرتجع من الفاتورة {row.InvoiceNumber}؟\nستُعاد الكميات إلى المخزن بكميات سالبة."))
            return;

        if (InvoiceNavigationBridge.ReturnSalesInvoiceAsync is null)
        {
            BeautifulMessageDialog.ShowWarning("تعذر فتح شاشة المرتجع");
            return;
        }

        try
        {
            await InvoiceNavigationBridge.ReturnSalesInvoiceAsync(row.InvoiceId);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task CopyInvoice(SalesReportRow? row)
    {
        if (row is null) return;
        if (!BeautifulMessageDialog.ShowConfirm(
                $"نسخ بنود الفاتورة {row.InvoiceNumber} إلى فاتورة مبيعات جديدة؟\nسيتم إنشاء رقم فاتورة جديد دون حفظ تلقائي."))
            return;

        if (InvoiceNavigationBridge.CopyToSalesInvoiceAsync is null)
        {
            BeautifulMessageDialog.ShowWarning("تعذر فتح شاشة فاتورة المبيعات");
            return;
        }

        try
        {
            await InvoiceNavigationBridge.CopyToSalesInvoiceAsync(row.InvoiceId);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
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
        var cols = new[] { "رقم الفاتورة", "التاريخ", "العميل", "المخزن", "طريقة الدفع", "المبلغ", "الخصم", "الصافي", "نسبة الشركة", "المدفوع", "المتبقي" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.WarehouseName, r.PaymentMethod,
            r.TotalAmount, r.Discount, r.NetAmount, r.CompanyFeeAmount, r.PaidAmount, r.RemainingAmount
        }).ToList();
        rows.Add(new object[] { "الإجمالي", "", "", "", "", _allRows.Sum(r => r.TotalAmount), _allRows.Sum(r => r.Discount), _allRows.Sum(r => r.NetAmount), _allRows.Sum(r => r.CompanyFeeAmount), _allRows.Sum(r => r.PaidAmount), _allRows.Sum(r => r.RemainingAmount) });
        _exportService.ExportToExcel(dlg.FileName, "المبيعات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم الفاتورة", "التاريخ", "العميل", "المخزن", "طريقة الدفع", "المبلغ", "الخصم", "الصافي", "نسبة الشركة", "المدفوع", "المتبقي" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.WarehouseName, r.PaymentMethod,
            r.TotalAmount.ToString("N0"), r.Discount.ToString("N0"), r.NetAmount.ToString("N0"),
            r.CompanyFeeAmount.ToString("N0"), r.PaidAmount.ToString("N0"), r.RemainingAmount.ToString("N0")
        }).ToList();
        rows.Add(new object[] { "الإجمالي", "", "", "", "", _allRows.Sum(r => r.TotalAmount).ToString("N0"), _allRows.Sum(r => r.Discount).ToString("N0"), _allRows.Sum(r => r.NetAmount).ToString("N0"), _allRows.Sum(r => r.CompanyFeeAmount).ToString("N0"), _allRows.Sum(r => r.PaidAmount).ToString("N0"), _allRows.Sum(r => r.RemainingAmount).ToString("N0") });
        _exportService.PrintTable("تقرير المبيعات", cols, rows);
    }
}
