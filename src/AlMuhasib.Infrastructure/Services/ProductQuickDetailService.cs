using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductQuickDetailService : IProductQuickDetailService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProductQuickDetailService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ProductQuickDetailResult?> GetDetailAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var product = await context.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
            return null;

        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Where(ws => ws.ProductId == productId)
            .Select(ws => new ProductQuickStockRow
            {
                WarehouseName = ws.Warehouse != null ? ws.Warehouse.Name : "—",
                Quantity = ws.Quantity
            })
            .OrderBy(s => s.WarehouseName)
            .ToListAsync(cancellationToken);

        var prices = await context.ProductPrices.AsNoTracking()
            .Where(pp => pp.ProductId == productId)
            .ToListAsync(cancellationToken);

        var saleItems = await context.InvoiceItems.AsNoTracking()
            .Where(ii => ii.ProductId == productId &&
                         (ii.Invoice.InvoiceType == InvoiceType.Sale ||
                          ii.Invoice.InvoiceType == InvoiceType.Installment))
            .Select(ii => new { ii.UnitPrice, ii.Quantity, Date = ii.Invoice.Date })
            .ToListAsync(cancellationToken);

        var purchaseItems = await context.InvoiceItems.AsNoTracking()
            .Where(ii => ii.ProductId == productId &&
                         ii.Invoice.InvoiceType == InvoiceType.Purchase)
            .Select(ii => new { ii.UnitPrice, ii.Quantity, Date = ii.Invoice.Date })
            .ToListAsync(cancellationToken);

        var lastSale = saleItems.OrderByDescending(x => x.Date).FirstOrDefault();
        var lastPurchase = purchaseItems.OrderByDescending(x => x.Date).FirstOrDefault();

        return new ProductQuickDetailResult
        {
            Id = product.Id,
            Name = product.Name,
            Barcode = product.Barcode,
            CategoryName = product.Category?.Name,
            Description = product.Description,
            TotalStockQuantity = stocks.Sum(s => s.Quantity),
            StocksByWarehouse = stocks,
            LastPurchasePrice = lastPurchase?.UnitPrice,
            LastPurchaseDate = lastPurchase?.Date,
            LastSalePrice = lastSale?.UnitPrice,
            LastSaleDate = lastSale?.Date,
            CurrentSalePrice = prices.Where(p => p.SalePrice > 0).Select(p => (decimal?)p.SalePrice).FirstOrDefault()
                               ?? prices.Select(p => (decimal?)p.SalePrice).FirstOrDefault(),
            CurrentPurchasePrice = prices.Where(p => p.PurchasePrice > 0).Select(p => (decimal?)p.PurchasePrice).FirstOrDefault()
                                   ?? prices.Select(p => (decimal?)p.PurchasePrice).FirstOrDefault(),
            SaleDealCount = saleItems.Count,
            TotalSoldQuantity = saleItems.Sum(x => x.Quantity),
            PurchaseDealCount = purchaseItems.Count,
            TotalPurchasedQuantity = purchaseItems.Sum(x => x.Quantity)
        };
    }
}

public sealed class NoOpProductQuickDetailService : IProductQuickDetailService
{
    public Task<ProductQuickDetailResult?> GetDetailAsync(int productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ProductQuickDetailResult?>(null);
}
