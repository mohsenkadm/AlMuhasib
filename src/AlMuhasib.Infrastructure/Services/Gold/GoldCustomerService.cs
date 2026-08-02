using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldCustomerService : IGoldCustomerService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldCustomerService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<GoldCustomerListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? activeOnly = true,
        bool creditOnly = false,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldCustomers.AsNoTracking().AsQueryable();

        if (activeOnly == true)
            query = query.Where(c => c.IsActive);
        else if (activeOnly == false)
            query = query.Where(c => !c.IsActive);

        if (creditOnly)
            query = query.Where(c => c.CreditBalanceIqd > 0 || c.CreditBalanceUsd > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(term) ||
                c.Phone.Contains(term) ||
                c.Address.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var customers = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var customerIds = customers.Select(c => c.Id).ToList();
        var invoiceStats = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.CustomerId.HasValue &&
                        customerIds.Contains(i.CustomerId.Value) &&
                        i.Status != GoldInvoiceStatus.Cancelled)
            .GroupBy(i => i.CustomerId!.Value)
            .Select(g => new
            {
                CustomerId = g.Key,
                OpenCount = g.Count(x => x.RemainingAmount > 0),
                LastDate = g.Max(x => (DateTime?)x.InvoiceDate)
            })
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        var items = customers.Select(c =>
        {
            invoiceStats.TryGetValue(c.Id, out var stats);
            return new GoldCustomerListItem
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Address = c.Address,
                CreditBalanceIqd = c.CreditBalanceIqd,
                CreditBalanceUsd = c.CreditBalanceUsd,
                IsActive = c.IsActive,
                OpenInvoiceCount = stats?.OpenCount ?? 0,
                LastTransactionDate = stats?.LastDate
            };
        }).ToList();

        return (items, totalCount);
    }

    public async Task<GoldCustomer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldCustomers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<GoldCustomer> CreateAsync(GoldCustomer customer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new InvalidOperationException("اسم الزبون مطلوب");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        customer.IsActive = true;
        await context.GoldCustomers.AddAsync(customer, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<GoldCustomer> UpdateAsync(GoldCustomer customer, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.GoldCustomers.FirstOrDefaultAsync(c => c.Id == customer.Id, cancellationToken)
            ?? throw new InvalidOperationException("الزبون غير موجود");

        existing.Name = customer.Name;
        existing.Phone = customer.Phone ?? string.Empty;
        existing.Address = customer.Address ?? string.Empty;
        existing.Notes = customer.Notes ?? string.Empty;
        existing.IsActive = customer.IsActive;
        // Credit balances are updated via sales/payments, not free-form edits here,
        // unless explicitly provided as absolute values from admin UI.
        existing.CreditBalanceIqd = customer.CreditBalanceIqd;
        existing.CreditBalanceUsd = customer.CreditBalanceUsd;

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await context.GoldCustomers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الزبون غير موجود");

        if (customer.CreditBalanceIqd > 0 || customer.CreditBalanceUsd > 0)
            throw new InvalidOperationException("لا يمكن حذف زبون عليه ذمم");

        customer.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoldInvoiceListItem>> GetCustomerInvoicesAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var invoices = await context.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .ToListAsync(cancellationToken);

        return invoices.Select(GoldCurrencyHelper.ToListItem).ToList();
    }

    public async Task<IReadOnlyList<GoldCustomerListItem>> GetOverdueCreditCustomersAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await context.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GoldSettings.SingletonId, cancellationToken);
        var thresholdDays = settings?.OverdueDaysThreshold ?? 30;
        var cutoff = DateTime.Today.AddDays(-thresholdDays);

        var openInvoices = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.CustomerId.HasValue &&
                        i.RemainingAmount > 0 &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceDate.Date <= cutoff)
            .Select(i => new { CustomerId = i.CustomerId!.Value, i.InvoiceDate })
            .ToListAsync(cancellationToken);

        var overdueCustomerIds = openInvoices.Select(x => x.CustomerId).Distinct().ToList();
        if (overdueCustomerIds.Count == 0)
            return [];

        var customers = await context.GoldCustomers.AsNoTracking()
            .Where(c => overdueCustomerIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var openCounts = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.CustomerId.HasValue &&
                        overdueCustomerIds.Contains(i.CustomerId.Value) &&
                        i.RemainingAmount > 0 &&
                        i.Status != GoldInvoiceStatus.Cancelled)
            .GroupBy(i => i.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Count = g.Count(), LastDate = g.Max(x => (DateTime?)x.InvoiceDate) })
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        return customers.Select(c =>
        {
            openCounts.TryGetValue(c.Id, out var stats);
            return new GoldCustomerListItem
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Address = c.Address,
                CreditBalanceIqd = c.CreditBalanceIqd,
                CreditBalanceUsd = c.CreditBalanceUsd,
                IsActive = c.IsActive,
                OpenInvoiceCount = stats?.Count ?? 0,
                LastTransactionDate = stats?.LastDate
            };
        }).ToList();
    }

    internal static void AdjustCredit(
        GoldCustomer customer,
        GoldCurrency currency,
        decimal delta)
    {
        if (currency == GoldCurrency.IQD)
            customer.CreditBalanceIqd = GoldCurrencyHelper.Round(customer.CreditBalanceIqd + delta);
        else
            customer.CreditBalanceUsd = GoldCurrencyHelper.Round(customer.CreditBalanceUsd + delta);

        if (customer.CreditBalanceIqd < 0)
            customer.CreditBalanceIqd = 0;
        if (customer.CreditBalanceUsd < 0)
            customer.CreditBalanceUsd = 0;
    }
}
