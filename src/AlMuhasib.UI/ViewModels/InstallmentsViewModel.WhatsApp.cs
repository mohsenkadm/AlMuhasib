using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentsViewModel
{
    private InstallmentPaymentReceiptPrintModel? _pendingWhatsAppReceipt;

    [ObservableProperty]
    private bool _canSendWhatsAppReceipt;

    private void ClearWhatsAppReceiptOption()
    {
        _pendingWhatsAppReceipt = null;
        CanSendWhatsAppReceipt = false;
    }

    [RelayCommand]
    private void SendPaymentReceiptWhatsApp()
    {
        if (_pendingWhatsAppReceipt is null) return;
        _whatsAppShare.ShareInstallmentPaymentReceipt(_pendingWhatsAppReceipt);
    }

    private void StageWhatsAppReceipt(InstallmentPaymentReceiptPrintModel receipt)
    {
        _pendingWhatsAppReceipt = receipt;
        CanSendWhatsAppReceipt = true;
    }

    private static string StatusAfterPay(decimal remainingAfter) =>
        remainingAfter <= 0 ? "مسدد" : "مسدد جزئياً";

    private InstallmentPaymentReceiptPrintModel BuildSinglePaymentReceipt(
        Installment inst,
        decimal paidAmount,
        decimal remainingAfter,
        string? cashBoxName,
        int sequenceNumber = 1)
    {
        var plan = inst.InstallmentPlan;
        var customer = plan?.Customer ?? PaymentSelectedCustomer;
        return new InstallmentPaymentReceiptPrintModel
        {
            CustomerName = customer?.Name ?? "—",
            CustomerPhone = customer?.Phone,
            FileNumber = plan?.FileNumber,
            InvoiceNumber = plan?.Invoice?.InvoiceNumber ?? "—",
            PaymentDate = DateTime.Now,
            CashBoxName = cashBoxName,
            TotalPaid = paidAmount,
            Lines =
            [
                new InstallmentPaymentReceiptLine
                {
                    SequenceNumber = sequenceNumber,
                    DueDate = inst.DueDate,
                    PaidAmount = paidAmount,
                    RemainingAfter = remainingAfter,
                    StatusText = StatusAfterPay(remainingAfter)
                }
            ],
            Notes = $"تسديد قسط بمبلغ {paidAmount:N0} د.ع"
        };
    }

    private InstallmentPaymentReceiptPrintModel BuildBulkPaymentReceipt(
        IReadOnlyList<Installment> installments,
        decimal totalPaid,
        string? cashBoxName)
    {
        var first = installments[0];
        var plan = first.InstallmentPlan;
        var customer = plan?.Customer;
        var lines = installments
            .OrderBy(i => i.DueDate)
            .Select((i, idx) => new InstallmentPaymentReceiptLine
            {
                SequenceNumber = idx + 1,
                DueDate = i.DueDate,
                PaidAmount = i.RemainingAmount,
                RemainingAfter = 0,
                StatusText = "مسدد"
            })
            .ToList();

        var distinctCustomers = installments
            .Select(i => i.InstallmentPlan?.Customer?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();

        var notes = distinctCustomers.Count > 1
            ? $"تسديد جماعي لـ {installments.Count} أقساط — عملاء متعددون"
            : $"تسديد جماعي لـ {installments.Count} قسط/أقساط";

        return new InstallmentPaymentReceiptPrintModel
        {
            CustomerName = distinctCustomers.Count == 1
                ? distinctCustomers[0]!
                : customer?.Name ?? "عملاء متعددون",
            CustomerPhone = customer?.Phone,
            FileNumber = plan?.FileNumber,
            InvoiceNumber = plan?.Invoice?.InvoiceNumber ?? "—",
            PaymentDate = DateTime.Now,
            CashBoxName = cashBoxName,
            TotalPaid = totalPaid,
            Lines = lines,
            Notes = notes
        };
    }
}
