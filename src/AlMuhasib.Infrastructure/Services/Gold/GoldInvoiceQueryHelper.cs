using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

internal static class GoldInvoiceQueryHelper
{
    public static async Task<(IReadOnlyList<GoldInvoiceListItem> Items, int TotalCount)> GetPagedAsync(
        IDbContextFactory<GoldDbContext> contextFactory,
        GoldInvoiceType invoiceType,
        int page,
        int pageSize,
        string? search,
        DateTime? dateFrom,
        DateTime? dateTo,
        GoldInvoiceStatus? status,
        int? customerId,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Where(i => i.InvoiceType == invoiceType);

        if (dateFrom.HasValue)
            query = query.Where(i => i.InvoiceDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(i => i.InvoiceDate.Date <= dateTo.Value.Date);
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);
        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(term) ||
                i.Notes.Contains(term) ||
                (i.Customer != null && i.Customer.Name.Contains(term)) ||
                (i.Supplier != null && i.Supplier.Name.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (invoices.Select(GoldCurrencyHelper.ToListItem).ToList(), totalCount);
    }

    public static async Task<string> GetNextInvoiceNumberAsync(
        GoldDbContext context,
        GoldInvoiceType type,
        CancellationToken cancellationToken)
    {
        var prefix = type switch
        {
            GoldInvoiceType.Sale => "GS",
            GoldInvoiceType.Purchase => "GP",
            GoldInvoiceType.Exchange => "GX",
            GoldInvoiceType.SaleReturn => "GR",
            _ => "G"
        };
        var last = await context.GoldInvoices
            .IgnoreQueryFilters()
            .Where(i => i.InvoiceType == type && i.InvoiceNumber.StartsWith(prefix + "-"))
            .OrderByDescending(i => i.Id)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNum = 1;
        if (last is not null)
        {
            var parts = last.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lastNum))
                nextNum = lastNum + 1;
        }

        return $"{prefix}-{nextNum:D4}";
    }
}
