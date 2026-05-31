using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class GlobalSearchService : IGlobalSearchService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private const int PerCategoryLimit = 8;

    public GlobalSearchService(IDbContextFactory<AppDbContext> contextFactory)
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

        var customers = await context.Customers.AsNoTracking()
            .Where(c => EF.Functions.Like(c.Name, like) || (c.Phone != null && EF.Functions.Like(c.Phone, like)))
            .OrderBy(c => c.Name)
            .Take(PerCategoryLimit)
            .Select(c => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.Customer,
                EntityId = c.Id,
                Title = c.Name,
                Subtitle = c.Phone ?? "عميل",
                ScreenName = "Customers"
            })
            .ToListAsync(cancellationToken);
        hits.AddRange(customers);

        var suppliers = await context.Suppliers.AsNoTracking()
            .Where(s => EF.Functions.Like(s.Name, like) || (s.Phone != null && EF.Functions.Like(s.Phone, like)))
            .OrderBy(s => s.Name)
            .Take(PerCategoryLimit)
            .Select(s => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.Supplier,
                EntityId = s.Id,
                Title = s.Name,
                Subtitle = s.Phone ?? "مورد",
                ScreenName = "Suppliers"
            })
            .ToListAsync(cancellationToken);
        hits.AddRange(suppliers);

        var products = await context.Products.AsNoTracking()
            .Where(p => EF.Functions.Like(p.Name, like)
                        || (p.Barcode != null && EF.Functions.Like(p.Barcode, like)))
            .OrderBy(p => p.Name)
            .Take(PerCategoryLimit)
            .Select(p => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.Product,
                EntityId = p.Id,
                Title = p.Name,
                Subtitle = p.Barcode ?? "منتج",
                ScreenName = "Products"
            })
            .ToListAsync(cancellationToken);
        hits.AddRange(products);

        var sales = await context.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment)
                        && (EF.Functions.Like(i.InvoiceNumber, like)
                            || (i.Customer != null && EF.Functions.Like(i.Customer.Name, like))))
            .OrderByDescending(i => i.Date)
            .Take(PerCategoryLimit)
            .Select(i => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.SalesInvoice,
                EntityId = i.Id,
                Title = i.InvoiceNumber,
                Subtitle = "فاتورة مبيعات — " + (i.Customer != null ? i.Customer.Name : ""),
                ScreenName = "SaleInvoice"
            })
            .ToListAsync(cancellationToken);
        hits.AddRange(sales);

        var purchases = await context.Invoices.AsNoTracking()
            .Include(i => i.Supplier)
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && (EF.Functions.Like(i.InvoiceNumber, like)
                            || (i.Supplier != null && EF.Functions.Like(i.Supplier.Name, like))))
            .OrderByDescending(i => i.Date)
            .Take(PerCategoryLimit)
            .Select(i => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.PurchaseInvoice,
                EntityId = i.Id,
                Title = i.InvoiceNumber,
                Subtitle = "فاتورة مشتريات — " + (i.Supplier != null ? i.Supplier.Name : ""),
                ScreenName = "PurchaseInvoice"
            })
            .ToListAsync(cancellationToken);
        hits.AddRange(purchases);

        var vouchers = await context.Vouchers.AsNoTracking()
            .Where(v => EF.Functions.Like(v.VoucherNumber, like))
            .OrderByDescending(v => v.Date)
            .Take(PerCategoryLimit)
            .Select(v => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.Voucher,
                EntityId = v.Id,
                Title = v.VoucherNumber,
                Subtitle = "سند — " + v.VoucherType.ToString(),
                ScreenName = "Vouchers"
            })
            .ToListAsync(cancellationToken);
        hits.AddRange(vouchers);

        return hits.Take(maxResults).ToList();
    }
}
