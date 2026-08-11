using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public static class CloudInvoiceFilters
{
    public static IQueryable<CloudInvoice> ForProfitAndSalesTotals(
        IQueryable<CloudInvoice> invoices,
        IQueryable<CloudInstallmentPlan> plans)
        => invoices.Where(i =>
            (i.InvoiceType == InvoiceType.Sale
             && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)))
            || (i.InvoiceType == InvoiceType.Installment
                && !plans.Any(p => p.InvoiceId == i.Id && p.InstallmentType == InstallmentType.OpeningBalance)));

    public static IQueryable<CloudInvoice> ForPurchasesTotals(IQueryable<CloudInvoice> invoices)
        => invoices.Where(i =>
            i.InvoiceType == InvoiceType.Purchase
            && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)));
}
