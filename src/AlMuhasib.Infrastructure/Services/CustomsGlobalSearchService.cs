using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CustomsGlobalSearchService : IGlobalSearchService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;

    public CustomsGlobalSearchService(IDbContextFactory<CustomsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<GlobalSearchHit>> SearchAsync(
        string term,
        int maxResults = 30,
        CancellationToken cancellationToken = default)
    {
        term = term?.Trim() ?? string.Empty;
        if (term.Length < 2)
            return [];

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var like = $"%{term}%";
        var take = Math.Max(1, maxResults);

        var customers = await context.CustomsCustomers.AsNoTracking()
            .Where(c =>
                EF.Functions.Like(c.Name, like) ||
                EF.Functions.Like(c.CompanyName, like) ||
                EF.Functions.Like(c.Phone, like))
            .OrderBy(c => c.Name)
            .Take(take)
            .Select(c => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.Customer,
                EntityId = c.Id,
                Title = c.Name,
                Subtitle = string.IsNullOrWhiteSpace(c.CompanyName) ? c.Phone : c.CompanyName,
                ScreenName = "CustomsCustomers"
            })
            .ToListAsync(cancellationToken);

        var remaining = take - customers.Count;
        if (remaining <= 0)
            return customers;

        var invoices = await context.CustomsInvoices.AsNoTracking()
            .Where(i =>
                EF.Functions.Like(i.InvoiceNumber, like) ||
                EF.Functions.Like(i.Description, like))
            .OrderByDescending(i => i.InvoiceDate)
            .Take(remaining)
            .Select(i => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.SalesInvoice,
                EntityId = i.Id,
                Title = i.InvoiceNumber,
                Subtitle = i.Description,
                ScreenName = "CustomsInvoices"
            })
            .ToListAsync(cancellationToken);

        return customers.Concat(invoices).ToList();
    }
}
