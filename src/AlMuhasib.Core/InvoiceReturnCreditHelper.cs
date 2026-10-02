using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core;

/// <summary>
/// تطبيق مرتجع المبيعات/المشتريات على فواتير الآجل الأصلية (وتتبع التطبيق عبر ملاحظات المرتجع).
/// </summary>
public static class InvoiceReturnCreditHelper
{
    public const string AppliedMarkerPrefix = "[RET-APPLIED:";
    public const string AdvanceMarkerPrefix = "[RET-ADVANCE:";

    public static bool IsReturnApplied(string? notes)
        => !string.IsNullOrEmpty(notes) &&
           notes.Contains(AppliedMarkerPrefix, StringComparison.Ordinal);

    public static string MarkApplied(
        string? notes,
        IReadOnlyList<(int InvoiceId, decimal Amount)> allocations,
        string? advanceVoucherNumber = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(notes))
            parts.Add(notes.Trim());

        // دائماً نضع علامة حتى لا يُعاد التطبيق في الإصلاح التلقائي
        var body = string.Join(';', allocations
            .Where(a => a.Amount > 0)
            .Select(a => $"{a.InvoiceId}={a.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        parts.Add($"{AppliedMarkerPrefix}{body}]");

        if (!string.IsNullOrWhiteSpace(advanceVoucherNumber))
            parts.Add($"{AdvanceMarkerPrefix}{advanceVoucherNumber.Trim()}]");

        return string.Join(' ', parts);
    }

    public static List<(int InvoiceId, decimal Amount)> ParseAllocations(string? notes)
    {
        var result = new List<(int InvoiceId, decimal Amount)>();
        if (string.IsNullOrEmpty(notes))
            return result;

        var start = notes.IndexOf(AppliedMarkerPrefix, StringComparison.Ordinal);
        if (start < 0)
            return result;

        start += AppliedMarkerPrefix.Length;
        var end = notes.IndexOf(']', start);
        if (end < 0)
            return result;

        var body = notes[start..end];
        foreach (var part in body.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0)
                continue;
            if (!int.TryParse(part[..eq], out var id))
                continue;
            if (!decimal.TryParse(
                    part[(eq + 1)..],
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var amount))
                continue;
            if (id > 0 && amount > 0)
                result.Add((id, amount));
        }

        return result;
    }

    public static string? ParseAdvanceVoucherNumber(string? notes)
    {
        if (string.IsNullOrEmpty(notes))
            return null;

        var start = notes.IndexOf(AdvanceMarkerPrefix, StringComparison.Ordinal);
        if (start < 0)
            return null;

        start += AdvanceMarkerPrefix.Length;
        var end = notes.IndexOf(']', start);
        if (end <= start)
            return null;

        var number = notes[start..end].Trim();
        return string.IsNullOrEmpty(number) ? null : number;
    }

    /// <summary>
    /// يوزّع مبلغ المرتجع على فواتير آجلة مفتوحة. يفضّل الفاتورة المرتبطة إن وُجدت.
    /// </summary>
    public static List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid, decimal Applied)> AllocateReturnToCreditInvoices(
        IEnumerable<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)> openInvoices,
        decimal returnAmount,
        int? preferredInvoiceId = null)
    {
        var result = new List<(int Id, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid, decimal Applied)>();
        if (returnAmount <= 0)
            return result;

        var list = openInvoices
            .Where(i => i.RemainingAmount > 0)
            .OrderBy(i => preferredInvoiceId.HasValue && i.Id == preferredInvoiceId.Value ? 0 : 1)
            .ThenBy(i => i.Date)
            .ThenBy(i => i.Id)
            .ToList();

        var remaining = returnAmount;
        foreach (var inv in list)
        {
            if (remaining <= 0)
                break;

            var pay = Math.Min(remaining, inv.RemainingAmount);
            var newPaid = inv.PaidAmount + pay;
            var newRemaining = Math.Max(0, inv.NetAmount - newPaid);
            result.Add((inv.Id, newPaid, newRemaining, newRemaining <= 0, pay));
            remaining -= pay;
        }

        return result;
    }

    public static bool IsCreditReturnType(InvoiceType type)
        => type is InvoiceType.SaleReturn or InvoiceType.PurchaseReturn;

    public static InvoiceType? GetOriginalInvoiceType(InvoiceType returnType) => returnType switch
    {
        InvoiceType.SaleReturn => InvoiceType.Sale,
        InvoiceType.PurchaseReturn => InvoiceType.Purchase,
        _ => null
    };

    /// <summary>
    /// بعد توزيع المرتجع على فواتير الآجل المفتوحة:
    /// نقدي → حركة الصندوق = المتبقي غير المطبّق فقط (يمنع ازدواج تخفيض الذمم + كامل المبلغ نقداً).
    /// آجل → حركة الصندوق = أقل من (دفعة نقدية مطلوبة، المتبقي)؛ وما تبقّى رصيد دائن على وثيقة المرتجع.
    /// </summary>
    /// <remarks>
    /// محاسبياً عند وجود ذمة آجلة مفتوحة لنفس المورد/العميل:
    /// المرتجع (نقدي أو آجل) يخفض الذمة أولاً. النقد يتحرك فقط للمتبقي بعد التطبيق.
    /// مثال: شراء آجل 390 ثم مرتجع 13 → المتبقي 377 (وليس 390 أو 403).
    /// </remarks>
    public static (decimal CashMovement, decimal PaidAmount, decimal RemainingAmount, bool IsCreditPaid)
        ResolveReturnSettlement(
            PaymentMethod paymentMethod,
            decimal netAmount,
            decimal appliedToOpenCredit,
            decimal requestedCashPortion)
    {
        var net = Math.Abs(netAmount);
        var leftover = Math.Max(0, net - Math.Max(0, appliedToOpenCredit));

        if (paymentMethod == PaymentMethod.Cash)
            return (leftover, net, 0m, true);

        var cash = Math.Min(Math.Clamp(requestedCashPortion, 0m, net), leftover);
        var remaining = leftover - cash;
        return (cash, cash, remaining, remaining <= 0);
    }

    /// <summary>
    /// يستنتج مبلغ حركة الصندوق المحفوظة لمرتجع عند الحذف/الاسترجاع.
    /// النقدي: المتبقي بعد مبالغ [RET-APPLIED]. الآجل: PaidAmount (الدفعة النقدية الفعلية).
    /// </summary>
    public static decimal ResolveStoredReturnCashMovement(
        PaymentMethod paymentMethod,
        decimal netAmount,
        decimal paidAmount,
        string? notes)
    {
        if (paymentMethod == PaymentMethod.Cash)
        {
            var applied = ParseAllocations(notes).Sum(a => a.Amount);
            return Math.Max(0, Math.Abs(netAmount) - applied);
        }

        return Math.Max(0, paidAmount);
    }
}
