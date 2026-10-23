using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

/// <summary>
/// Shared settlement: Paid/Remaining from allocations; customer BalanceIqd/Usd from approved remainings.
/// </summary>
internal static class CustomsSettlementHelper
{
    public static async Task RecomputeInvoiceSettlementAsync(
        CustomsDbContext db,
        IEnumerable<int> invoiceIds,
        CancellationToken cancellationToken = default)
    {
        var ids = invoiceIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0) return;

        var invoices = await db.CustomsInvoices
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(cancellationToken);

        var paidByInvoice = await db.CustomsReceiptAllocations.AsNoTracking()
            .Where(a => ids.Contains(a.InvoiceId) && !a.IsDeleted)
            .GroupBy(a => a.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Paid = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Paid, cancellationToken);

        foreach (var invoice in invoices)
        {
            paidByInvoice.TryGetValue(invoice.Id, out var paid);
            if (paid > invoice.TotalAmount)
            {
                throw new InvalidOperationException(
                    $"مجموع التخصيصات ({paid:N0}) يتجاوز إجمالي الفاتورة {invoice.InvoiceNumber} ({invoice.TotalAmount:N0}).");
            }

            invoice.PaidAmount = paid;
            invoice.RemainingAmount = CustomsFinancialRules.ComputeRemaining(invoice.TotalAmount, paid);
            invoice.UpdatedAt = DateTime.UtcNow;
        }
    }

    public static async Task RecomputeCustomerBalanceAsync(
        CustomsDbContext db,
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await db.CustomsCustomers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null) return;

        var approved = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.Status == CustomsInvoiceStatus.Approved)
            .Select(i => new { i.Currency, i.RemainingAmount })
            .ToListAsync(cancellationToken);

        customer.BalanceIqd = approved
            .Where(i => i.Currency == AccountingCurrency.IQD)
            .Sum(i => i.RemainingAmount);
        customer.BalanceUsd = approved
            .Where(i => i.Currency == AccountingCurrency.USD)
            .Sum(i => i.RemainingAmount);
        customer.UpdatedAt = DateTime.UtcNow;
    }
}
