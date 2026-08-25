using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldPartyQuickDetailService : IPartyQuickDetailService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldPartyQuickDetailService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<PartyQuickDetailResult?> GetCustomerDetailAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await context.GoldCustomers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
            return null;

        var invoices = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.Status != Core.Enums.Gold.GoldInvoiceStatus.Cancelled)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        var last = invoices.FirstOrDefault();
        var balance = customer.CreditBalanceIqd + customer.CreditBalanceUsd;

        return new PartyQuickDetailResult
        {
            PartyType = PersonPartyType.Customer,
            Id = customer.Id,
            Name = customer.Name,
            TypeLabel = "زبون",
            Phone = string.IsNullOrWhiteSpace(customer.Phone) ? null : customer.Phone,
            Address = string.IsNullOrWhiteSpace(customer.Address) ? null : customer.Address,
            Notes = string.IsNullOrWhiteSpace(customer.Notes)
                ? (customer.GoldCreditGrams > 0 ? $"ذمة ذهب: {customer.GoldCreditGrams:N3} غ" : null)
                : customer.Notes,
            Balance = balance,
            TotalDealAmount = invoices.Sum(i => i.TotalAmountIqd + i.TotalAmountUsd),
            DealCount = invoices.Count,
            LastDealDate = last?.InvoiceDate,
            LastDealDescription = last is null ? null : $"فاتورة {last.InvoiceNumber}",
            LastDealAmount = last?.TotalAmountIqd > 0 ? last.TotalAmountIqd : last?.TotalAmountUsd,
            RecentTimeline = invoices.Take(8).Select(i => new PartyQuickTimelineRow
            {
                Date = i.InvoiceDate,
                Description = i.InvoiceNumber,
                Debit = i.RemainingAmount > 0 ? i.RemainingAmount : 0,
                Credit = i.PaidAmount,
                RunningBalance = i.RemainingAmount
            }).ToList()
        };
    }

    public async Task<PartyQuickDetailResult?> GetSupplierDetailAsync(
        int supplierId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var supplier = await context.GoldSuppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);
        if (supplier is null)
            return null;

        var invoices = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.SupplierId == supplierId && i.Status != Core.Enums.Gold.GoldInvoiceStatus.Cancelled)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        var last = invoices.FirstOrDefault();
        var balance = supplier.CreditBalanceIqd + supplier.CreditBalanceUsd;

        return new PartyQuickDetailResult
        {
            PartyType = PersonPartyType.Supplier,
            Id = supplier.Id,
            Name = supplier.Name,
            TypeLabel = "مورد",
            Phone = string.IsNullOrWhiteSpace(supplier.Phone) ? null : supplier.Phone,
            Address = string.IsNullOrWhiteSpace(supplier.Address) ? null : supplier.Address,
            Notes = supplier.Notes,
            Balance = balance,
            TotalDealAmount = invoices.Sum(i => i.TotalAmountIqd + i.TotalAmountUsd),
            DealCount = invoices.Count,
            LastDealDate = last?.InvoiceDate,
            LastDealDescription = last is null ? null : $"فاتورة {last.InvoiceNumber}",
            LastDealAmount = last?.TotalAmountIqd > 0 ? last.TotalAmountIqd : last?.TotalAmountUsd,
            RecentTimeline = invoices.Take(8).Select(i => new PartyQuickTimelineRow
            {
                Date = i.InvoiceDate,
                Description = i.InvoiceNumber,
                Debit = i.RemainingAmount > 0 ? i.RemainingAmount : 0,
                Credit = i.PaidAmount,
                RunningBalance = i.RemainingAmount
            }).ToList()
        };
    }
}
