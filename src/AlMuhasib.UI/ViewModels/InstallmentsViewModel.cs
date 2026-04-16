using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentsViewModel : ViewModelBase
{
    private readonly IInstallmentService _installmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExportService _exportService;

    // ── Tab selection ──────────────────────────────────────
    [ObservableProperty]
    private int _selectedTabIndex;

    // ══════════════════════════════════════════════════════
    // TAB 0: ALL PLANS (كشف الأقساط العام)
    // ══════════════════════════════════════════════════════
    public ObservableCollection<InstallmentPlan> AllPlans { get; } = [];

    [ObservableProperty]
    private string _plansSearchText = string.Empty;

    [ObservableProperty]
    private int _plansCurrentPage = 1;

    [ObservableProperty]
    private int _plansTotalPages = 1;

    [ObservableProperty]
    private int _plansTotalCount;

    private const int PlansPageSize = 20;

    // ══════════════════════════════════════════════════════
    // TAB 1: OVERDUE (كشف المتلكئين)
    // ══════════════════════════════════════════════════════
    public ObservableCollection<Installment> OverdueInstallments { get; } = [];

    [ObservableProperty]
    private int _overdueCount;

    // ══════════════════════════════════════════════════════
    // TAB 2: PAYMENT (تسديد الأقساط)
    // ══════════════════════════════════════════════════════
    [ObservableProperty]
    private string _paymentCustomerSearch = string.Empty;

    public ObservableCollection<Customer> PaymentCustomers { get; } = [];

    [ObservableProperty]
    private Customer? _paymentSelectedCustomer;

    public ObservableCollection<InstallmentPlan> CustomerPlans { get; } = [];

    [ObservableProperty]
    private InstallmentPlan? _paymentSelectedPlan;

    public ObservableCollection<Installment> PlanInstallments { get; } = [];

    [ObservableProperty]
    private Installment? _paymentSelectedInstallment;

    [ObservableProperty]
    private decimal _paymentAmount;

    [ObservableProperty]
    private CashBox? _paymentCashBox;

    public ObservableCollection<CashBox> PaymentCashBoxes { get; } = [];

    [ObservableProperty]
    private string _paymentMessage = string.Empty;

    [ObservableProperty]
    private bool _isPaymentSuccess;

    // ══════════════════════════════════════════════════════
    // TAB 3: DETAILED (كشف أقساط تفصيلي)
    // ══════════════════════════════════════════════════════
    [ObservableProperty]
    private string _detailedSearchText = string.Empty;

    public ObservableCollection<InstallmentPlan> DetailedPlans { get; } = [];

    [ObservableProperty]
    private InstallmentPlan? _detailedSelectedPlan;

    public ObservableCollection<Installment> DetailedInstallments { get; } = [];

    // ══════════════════════════════════════════════════════
    // TAB 4: PAID (كشف مسددة)
    // ══════════════════════════════════════════════════════
    public ObservableCollection<Installment> PaidInstallments { get; } = [];

    [ObservableProperty]
    private int _paidCurrentPage = 1;

    [ObservableProperty]
    private int _paidTotalPages = 1;

    [ObservableProperty]
    private int _paidTotalCount;

    [ObservableProperty]
    private string _paidSearchText = string.Empty;

    private const int PaidPageSize = 20;

    // ══════════════════════════════════════════════════════
    // TAB 5: UNPAID (كشف غير مسددة)
    // ══════════════════════════════════════════════════════
    public ObservableCollection<Installment> UnpaidInstallments { get; } = [];

    [ObservableProperty]
    private int _unpaidCurrentPage = 1;

    [ObservableProperty]
    private int _unpaidTotalPages = 1;

    [ObservableProperty]
    private int _unpaidTotalCount;

    [ObservableProperty]
    private string _unpaidSearchText = string.Empty;

    private const int UnpaidPageSize = 20;

    // ── Shared ─────────────────────────────────────────────
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public InstallmentsViewModel(
        IInstallmentService installmentService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IExportService exportService)
    {
        _installmentService = installmentService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _exportService = exportService;

        PageTitle = "الأقساط";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            LoadPermissions(_currentUserService, "Installments");

            // Load CashBoxes for payment tab
            var cashBoxes = await _unitOfWork.CashBoxes.GetAllAsync();
            PaymentCashBoxes.Clear();
            foreach (var cb in cashBoxes)
                PaymentCashBoxes.Add(cb);
            if (PaymentCashBoxes.Count > 0)
                PaymentCashBox = PaymentCashBoxes[0];

            // Load all customers for payment search
            var customers = await _unitOfWork.Customers.GetAllAsync();
            PaymentCustomers.Clear();
            foreach (var c in customers)
                PaymentCustomers.Add(c);

            // Load initial data for active tab
            await LoadAllPlansAsync();
            await LoadOverdueAsync();
            await RefreshSummaryAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        _ = OnTabChangedAsync(value);
    }

    private async Task OnTabChangedAsync(int tabIndex)
    {
        ErrorMessage = string.Empty;
        try
        {
            switch (tabIndex)
            {
                case 0: await LoadAllPlansAsync(); break;
                case 1: await LoadOverdueAsync(); break;
                case 4: await LoadPaidAsync(); break;
                case 5: await LoadUnpaidAsync(); break;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.Message}";
        }
    }

    // ══════════════════════════════════════════════════════
    // TAB 0: ALL PLANS
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private async Task LoadAllPlansAsync()
    {
        var (items, totalCount) = await _installmentService.GetPagedPlansAsync(
            PlansCurrentPage, PlansPageSize,
            string.IsNullOrWhiteSpace(PlansSearchText) ? null : PlansSearchText.Trim());

        AllPlans.Clear();
        foreach (var p in items)
            AllPlans.Add(p);

        PlansTotalCount = totalCount;
        PlansTotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PlansPageSize));
    }

    [RelayCommand]
    private async Task PlansSearchAsync()
    {
        PlansCurrentPage = 1;
        await LoadAllPlansAsync();
    }

    [RelayCommand]
    private async Task PlansNextPage()
    {
        if (PlansCurrentPage < PlansTotalPages)
        {
            PlansCurrentPage++;
            await LoadAllPlansAsync();
        }
    }

    [RelayCommand]
    private async Task PlansPreviousPage()
    {
        if (PlansCurrentPage > 1)
        {
            PlansCurrentPage--;
            await LoadAllPlansAsync();
        }
    }

    [RelayCommand]
    private void ExportPlans()
    {
        if (AllPlans.Count == 0) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"كشف_الأقساط_{DateTime.Now:yyyyMMdd}.xlsx",
            DefaultExt = ".xlsx"
        };

        if (dialog.ShowDialog() != true) return;

        var columns = new[] { "العميل", "رقم الإضبارة", "المبلغ الكلي", "عدد الأقساط", "مبلغ القسط", "تاريخ البدء", "رقم الفاتورة" };
        var rows = AllPlans.Select(p => new object[]
        {
            p.Customer?.Name ?? "",
            p.FileNumber ?? "",
            p.TotalAmount,
            p.NumberOfInstallments,
            p.InstallmentAmount,
            p.StartDate.ToString("yyyy/MM/dd"),
            p.Invoice?.InvoiceNumber ?? ""
        }).ToList();

        _exportService.ExportToExcel(dialog.FileName, "كشف الأقساط", columns, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintPlans()
    {
        if (AllPlans.Count == 0) return;
        _exportService.PrintTable("كشف الأقساط العام",
            new[] { "العميل", "رقم الإضبارة", "المبلغ الكلي", "عدد الأقساط", "مبلغ القسط", "تاريخ البدء" },
            AllPlans.Select(p => new object[]
            {
                p.Customer?.Name ?? "",
                p.FileNumber ?? "",
                p.TotalAmount.ToString("N0"),
                p.NumberOfInstallments,
                p.InstallmentAmount.ToString("N0"),
                p.StartDate.ToString("yyyy/MM/dd")
            }).ToList());
    }

    // ══════════════════════════════════════════════════════
    // TAB 1: OVERDUE
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private async Task LoadOverdueAsync()
    {
        var overdue = await _installmentService.GetOverdueInstallmentsAsync();
        OverdueInstallments.Clear();
        foreach (var i in overdue)
            OverdueInstallments.Add(i);
        OverdueCount = OverdueInstallments.Count;
    }

    [RelayCommand]
    private async Task PayOverdueInstallment(Installment? installment)
    {
        if (installment is null) return;
        ErrorMessage = string.Empty;

        // Show payment dialog
        var cashBoxes = PaymentCashBoxes.ToList();
        if (cashBoxes.Count == 0)
        {
            ErrorMessage = "لا توجد قاصات مسجلة";
            return;
        }

var confirmed = BeautifulMessageDialog.ShowConfirm(
                $"هل تريد تسديد المبلغ المتبقي بالكامل؟\n" +
                $"المبلغ المتبقي: {installment.RemainingAmount:N0} د.ع\n" +
                $"العميل: {installment.InstallmentPlan?.Customer?.Name}");

            if (!confirmed) return;

        try
        {
            IsBusy = true;
            await _installmentService.PayInstallmentAsync(installment.Id, installment.RemainingAmount, cashBoxes[0].Id);
            await LoadOverdueAsync();
            BeautifulMessageDialog.ShowSuccess("تم التسديد بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ══════════════════════════════════════════════════════
    // TAB 2: PAYMENT SCREEN
    // ══════════════════════════════════════════════════════

    partial void OnPaymentCustomerSearchChanged(string value)
    {
        // Not used for auto-filter; search is triggered by command
    }

    [RelayCommand]
    private async Task SearchCustomerPlans()
    {
        PaymentMessage = string.Empty;
        IsPaymentSuccess = false;
        CustomerPlans.Clear();
        PlanInstallments.Clear();
        PaymentSelectedPlan = null;
        PaymentSelectedInstallment = null;

        if (PaymentSelectedCustomer is null)
        {
            PaymentMessage = "يرجى اختيار العميل";
            return;
        }

        var plans = await _installmentService.GetPlansByCustomerAsync(PaymentSelectedCustomer.Id);
        foreach (var p in plans)
            CustomerPlans.Add(p);

        if (CustomerPlans.Count == 0)
            PaymentMessage = "لا توجد خطط أقساط لهذا العميل";
    }

    partial void OnPaymentSelectedPlanChanged(InstallmentPlan? value)
    {
        _ = LoadPlanInstallmentsAsync(value);
    }

    private async Task LoadPlanInstallmentsAsync(InstallmentPlan? plan)
    {
        PlanInstallments.Clear();
        PaymentSelectedInstallment = null;
        PaymentAmount = 0;

        if (plan is null) return;

        try
        {
            var installments = await _installmentService.GetInstallmentsByPlanIdAsync(plan.Id);
            foreach (var inst in installments.Where(i => i.Status != InstallmentStatus.Paid))
                PlanInstallments.Add(inst);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.Message}";
        }
    }

    partial void OnPaymentSelectedInstallmentChanged(Installment? value)
    {
        if (value is not null)
            PaymentAmount = value.RemainingAmount;
        else
            PaymentAmount = 0;
    }

    [RelayCommand]
    private async Task PayInstallment()
    {
        PaymentMessage = string.Empty;
        IsPaymentSuccess = false;

        if (PaymentSelectedInstallment is null)
        {
            PaymentMessage = "يرجى اختيار القسط المراد تسديده";
            return;
        }

        if (PaymentCashBox is null)
        {
            PaymentMessage = "يرجى اختيار القاصة";
            return;
        }

        if (PaymentAmount <= 0)
        {
            PaymentMessage = "مبلغ الدفع يجب أن يكون أكبر من صفر";
            return;
        }

        try
        {
            IsBusy = true;
            await _installmentService.PayInstallmentAsync(
                PaymentSelectedInstallment.Id, PaymentAmount, PaymentCashBox.Id);

            IsPaymentSuccess = true;
            PaymentMessage = $"تم تسديد {PaymentAmount:N0} د.ع بنجاح";

            // Refresh the plan installments
            await LoadPlanInstallmentsAsync(PaymentSelectedPlan);
        }
        catch (Exception ex)
        {
            PaymentMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ══════════════════════════════════════════════════════
    // TAB 3: DETAILED VIEW
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private async Task SearchDetailedPlans()
    {
        DetailedPlans.Clear();
        DetailedInstallments.Clear();
        DetailedSelectedPlan = null;

        var (items, _) = await _installmentService.GetPagedPlansAsync(
            1, 100,
            string.IsNullOrWhiteSpace(DetailedSearchText) ? null : DetailedSearchText.Trim());

        foreach (var p in items)
            DetailedPlans.Add(p);
    }

    partial void OnDetailedSelectedPlanChanged(InstallmentPlan? value)
    {
        _ = LoadDetailedInstallmentsAsync(value);
    }

    private async Task LoadDetailedInstallmentsAsync(InstallmentPlan? plan)
    {
        DetailedInstallments.Clear();
        if (plan is null) return;

        try
        {
            var installments = await _installmentService.GetInstallmentsByPlanIdAsync(plan.Id);
            foreach (var inst in installments)
                DetailedInstallments.Add(inst);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.Message}";
        }
    }

    // ══════════════════════════════════════════════════════
    // TAB 4: PAID
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private async Task LoadPaidAsync()
    {
        var (items, totalCount) = await _installmentService.GetPagedInstallmentsAsync(
            PaidCurrentPage, PaidPageSize, InstallmentStatus.Paid, searchTerm:
            string.IsNullOrWhiteSpace(PaidSearchText) ? null : PaidSearchText.Trim());

        PaidInstallments.Clear();
        foreach (var i in items)
            PaidInstallments.Add(i);

        PaidTotalCount = totalCount;
        PaidTotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PaidPageSize));
    }

    [RelayCommand]
    private async Task PaidSearchAsync()
    {
        PaidCurrentPage = 1;
        await LoadPaidAsync();
    }

    [RelayCommand]
    private async Task PaidNextPage()
    {
        if (PaidCurrentPage < PaidTotalPages)
        {
            PaidCurrentPage++;
            await LoadPaidAsync();
        }
    }

    [RelayCommand]
    private async Task PaidPreviousPage()
    {
        if (PaidCurrentPage > 1)
        {
            PaidCurrentPage--;
            await LoadPaidAsync();
        }
    }

    [RelayCommand]
    private void ExportPaid()
    {
        if (PaidInstallments.Count == 0) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"أقساط_مسددة_{DateTime.Now:yyyyMMdd}.xlsx",
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        var columns = new[] { "العميل", "تاريخ الاستحقاق", "المبلغ", "المسدد", "تاريخ التسديد", "القاصة" };
        var rows = PaidInstallments.Select(i => new object[]
        {
            i.InstallmentPlan?.Customer?.Name ?? "",
            i.DueDate.ToString("yyyy/MM/dd"),
            i.Amount,
            i.PaidAmount,
            i.PaymentDate?.ToString("yyyy/MM/dd") ?? "",
            i.CashBox?.Name ?? ""
        }).ToList();

        _exportService.ExportToExcel(dialog.FileName, "أقساط مسددة", columns, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintPaid()
    {
        if (PaidInstallments.Count == 0) return;
        _exportService.PrintTable("كشف الأقساط المسددة",
            new[] { "العميل", "تاريخ الاستحقاق", "المبلغ", "المسدد", "تاريخ التسديد" },
            PaidInstallments.Select(i => new object[]
            {
                i.InstallmentPlan?.Customer?.Name ?? "",
                i.DueDate.ToString("yyyy/MM/dd"),
                i.Amount.ToString("N0"),
                i.PaidAmount.ToString("N0"),
                i.PaymentDate?.ToString("yyyy/MM/dd") ?? ""
            }).ToList());
    }

    // ══════════════════════════════════════════════════════
    // TAB 5: UNPAID
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private async Task LoadUnpaidAsync()
    {
        await _installmentService.UpdateOverdueStatusesAsync();

        // Get pending + overdue + partially paid
        var pendingResult = await _installmentService.GetPagedInstallmentsAsync(
            UnpaidCurrentPage, UnpaidPageSize, InstallmentStatus.Pending, searchTerm:
            string.IsNullOrWhiteSpace(UnpaidSearchText) ? null : UnpaidSearchText.Trim());

        var overdueResult = await _installmentService.GetPagedInstallmentsAsync(
            1, 1000, InstallmentStatus.Overdue, searchTerm:
            string.IsNullOrWhiteSpace(UnpaidSearchText) ? null : UnpaidSearchText.Trim());

        var partialResult = await _installmentService.GetPagedInstallmentsAsync(
            1, 1000, InstallmentStatus.PartiallyPaid, searchTerm:
            string.IsNullOrWhiteSpace(UnpaidSearchText) ? null : UnpaidSearchText.Trim());

        UnpaidInstallments.Clear();
        foreach (var i in overdueResult.Items)
            UnpaidInstallments.Add(i);
        foreach (var i in partialResult.Items)
            UnpaidInstallments.Add(i);
        foreach (var i in pendingResult.Items)
            UnpaidInstallments.Add(i);

        UnpaidTotalCount = pendingResult.TotalCount + overdueResult.TotalCount + partialResult.TotalCount;
        UnpaidTotalPages = Math.Max(1, (int)Math.Ceiling(UnpaidTotalCount / (double)UnpaidPageSize));
    }

    [RelayCommand]
    private async Task UnpaidSearchAsync()
    {
        UnpaidCurrentPage = 1;
        await LoadUnpaidAsync();
    }

    [RelayCommand]
    private async Task UnpaidNextPage()
    {
        if (UnpaidCurrentPage < UnpaidTotalPages)
        {
            UnpaidCurrentPage++;
            await LoadUnpaidAsync();
        }
    }

    [RelayCommand]
    private async Task UnpaidPreviousPage()
    {
        if (UnpaidCurrentPage > 1)
        {
            UnpaidCurrentPage--;
            await LoadUnpaidAsync();
        }
    }

    [RelayCommand]
    private void ExportUnpaid()
    {
        if (UnpaidInstallments.Count == 0) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"أقساط_غير_مسددة_{DateTime.Now:yyyyMMdd}.xlsx",
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        var columns = new[] { "العميل", "الحالة", "تاريخ الاستحقاق", "المبلغ", "المسدد", "المتبقي" };
        var rows = UnpaidInstallments.Select(i => new object[]
        {
            i.InstallmentPlan?.Customer?.Name ?? "",
            StatusToArabic(i.Status),
            i.DueDate.ToString("yyyy/MM/dd"),
            i.Amount,
            i.PaidAmount,
            i.RemainingAmount
        }).ToList();

        _exportService.ExportToExcel(dialog.FileName, "أقساط غير مسددة", columns, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintUnpaid()
    {
        if (UnpaidInstallments.Count == 0) return;
        _exportService.PrintTable("كشف الأقساط غير المسددة",
            new[] { "العميل", "الحالة", "تاريخ الاستحقاق", "المبلغ", "المتبقي" },
            UnpaidInstallments.Select(i => new object[]
            {
                i.InstallmentPlan?.Customer?.Name ?? "",
                StatusToArabic(i.Status),
                i.DueDate.ToString("yyyy/MM/dd"),
                i.Amount.ToString("N0"),
                i.RemainingAmount.ToString("N0")
            }).ToList());
    }

    private static string StatusToArabic(InstallmentStatus status) => status switch
    {
        InstallmentStatus.Paid => "مسدد",
        InstallmentStatus.PartiallyPaid => "مسدد جزئياً",
        InstallmentStatus.Overdue => "متأخر",
        InstallmentStatus.Pending => "قيد الانتظار",
        _ => status.ToString()
    };

    // ══════════════════════════════════════════════════════
    // INLINE ACTIONS: PAY & CANCEL PAYMENT
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private async Task PayUnpaidInstallment(Installment? installment)
    {
        if (installment is null) return;
        ErrorMessage = string.Empty;

        var cashBoxes = PaymentCashBoxes.ToList();
        if (cashBoxes.Count == 0)
        {
            ErrorMessage = "لا توجد قاصات مسجلة";
            return;
        }

        var confirmed = BeautifulMessageDialog.ShowConfirm(
            $"هل تريد تسديد المبلغ المتبقي بالكامل؟\n" +
            $"المبلغ المتبقي: {installment.RemainingAmount:N0} د.ع\n" +
            $"العميل: {installment.InstallmentPlan?.Customer?.Name}");

        if (!confirmed) return;

        try
        {
            IsBusy = true;
            await _installmentService.PayInstallmentAsync(installment.Id, installment.RemainingAmount, cashBoxes[0].Id);
            await LoadUnpaidAsync();
            await RefreshSummaryAsync();
            BeautifulMessageDialog.ShowSuccess("تم التسديد بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelPaidInstallment(Installment? installment)
    {
        if (installment is null) return;
        ErrorMessage = string.Empty;

        var confirmed = BeautifulMessageDialog.ShowConfirm(
            $"هل تريد إلغاء تسديد هذا القسط؟\n" +
            $"المبلغ المدفوع: {installment.PaidAmount:N0} د.ع\n" +
            $"العميل: {installment.InstallmentPlan?.Customer?.Name}\n\n" +
            $"⚠ سيتم خصم المبلغ من رصيد القاصة");

        if (!confirmed) return;

        try
        {
            IsBusy = true;
            await _installmentService.CancelPaymentAsync(installment.Id);
            await LoadPaidAsync();
            await RefreshSummaryAsync();
            BeautifulMessageDialog.ShowSuccess("تم إلغاء التسديد بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PayDetailedInstallment(Installment? installment)
    {
        if (installment is null || installment.Status == InstallmentStatus.Paid) return;
        ErrorMessage = string.Empty;

        var cashBoxes = PaymentCashBoxes.ToList();
        if (cashBoxes.Count == 0)
        {
            ErrorMessage = "لا توجد قاصات مسجلة";
            return;
        }

        var confirmed = BeautifulMessageDialog.ShowConfirm(
            $"هل تريد تسديد المبلغ المتبقي بالكامل؟\n" +
            $"المبلغ المتبقي: {installment.RemainingAmount:N0} د.ع");

        if (!confirmed) return;

        try
        {
            IsBusy = true;
            await _installmentService.PayInstallmentAsync(installment.Id, installment.RemainingAmount, cashBoxes[0].Id);
            await LoadDetailedInstallmentsAsync(DetailedSelectedPlan);
            await RefreshSummaryAsync();
            BeautifulMessageDialog.ShowSuccess("تم التسديد بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelDetailedInstallment(Installment? installment)
    {
        if (installment is null || installment.PaidAmount <= 0) return;
        ErrorMessage = string.Empty;

        var confirmed = BeautifulMessageDialog.ShowConfirm(
            $"هل تريد إلغاء تسديد هذا القسط؟\n" +
            $"المبلغ المدفوع: {installment.PaidAmount:N0} د.ع\n\n" +
            $"⚠ سيتم خصم المبلغ من رصيد القاصة");

        if (!confirmed) return;

        try
        {
            IsBusy = true;
            await _installmentService.CancelPaymentAsync(installment.Id);
            await LoadDetailedInstallmentsAsync(DetailedSelectedPlan);
            await RefreshSummaryAsync();
            BeautifulMessageDialog.ShowSuccess("تم إلغاء التسديد بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ══════════════════════════════════════════════════════
    // SUMMARY
    // ══════════════════════════════════════════════════════

    [ObservableProperty]
    private int _summaryTotalPlans;

    [ObservableProperty]
    private decimal _summaryTotalAmount;

    [ObservableProperty]
    private decimal _summaryPaidAmount;

    [ObservableProperty]
    private decimal _summaryRemainingAmount;

    [ObservableProperty]
    private int _summaryPaidCount;

    [ObservableProperty]
    private int _summaryUnpaidCount;

    [ObservableProperty]
    private int _summaryOverdueCount;

    private async Task RefreshSummaryAsync()
    {
        try
        {
            var (plans, totalPlans) = await _installmentService.GetPagedPlansAsync(1, int.MaxValue);
            var allPlans = plans.ToList();

            SummaryTotalPlans = totalPlans;
            SummaryTotalAmount = allPlans.Sum(p => p.TotalAmount);

            var allInstallments = allPlans.SelectMany(p => p.Installments).ToList();
            SummaryPaidAmount = allInstallments.Sum(i => i.PaidAmount);
            SummaryRemainingAmount = allInstallments.Sum(i => i.RemainingAmount);
            SummaryPaidCount = allInstallments.Count(i => i.Status == InstallmentStatus.Paid);
            SummaryUnpaidCount = allInstallments.Count(i => i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.PartiallyPaid);
            SummaryOverdueCount = allInstallments.Count(i => i.Status == InstallmentStatus.Overdue);
        }
        catch
        {
            // Silently ignore summary errors
        }
    }
}
