using System.Collections.ObjectModel;
using System.Windows.Controls;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ReportViewModelBase
{
    protected IInstallmentService? InstallmentService { get; private set; }
    protected IInvoiceService? InvoiceService { get; private set; }

    public ObservableCollection<CashBox> BulkPayCashBoxes { get; } = [];

    [ObservableProperty] private CashBox? _bulkPayCashBox;

    [ObservableProperty] private string _bulkInstallmentMessage = string.Empty;

    [ObservableProperty] private string _bulkInstallmentTotal = "0";

    [ObservableProperty] private string _bulkInstallmentCount = "0";

    [ObservableProperty] private bool _hasBulkInstallmentSelection;

    private readonly List<InstallmentBulkSelection> _bulkInstallmentSelection = [];

    protected void InitReportActionServices(IInvoiceService invoiceService, IInstallmentService? installmentService = null)
    {
        InvoiceService = invoiceService;
        InstallmentService = installmentService;
    }

    protected async Task LoadBulkPayCashBoxesAsync()
    {
        if (BulkPayCashBoxes.Count > 0)
            return;

        var boxes = await _unitOfWork.CashBoxes.GetAllAsync();
        BulkPayCashBoxes.Clear();
        foreach (var cb in boxes)
            BulkPayCashBoxes.Add(cb);
        BulkPayCashBox ??= BulkPayCashBoxes.FirstOrDefault();
    }

    protected void UpdateBulkInstallmentSelection(IEnumerable<InstallmentBulkSelection> items)
    {
        _bulkInstallmentSelection.Clear();
        _bulkInstallmentSelection.AddRange(items.Where(i => i.InstallmentId > 0 && i.RemainingAmount > 0));

        var total = _bulkInstallmentSelection.Sum(i => i.RemainingAmount);
        BulkInstallmentCount = _bulkInstallmentSelection.Count.ToString("N0");
        BulkInstallmentTotal = total.ToString("N0");
        HasBulkInstallmentSelection = _bulkInstallmentSelection.Count > 0;
        BulkInstallmentMessage = string.Empty;
    }

    public void UpdateBulkInstallmentSelectionFromGrid(DataGrid grid)
    {
        var items = grid.SelectedItems
            .Cast<object>()
            .Select(MapToBulkSelection)
            .Where(i => i is not null)
            .Cast<InstallmentBulkSelection>();
        UpdateBulkInstallmentSelection(items);
    }

    protected static InstallmentBulkSelection? MapToBulkSelection(object row) => row switch
    {
        UnpaidInstallmentRow u when u.InstallmentId > 0 => new(u.InstallmentId, u.InvoiceId, u.RemainingAmount, u.CustomerName, u.DueDate),
        OverdueRow o when o.InstallmentId > 0 => new(o.InstallmentId, o.InvoiceId, o.OverdueAmount, o.CustomerName, o.DueDate),
        OverdueRow o when o.InstallmentId == 0 && o.InvoiceId > 0 => null,
        InstallmentAgingRow a when a.InstallmentId > 0 => new(a.InstallmentId, a.InvoiceId, a.RemainingAmount, a.CustomerName, a.DueDate),
        _ => null
    };

    [RelayCommand]
    protected void ClearBulkInstallmentSelection()
    {
        _bulkInstallmentSelection.Clear();
        BulkInstallmentMessage = string.Empty;
        BulkInstallmentCount = "0";
        BulkInstallmentTotal = "0";
        HasBulkInstallmentSelection = false;
        OnBulkInstallmentSelectionCleared();
    }

    protected virtual void OnBulkInstallmentSelectionCleared() { }

    [RelayCommand]
    protected async Task PayBulkInstallmentsAsync()
    {
        BulkInstallmentMessage = string.Empty;
        if (InstallmentService is null)
        {
            BulkInstallmentMessage = "خدمة الأقساط غير متوفرة";
            return;
        }

        if (_bulkInstallmentSelection.Count == 0)
        {
            BulkInstallmentMessage = "حدّد قسطاً واحداً على الأقل (Ctrl+نقر لتحديد عدة صفوف)";
            return;
        }

        if (BulkPayCashBox is null)
        {
            BulkInstallmentMessage = "يرجى اختيار الصندوق";
            return;
        }

        var total = _bulkInstallmentSelection.Sum(i => i.RemainingAmount);
        var count = _bulkInstallmentSelection.Count;
        var preview = string.Join("\n", _bulkInstallmentSelection.Take(8).Select(i =>
            $"• {i.CustomerName} — {i.RemainingAmount:N0} د.ع ({i.DueDate:yyyy/MM/dd})"));
        if (count > 8)
            preview += $"\n... و {count - 8} قسط/أقساط أخرى";

        if (!BeautifulMessageDialog.ShowConfirm(
                $"تسديد جماعي لـ {count} قسط/أقساط\nالإجمالي: {total:N0} د.ع\nالصندوق: {BulkPayCashBox.Name}\n\n{preview}\n\nهل تريد المتابعة؟",
                "تسديد جماعي"))
            return;

        try
        {
            IsBusy = true;
            var ids = _bulkInstallmentSelection.Select(i => i.InstallmentId).ToList();
            var result = await InstallmentService.PayInstallmentsBatchAsync(ids, BulkPayCashBox.Id);

            if (result.PaidCount > 0)
            {
                ClearBulkInstallmentSelection();
                await OnAfterBulkInstallmentPayAsync();

                if (result.AllSucceeded)
                    BeautifulMessageDialog.ShowSuccess($"تم تسديد {result.PaidCount} قسط/أقساط بإجمالي {result.TotalPaid:N0} د.ع");
                else
                {
                    var errText = string.Join("\n", result.Errors.Take(5));
                    BeautifulMessageDialog.ShowWarning(
                        $"تم تسديد {result.PaidCount} من {ids.Count} (إجمالي {result.TotalPaid:N0} د.ع)\n\n{errText}");
                }
            }
            else
            {
                BulkInstallmentMessage = result.Errors.Count > 0
                    ? string.Join("؛ ", result.Errors.Take(3))
                    : "لم يتم تسديد أي قسط";
            }
        }
        catch (Exception ex)
        {
            BulkInstallmentMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected virtual Task OnAfterBulkInstallmentPayAsync() => Task.CompletedTask;

    [RelayCommand]
    protected async Task OpenInvoiceDetailFromReportRowAsync(object? row)
    {
        if (InvoiceService is null || row is null)
            return;

        var (invoiceId, paymentLabel, companyFee) = ResolveInvoiceFromRow(row);
        if (invoiceId <= 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد فاتورة مرتبطة بهذا السجل");
            return;
        }

        try
        {
            IsBusy = true;
            var invoice = await InvoiceService.GetByIdWithDetailsAsync(invoiceId);
            if (invoice is null)
            {
                BeautifulMessageDialog.ShowWarning("الفاتورة غير موجودة");
                return;
            }

            InvoiceDetailDialog.Show(invoice, paymentLabel, companyFee);
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

    private static (int invoiceId, string? paymentLabel, decimal? companyFee) ResolveInvoiceFromRow(object row) =>
        row switch
        {
            SalesReportRow s => (s.InvoiceId, s.PaymentMethod, s.CompanyFeeAmount > 0 ? s.CompanyFeeAmount : null),
            PurchasesReportRow p => (p.InvoiceId, p.PaymentMethod, null),
            UnpaidInstallmentRow u => (u.InvoiceId, "أقساط", null),
            OverdueRow o => (o.InvoiceId, o.InstallmentId > 0 ? "أقساط" : "آجل", null),
            InstallmentAgingRow a => (a.InvoiceId, "أقساط", null),
            _ => (0, null, null)
        };

    protected sealed record InstallmentBulkSelection(
        int InstallmentId,
        int InvoiceId,
        decimal RemainingAmount,
        string CustomerName,
        DateTime DueDate);
}
