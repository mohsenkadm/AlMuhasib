using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core;

/// <summary>
/// معادلة موحّدة لرصيد الزبون بين سطح المكتب والسحابة والتطبيق.
/// الرصيد المستحق = متبقي الفواتير الآجلة + متبقي الأقساط غير المسددة − سندات القبض العامة − سندات دين غير المطبّقة.
/// </summary>
public static class CustomerBalanceHelper
{
    /// <summary>علامة تُضاف لملاحظات سند قبض الدين بعد تطبيقه على فواتير آجلة.</summary>
    public const string DebtReceiptAppliedMarker = "[CR-APPLIED]";

    public static bool IsDebtReceiptApplied(string? notes)
        => !string.IsNullOrEmpty(notes) &&
           notes.Contains(DebtReceiptAppliedMarker, StringComparison.Ordinal);

    public static string MarkDebtReceiptApplied(string? notes)
    {
        if (IsDebtReceiptApplied(notes))
            return notes ?? string.Empty;
        return string.IsNullOrWhiteSpace(notes)
            ? DebtReceiptAppliedMarker
            : $"{notes.Trim()} {DebtReceiptAppliedMarker}";
    }

    /// <summary>
    /// توزيع FIFO لمبلغ على فواتير آجلة. يُرجع التحديثات: (Id, NewPaid, NewRemaining, IsCreditPaid).
    /// </summary>
    public static List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid)> AllocateToCreditInvoices(
        IEnumerable<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)> invoices,
        decimal amount)
    {
        var updates = new List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid)>();
        if (amount <= 0)
            return updates;

        var remainingToApply = amount;
        foreach (var inv in invoices
                     .Where(i => i.RemainingAmount > 0)
                     .OrderBy(i => i.Date)
                     .ThenBy(i => i.Id))
        {
            if (remainingToApply <= 0)
                break;

            var pay = Math.Min(remainingToApply, inv.RemainingAmount);
            var newPaid = inv.PaidAmount + pay;
            var newRemaining = Math.Max(0, inv.NetAmount - newPaid);
            updates.Add((inv.Id, newPaid, newRemaining, newRemaining <= 0));
            remainingToApply -= pay;
        }

        return updates;
    }

    public static decimal ComputeOutstandingBalance(
        decimal creditInvoiceRemaining,
        decimal unpaidInstallmentRemaining,
        decimal unappliedDebtReceipts = 0,
        decimal receiptAdvances = 0)
        => Math.Max(0, creditInvoiceRemaining)
           + Math.Max(0, unpaidInstallmentRemaining)
           - Math.Max(0, unappliedDebtReceipts)
           - Math.Max(0, receiptAdvances);

    /// <summary>
    /// يبني بنود كشف الحساب. سندات قبض الدين المطبّقة تظهر عبر PaidAmount على الفاتورة لتجنب الازدواج.
    /// </summary>
    public static (List<CustomerBalanceLedgerRow> Rows, decimal Balance) BuildCustomerStatementLedger(
        IEnumerable<CustomerBalanceInvoiceRow> invoices,
        IEnumerable<CustomerBalanceVoucherRow> vouchers,
        IEnumerable<CustomerBalanceInstallmentPaymentRow> installmentPayments,
        decimal unpaidInstallmentRemaining)
    {
        var invoiceList = invoices.ToList();
        var voucherList = vouchers.ToList();
        var rows = new List<CustomerBalanceLedgerRow>();

        foreach (var inv in invoiceList
                     .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                                 (i.PaymentMethod == PaymentMethod.Credit || i.PaymentMethod == PaymentMethod.Installment))
                     .OrderBy(i => i.Date)
                     .ThenBy(i => i.Id))
        {
            rows.Add(new CustomerBalanceLedgerRow
            {
                Date = inv.Date,
                Description = $"فاتورة مبيعات {inv.InvoiceNumber}",
                Debit = inv.NetAmount
            });

            if (inv.PaymentMethod == PaymentMethod.Credit && inv.PaidAmount > 0)
            {
                rows.Add(new CustomerBalanceLedgerRow
                {
                    Date = inv.Date,
                    Description = $"تسديد فاتورة آجلة {inv.InvoiceNumber}",
                    Credit = inv.PaidAmount
                });
            }
        }

        foreach (var v in voucherList
                     .Where(v => v.VoucherType == VoucherType.Receipt)
                     .OrderBy(v => v.Date)
                     .ThenBy(v => v.Id))
        {
            rows.Add(new CustomerBalanceLedgerRow
            {
                Date = v.Date,
                Description = $"سند قبض {v.VoucherNumber}",
                Credit = v.Amount
            });
        }

        foreach (var v in voucherList
                     .Where(v => v.VoucherType == VoucherType.DebtReceipt && !IsDebtReceiptApplied(v.Notes))
                     .OrderBy(v => v.Date)
                     .ThenBy(v => v.Id))
        {
            rows.Add(new CustomerBalanceLedgerRow
            {
                Date = v.Date,
                Description = $"سند تسديد دين {v.VoucherNumber}",
                Credit = v.Amount
            });
        }

        foreach (var p in installmentPayments.OrderBy(p => p.Date).ThenBy(p => p.Id))
        {
            rows.Add(new CustomerBalanceLedgerRow
            {
                Date = p.Date,
                Description = "دفعة قسط",
                Credit = p.PaidAmount
            });
        }

        rows = rows.OrderBy(r => r.Date).ToList();
        decimal running = 0;
        foreach (var r in rows)
        {
            running += r.Debit - r.Credit;
            r.RunningBalance = running;
        }

        var creditRemaining = invoiceList
            .Where(i => i.PaymentMethod == PaymentMethod.Credit)
            .Sum(i => Math.Max(0, i.RemainingAmount));

        var unappliedDebtReceipts = voucherList
            .Where(v => v.VoucherType == VoucherType.DebtReceipt && !IsDebtReceiptApplied(v.Notes))
            .Sum(v => v.Amount);

        var receiptAdvances = voucherList
            .Where(v => v.VoucherType == VoucherType.Receipt)
            .Sum(v => v.Amount);

        var balance = ComputeOutstandingBalance(
            creditRemaining, unpaidInstallmentRemaining, unappliedDebtReceipts, receiptAdvances);

        if (rows.Count > 0 && Math.Abs(rows[^1].RunningBalance - balance) >= 0.01m)
            rows[^1].RunningBalance = balance;

        return (rows, balance);
    }
}

public sealed class CustomerBalanceInvoiceRow
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public InvoiceType InvoiceType { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public decimal NetAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
}

public sealed class CustomerBalanceVoucherRow
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public string VoucherNumber { get; init; } = string.Empty;
    public VoucherType VoucherType { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
}

public sealed class CustomerBalanceInstallmentPaymentRow
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public decimal PaidAmount { get; init; }
}

public sealed class CustomerBalanceLedgerRow
{
    public DateTime Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; set; }
}
