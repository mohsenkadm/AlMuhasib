using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models;
using Microsoft.EntityFrameworkCore;

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
                && !plans.Any(p => p.InvoiceId == i.Id && p.InstallmentType == InstallmentType.OpeningBalance))
            || i.InvoiceType == InvoiceType.SaleReturn);

    public static IQueryable<CloudInvoice> ForPurchasesTotals(IQueryable<CloudInvoice> invoices)
        => invoices.Where(i =>
            (i.InvoiceType == InvoiceType.Purchase
             && (i.Notes == null || !i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix)))
            || i.InvoiceType == InvoiceType.PurchaseReturn);

    /// <summary>مجموع NetAmount بإشارة صحيحة (المرتجعات تُطرح).</summary>
    public static async Task<decimal> SumSignedNetAsync(IQueryable<CloudInvoice> invoices)
        => await invoices.SumAsync(i => (decimal?)(
               i.InvoiceType == InvoiceType.SaleReturn || i.InvoiceType == InvoiceType.PurchaseReturn
                   ? -i.NetAmount
                   : i.NetAmount))
           ?? 0;

    public static decimal SumSignedNet(IEnumerable<CloudInvoice> invoices)
        => invoices.Sum(i => InvoiceFilters.SignedNetAmount(i.InvoiceType, i.NetAmount));

    public static decimal SignedNetAmount(CloudInvoice invoice) =>
        InvoiceFilters.SignedNetAmount(invoice.InvoiceType, invoice.NetAmount);
}
