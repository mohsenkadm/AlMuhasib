using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Core;

/// <summary>استعلامات الفواتير — استبعاد الأرصدة الافتتاحية من المبيعات والأرباح والمشتريات التشغيلية</summary>
public static class InvoiceFilters
{
    public static IQueryable<Invoice> ForProfitAndSalesTotals(
        IQueryable<Invoice> invoices,
        IQueryable<InstallmentPlan> plans)
        => invoices.Where(i =>
            (i.InvoiceType == InvoiceType.Sale
             && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)))
            || (i.InvoiceType == InvoiceType.Installment
                && !plans.Any(p => p.InvoiceId == i.Id && p.InstallmentType == InstallmentType.OpeningBalance)));

    public static IQueryable<Invoice> ForPurchasesTotals(IQueryable<Invoice> invoices)
        => invoices.Where(i =>
            i.InvoiceType == InvoiceType.Purchase
            && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)));
}
