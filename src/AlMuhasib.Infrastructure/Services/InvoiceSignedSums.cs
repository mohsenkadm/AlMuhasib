using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>مجاميع الفواتير بإشارة صحيحة (خصم المرتجعات) — يتطلب EF.</summary>
public static class InvoiceSignedSums
{
    public static async Task<decimal> SumSignedNetAsync(IQueryable<Invoice> invoices)
        => await invoices.SumAsync(i => (decimal?)(
               i.InvoiceType == InvoiceType.SaleReturn || i.InvoiceType == InvoiceType.PurchaseReturn
                   ? -i.NetAmount
                   : i.NetAmount))
           ?? 0;
}
