using AlMuhasib.Core.Enums.Gold;
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
        var results = new List<GlobalSearchHit>(maxResults);

        var invoices = await context.GoldInvoices.AsNoTracking()
            .Where(i =>
                EF.Functions.Like(i.InvoiceNumber, like) ||
                (i.Customer != null && EF.Functions.Like(i.Customer.Name, like)) ||
                EF.Functions.Like(i.Notes, like))
            .OrderByDescending(i => i.InvoiceDate)
            .Take(maxResults)
            .Select(i => new GlobalSearchHit
            {
                Kind = i.InvoiceType == GoldInvoiceType.Sale
                    ? GlobalSearchKind.SalesInvoice
                    : GlobalSearchKind.PurchaseInvoice,
                EntityId = i.Id,
                Title = i.InvoiceNumber,
                Subtitle = i.Customer != null ? i.Customer.Name : i.InvoiceType.ToString(),
                ScreenName = GoldPermissionRegistryScreen.GoldInvoices
            })
            .ToListAsync(cancellationToken);
        results.AddRange(invoices);

        if (results.Count < maxResults)
        {
            var remaining = maxResults - results.Count;
            var customers = await context.GoldCustomers.AsNoTracking()
                .Where(c =>
                    EF.Functions.Like(c.Name, like) ||
                    EF.Functions.Like(c.Phone, like))
                .OrderBy(c => c.Name)
                .Take(remaining)
                .Select(c => new GlobalSearchHit
                {
                    Kind = GlobalSearchKind.Customer,
                    EntityId = c.Id,
                    Title = c.Name,
                    Subtitle = c.Phone,
                    ScreenName = GoldPermissionRegistryScreen.GoldCustomers
                })
                .ToListAsync(cancellationToken);
            results.AddRange(customers);
        }

        if (results.Count < maxResults)
        {
            var remaining = maxResults - results.Count;
            var items = await context.GoldItems.AsNoTracking()
                .Where(i =>
                    EF.Functions.Like(i.Name, like) ||
                    EF.Functions.Like(i.Barcode, like) ||
                    EF.Functions.Like(i.Category, like))
                .OrderBy(i => i.Name)
                .Take(remaining)
                .Select(i => new GlobalSearchHit
                {
                    Kind = GlobalSearchKind.Product,
                    EntityId = i.Id,
                    Title = i.Name,
                    Subtitle = string.IsNullOrEmpty(i.Barcode)
                        ? $"عيار {i.KaratValue}"
                        : i.Barcode,
                    ScreenName = GoldPermissionRegistryScreen.GoldItems
                })
                .ToListAsync(cancellationToken);
            results.AddRange(items);
        }

        return results.Take(maxResults).ToList();
    }
}

internal static class GoldPermissionRegistryScreen
{
    public const string GoldInvoices = "GoldInvoices";
    public const string GoldCustomers = "GoldCustomers";
    public const string GoldItems = "GoldItems";
}
