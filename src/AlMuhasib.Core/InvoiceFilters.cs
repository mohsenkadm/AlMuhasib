using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core;

/// <summary>استعلامات الفواتير — استبعاد أرصدة الأقساط الافتتاحية من المبيعات والأرباح</summary>
public static class InvoiceFilters
{
    public static IQueryable<Invoice> ForProfitAndSalesTotals(
        IQueryable<Invoice> invoices,
        IQueryable<InstallmentPlan> plans)
        => invoices.Where(i =>
            i.InvoiceType == InvoiceType.Sale
            || (i.InvoiceType == InvoiceType.Installment
                && !plans.Any(p => p.InvoiceId == i.Id && p.InstallmentType == InstallmentType.OpeningBalance)));
}
