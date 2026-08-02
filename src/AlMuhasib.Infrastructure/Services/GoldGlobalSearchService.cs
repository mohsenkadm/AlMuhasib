using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class GoldGlobalSearchService : IGlobalSearchService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldGlobalSearchService(IDbContextFactory<GoldDbContext> contextFactory)
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
        var hits = new List<GlobalSearchHit>();

        var customers = await context.GoldCustomers.AsNoTracking()
            .Where(c => EF.Functions.Like(c.Name, like) || EF.Functions.Like(c.Phone, like))
            .OrderBy(c => c.Name)
            .Take(maxResults)
            .Select(c => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.Customer,
                EntityId = c.Id,
                Title = c.Name,
                Subtitle = c.Phone,
                ScreenName = "GoldCustomers"
            })
            .ToListAsync(cancellationToken);
        hits.AddRange(customers);

        if (hits.Count < maxResults)
        {
            var invoices = await context.GoldInvoices.AsNoTracking()
                .Include(i => i.Customer)
                .Where(i => EF.Functions.Like(i.InvoiceNumber, like) ||
                            (i.Customer != null && EF.Functions.Like(i.Customer.Name, like)))
                .OrderByDescending(i => i.InvoiceDate)
                .Take(maxResults - hits.Count)
                .ToListAsync(cancellationToken);

            hits.AddRange(invoices.Select(i => new GlobalSearchHit
            {
                Kind = i.InvoiceType == Core.Enums.Gold.GoldInvoiceType.Sale
                    ? GlobalSearchKind.SalesInvoice
                    : GlobalSearchKind.PurchaseInvoice,
                EntityId = i.Id,
                Title = i.InvoiceNumber,
                Subtitle = i.Customer?.Name ?? i.Notes,
                ScreenName = i.InvoiceType == Core.Enums.Gold.GoldInvoiceType.Sale
                    ? "GoldSales"
                    : "GoldPurchases"
            }));
        }

        if (hits.Count < maxResults)
        {
            var items = await context.GoldItems.AsNoTracking()
                .Where(i => EF.Functions.Like(i.Name, like) || EF.Functions.Like(i.Barcode, like))
                .OrderBy(i => i.Name)
                .Take(maxResults - hits.Count)
                .Select(i => new GlobalSearchHit
                {
                    Kind = GlobalSearchKind.Product,
                    EntityId = i.Id,
                    Title = i.Name,
                    Subtitle = i.Barcode,
                    ScreenName = "GoldInventory"
                })
                .ToListAsync(cancellationToken);
            hits.AddRange(items);
        }

        return hits.Take(maxResults).ToList();
    }
}
