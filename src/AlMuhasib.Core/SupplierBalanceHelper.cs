namespace AlMuhasib.Core;

/// <summary>
/// معادلة موحّدة لرصيد المورد (الذمم الدائنة) بين سطح المكتب والسحابة.
/// الرصيد المستحق = متبقي فواتير المشتريات الآجلة − سندات الصرف غير المطبّقة.
/// </summary>
public static class SupplierBalanceHelper
{
    /// <summary>نفس علامة تطبيق سندات العملاء — تُستخدم لسندات الصرف أيضاً.</summary>
    public const string PaymentAppliedMarker = CustomerBalanceHelper.DebtReceiptAppliedMarker;

    public static bool IsPaymentApplied(string? notes)
        => CustomerBalanceHelper.IsDebtReceiptApplied(notes);

    public static string MarkPaymentApplied(string? notes)
        => CustomerBalanceHelper.MarkDebtReceiptApplied(notes);

    public static decimal ComputeOutstandingPayables(
        decimal creditInvoiceRemaining,
        decimal unappliedPayments = 0)
        => Math.Max(0, Math.Max(0, creditInvoiceRemaining) - Math.Max(0, unappliedPayments));

    /// <summary>
    /// توزيع FIFO لمبلغ سند صرف على فواتير مشتريات آجلة مفتوحة.
    /// يُرجع التحديثات: (Id, NewPaid, NewRemaining, IsCreditPaid).
    /// </summary>
    public static List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid)> AllocateToPurchaseInvoices(
        IEnumerable<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)> invoices,
        decimal amount)
        => CustomerBalanceHelper.AllocateToCreditInvoices(invoices, amount);

    /// <summary>
    /// يوزّع سندات صرف غير مطبّقة FIFO على صفوف أعمار الذمم (في الذاكرة فقط).
    /// يُرجع الصفوف المتبقية بعد الخصم مع مبالغها المحدّثة.
    /// </summary>
    public static List<T> ApplyUnappliedPaymentsToAgingRows<T>(
        IEnumerable<T> rows,
        IEnumerable<(int SupplierKey, decimal Amount)> unappliedPayments,
        Func<T, int?> getSupplierKey,
        Func<T, decimal> getRemaining,
        Func<T, decimal, T> withRemaining)
    {
        var list = rows.ToList();
        var paymentsBySupplier = unappliedPayments
            .GroupBy(p => p.SupplierKey)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        if (paymentsBySupplier.Count == 0)
            return list.Where(r => getRemaining(r) > 0).ToList();

        // حافظ على ترتيب الصفوف كما ورد (عادةً حسب الاستحقاق) لكل مورد
        var result = new List<T>(list.Count);
        var bySupplier = list
            .Select((row, index) => (row, index))
            .GroupBy(x => getSupplierKey(x.row) ?? int.MinValue);

        var kept = new List<(int Index, T Row)>();
        foreach (var group in bySupplier)
        {
            var remainingPayment = paymentsBySupplier.GetValueOrDefault(group.Key);
            foreach (var (row, index) in group.OrderBy(x => x.index))
            {
                var rem = getRemaining(row);
                if (rem <= 0)
                    continue;

                if (remainingPayment > 0)
                {
                    var apply = Math.Min(remainingPayment, rem);
                    rem -= apply;
                    remainingPayment -= apply;
                }

                if (rem > 0)
                    kept.Add((index, withRemaining(row, rem)));
            }
        }

        return kept.OrderBy(x => x.Index).Select(x => x.Row).ToList();
    }
}
