using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.Core;

/// <summary>
/// معادلة موحّدة لرصيد الزبون بين سطح المكتب والسحابة والتطبيق.
/// الرصيد المستحق = متبقي الفواتير الآجلة + متبقي الأقساط غير المسددة − سندات القبض غير المطبّقة − سندات دين غير المطبّقة.
/// التسوية والحسابات تتم لكل عملة على حدة — لا يُخلط دينار بدولار.
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

    public static string UnmarkDebtReceiptApplied(string? notes)
    {
        if (string.IsNullOrEmpty(notes) || !IsDebtReceiptApplied(notes))
            return notes ?? string.Empty;

        return notes
            .Replace(DebtReceiptAppliedMarker, string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    /// <summary>
    /// توزيع FIFO لمبلغ على فواتير آجلة بنفس العملة فقط.
    /// يُرجع التحديثات: (Id, NewPaid, NewRemaining, IsCreditPaid).
    /// </summary>
    public static List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid)> AllocateToCreditInvoices(
        IEnumerable<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount, AccountingCurrency Currency)> invoices,
        decimal amount,
        AccountingCurrency currency)
    {
        var updates = new List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid)>();
        if (amount <= 0)
            return updates;

        var remainingToApply = amount;
        foreach (var inv in invoices
                     .Where(i => i.Currency == currency && i.RemainingAmount > 0)
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

    /// <summary>
    /// عكس توزيع FIFO: يخصم من أحدث الفواتير المسدّدة جزئياً/كلياً بنفس العملة أولاً (LIFO reverse).
    /// </summary>
    public static List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid)> DeallocateFromCreditInvoices(
        IEnumerable<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount, AccountingCurrency Currency)> invoices,
        decimal amount,
        AccountingCurrency currency)
    {
        var updates = new List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid)>();
        if (amount <= 0)
            return updates;

        var remainingToReverse = amount;
        foreach (var inv in invoices
                     .Where(i => i.Currency == currency && i.PaidAmount > 0)
                     .OrderByDescending(i => i.Date)
                     .ThenByDescending(i => i.Id))
        {
            if (remainingToReverse <= 0)
                break;

            var reverse = Math.Min(remainingToReverse, inv.PaidAmount);
            var newPaid = Math.Max(0, inv.PaidAmount - reverse);
            var newRemaining = Math.Max(0, inv.NetAmount - newPaid);
            updates.Add((inv.Id, newPaid, newRemaining, newRemaining <= 0));
            remainingToReverse -= reverse;
        }

        return updates;
    }

    /// <summary>توافق خلفي: يفترض الدينار عندما لا تُمرَّر العملة.</summary>
    public static List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid)> AllocateToCreditInvoices(
        IEnumerable<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)> invoices,
        decimal amount)
        => AllocateToCreditInvoices(
            invoices.Select(i => (i.Id, i.Date, i.NetAmount, i.PaidAmount, i.RemainingAmount, AccountingCurrency.IQD)),
            amount,
            AccountingCurrency.IQD);

    public static decimal ComputeOutstandingBalance(
        decimal creditInvoiceRemaining,
        decimal unpaidInstallmentRemaining,
        decimal unappliedDebtReceipts = 0,
        decimal receiptAdvances = 0)
        => Math.Max(0, creditInvoiceRemaining)
           + Math.Max(0, unpaidInstallmentRemaining)
           - Math.Max(0, unappliedDebtReceipts)
           - Math.Max(0, receiptAdvances);

    /// <summary>رصيد مستحق لكل عملة على حدة (لا خلط).</summary>
    public static DualCurrencyBalance ComputeOutstandingBalances(
        IEnumerable<(AccountingCurrency Currency, decimal Amount)> creditInvoiceRemainings,
        IEnumerable<(AccountingCurrency Currency, decimal Amount)> unpaidInstallmentRemainings,
        IEnumerable<(AccountingCurrency Currency, decimal Amount)> unappliedDebtReceipts,
        IEnumerable<(AccountingCurrency Currency, decimal Amount)> receiptAdvances)
    {
        decimal SumFor(AccountingCurrency c, IEnumerable<(AccountingCurrency Currency, decimal Amount)> rows)
            => rows.Where(r => r.Currency == c).Sum(r => Math.Max(0, r.Amount));

        var iqd = ComputeOutstandingBalance(
            SumFor(AccountingCurrency.IQD, creditInvoiceRemainings),
            SumFor(AccountingCurrency.IQD, unpaidInstallmentRemainings),
            SumFor(AccountingCurrency.IQD, unappliedDebtReceipts),
            SumFor(AccountingCurrency.IQD, receiptAdvances));

        var usd = ComputeOutstandingBalance(
            SumFor(AccountingCurrency.USD, creditInvoiceRemainings),
            SumFor(AccountingCurrency.USD, unpaidInstallmentRemainings),
            SumFor(AccountingCurrency.USD, unappliedDebtReceipts),
            SumFor(AccountingCurrency.USD, receiptAdvances));

        return new DualCurrencyBalance(iqd, usd);
    }

    /// <summary>
    /// يبني بنود كشف الحساب لعملة واحدة. سندات القبض/الدين المطبّقة تظهر عبر PaidAmount على الفاتورة لتجنب الازدواج.
    /// </summary>
    public static (List<CustomerBalanceLedgerRow> Rows, decimal Balance) BuildCustomerStatementLedger(
        IEnumerable<CustomerBalanceInvoiceRow> invoices,
        IEnumerable<CustomerBalanceVoucherRow> vouchers,
        IEnumerable<CustomerBalanceInstallmentPaymentRow> installmentPayments,
        decimal unpaidInstallmentRemaining,
        AccountingCurrency? currencyFilter = null)
    {
        var currency = currencyFilter ?? AccountingCurrency.IQD;
        var invoiceList = invoices.Where(i => i.Currency == currency).ToList();
        var voucherList = vouchers.Where(v => v.Currency == currency).ToList();
        var paymentList = installmentPayments.Where(p => p.Currency == currency).ToList();
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
                Debit = inv.NetAmount,
                Currency = currency,
                SourceKind = "Invoice",
                DocumentId = inv.Id
            });

            if (inv.PaymentMethod == PaymentMethod.Credit && inv.PaidAmount > 0)
            {
                rows.Add(new CustomerBalanceLedgerRow
                {
                    Date = inv.Date,
                    Description = $"تسديد فاتورة آجلة {inv.InvoiceNumber}",
                    Credit = inv.PaidAmount,
                    Currency = currency,
                    SourceKind = "Invoice",
                    DocumentId = inv.Id
                });
            }
        }

        foreach (var v in voucherList
                     .Where(v => v.VoucherType == VoucherType.Receipt && !IsDebtReceiptApplied(v.Notes))
                     .OrderBy(v => v.Date)
                     .ThenBy(v => v.Id))
        {
            rows.Add(new CustomerBalanceLedgerRow
            {
                Date = v.Date,
                Description = $"سند قبض {v.VoucherNumber}",
                Credit = v.Amount,
                Currency = currency,
                SourceKind = "Voucher",
                DocumentId = v.Id
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
                Credit = v.Amount,
                Currency = currency,
                SourceKind = "Voucher",
                DocumentId = v.Id
            });
        }

        foreach (var p in paymentList.OrderBy(p => p.Date).ThenBy(p => p.Id))
        {
            rows.Add(new CustomerBalanceLedgerRow
            {
                Date = p.Date,
                Description = "دفعة قسط",
                Credit = p.PaidAmount,
                Currency = currency,
                SourceKind = "Installment",
                DocumentId = p.Id
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
            .Where(v => v.VoucherType == VoucherType.Receipt && !IsDebtReceiptApplied(v.Notes))
            .Sum(v => v.Amount);

        var balance = ComputeOutstandingBalance(
            creditRemaining, unpaidInstallmentRemaining, unappliedDebtReceipts, receiptAdvances);

        if (rows.Count > 0 && Math.Abs(rows[^1].RunningBalance - balance) >= 0.01m)
            rows[^1].RunningBalance = balance;

        return (rows, balance);
    }

    /// <summary>يبني كشفي دينار ودولار معاً.</summary>
    public static (List<CustomerBalanceLedgerRow> Rows, DualCurrencyBalance Balance) BuildDualCustomerStatementLedger(
        IEnumerable<CustomerBalanceInvoiceRow> invoices,
        IEnumerable<CustomerBalanceVoucherRow> vouchers,
        IEnumerable<CustomerBalanceInstallmentPaymentRow> installmentPayments,
        DualCurrencyBalance unpaidInstallmentRemaining)
    {
        var invoiceList = invoices.ToList();
        var voucherList = vouchers.ToList();
        var paymentList = installmentPayments.ToList();

        var (iqdRows, iqdBal) = BuildCustomerStatementLedger(
            invoiceList, voucherList, paymentList, unpaidInstallmentRemaining.Iqd, AccountingCurrency.IQD);
        var (usdRows, usdBal) = BuildCustomerStatementLedger(
            invoiceList, voucherList, paymentList, unpaidInstallmentRemaining.Usd, AccountingCurrency.USD);

        var rows = iqdRows.Concat(usdRows).OrderBy(r => r.Date).ThenBy(r => r.Currency).ToList();
        return (rows, new DualCurrencyBalance(iqdBal, usdBal));
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
    public AccountingCurrency Currency { get; init; } = AccountingCurrency.IQD;
}

public sealed class CustomerBalanceVoucherRow
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public string VoucherNumber { get; init; } = string.Empty;
    public VoucherType VoucherType { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
    public AccountingCurrency Currency { get; init; } = AccountingCurrency.IQD;
}

public sealed class CustomerBalanceInstallmentPaymentRow
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public decimal PaidAmount { get; init; }
    public AccountingCurrency Currency { get; init; } = AccountingCurrency.IQD;
}

public sealed class CustomerBalanceLedgerRow
{
    public DateTime Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; set; }
    public AccountingCurrency Currency { get; init; } = AccountingCurrency.IQD;
    public string SourceKind { get; init; } = string.Empty;
    public int DocumentId { get; init; }
}
