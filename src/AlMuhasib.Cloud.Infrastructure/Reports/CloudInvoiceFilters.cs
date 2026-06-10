using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public static class CloudInvoiceFilters
{
    public static IQueryable<CloudInvoice> ForProfitAndSalesTotals(
        IQueryable<CloudInvoice> invoices,
        IQueryable<CloudInstallmentPlan> plans)
        => invoices.Where(i =>
            i.InvoiceType == InvoiceType.Sale
            || (i.InvoiceType == InvoiceType.Installment
                && !plans.Any(p => p.InvoiceId == i.Id && p.InstallmentType == InstallmentType.OpeningBalance)));
}
