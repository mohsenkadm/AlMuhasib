using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Core;

/// <summary>
/// استعلامات الفواتير — استبعاد الأرصدة الافتتاحية من المبيعات والأرباح والمشتريات التشغيلية،
/// وخصم مرتجعات المبيعات/المشتريات من الصافي (ممارسة محاسبية تجارية عراقية).
/// </summary>
public static class InvoiceFilters
{
    public static bool IsReturnType(InvoiceType type) =>
        type is InvoiceType.SaleReturn or InvoiceType.PurchaseReturn;

    /// <summary>مبلغ موقّع: المرتجع سالب، غير ذلك موجب.</summary>
    public static decimal SignedNetAmount(InvoiceType type, decimal netAmount) =>
        IsReturnType(type) ? -netAmount : netAmount;

    public static decimal SignedNetAmount(Invoice invoice) =>
        SignedNetAmount(invoice.InvoiceType, invoice.NetAmount);

    /// <summary>كمية بند موقّعة لمرتجع المبيعات (سالب).</summary>
    public static decimal SignedSaleLineQuantity(InvoiceType type, decimal quantity) =>
        type == InvoiceType.SaleReturn ? -Math.Abs(quantity) : quantity;

    /// <summary>قيمة بند موقّعة لمرتجع المبيعات (سالب).</summary>
    public static decimal SignedSaleLineAmount(InvoiceType type, decimal amount) =>
        type == InvoiceType.SaleReturn ? -Math.Abs(amount) : amount;

    /// <summary>
    /// إجماليات الربح/المبيعات لعملة واحدة — لا تُخلط مع عملة أخرى.
    /// الافتراضي دينار (توافق المستثمرين/لوحة التحكم). مرّر USD لإفصاح منفصل.
    /// </summary>
    public static IQueryable<Invoice> ForProfitAndSalesTotals(
        IQueryable<Invoice> invoices,
        IQueryable<InstallmentPlan> plans,
        AccountingCurrency currency = AccountingCurrency.IQD)
        => ApplyOperationalSalesTypes(invoices, plans)
            .Where(i => i.Currency == currency);

    /// <summary>إجماليات الربح/المبيعات حسب نطاق العملة — عند All لا يُفلتر بعملة (أفصح *Usd منفصلاً).</summary>
    public static IQueryable<Invoice> ForProfitAndSalesTotals(
        IQueryable<Invoice> invoices,
        IQueryable<InstallmentPlan> plans,
        ReportCurrencyScope scope)
    {
        var q = ApplyOperationalSalesTypes(invoices, plans);
        var strict = ReportCurrencyScopeHelper.ToStrictFilter(scope);
        return strict is null ? q : q.Where(i => i.Currency == strict.Value);
    }

    /// <summary>إجماليات المشتريات لعملة واحدة — الافتراضي دينار.</summary>
    public static IQueryable<Invoice> ForPurchasesTotals(
        IQueryable<Invoice> invoices,
        AccountingCurrency currency = AccountingCurrency.IQD)
        => invoices.Where(i =>
            i.Currency == currency &&
            ((i.InvoiceType == InvoiceType.Purchase
              && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)))
             || i.InvoiceType == InvoiceType.PurchaseReturn));

    public static IQueryable<Invoice> ForPurchasesTotals(
        IQueryable<Invoice> invoices,
        ReportCurrencyScope scope)
    {
        var q = invoices.Where(i =>
            (i.InvoiceType == InvoiceType.Purchase
             && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)))
            || i.InvoiceType == InvoiceType.PurchaseReturn);
        var strict = ReportCurrencyScopeHelper.ToStrictFilter(scope);
        return strict is null ? q : q.Where(i => i.Currency == strict.Value);
    }

    private static IQueryable<Invoice> ApplyOperationalSalesTypes(
        IQueryable<Invoice> invoices,
        IQueryable<InstallmentPlan> plans)
        => invoices.Where(i =>
            (i.InvoiceType == InvoiceType.Sale
             && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)))
            || (i.InvoiceType == InvoiceType.Installment
                && !plans.Any(p => p.InvoiceId == i.Id && p.InstallmentType == InstallmentType.OpeningBalance))
            || i.InvoiceType == InvoiceType.SaleReturn);

    public static decimal SumSignedNet(IEnumerable<Invoice> invoices)
        => invoices.Sum(SignedNetAmount);

    public static decimal SumSignedNet(
        IEnumerable<(InvoiceType Type, decimal NetAmount)> items)
        => items.Sum(x => SignedNetAmount(x.Type, x.NetAmount));
}
