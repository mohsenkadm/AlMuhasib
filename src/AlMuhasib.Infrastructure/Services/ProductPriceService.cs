using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductPriceService : IProductPriceService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBranchContext _branchContext;

    public ProductPriceService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IBranchContext branchContext)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _branchContext = branchContext;
    }

    public async Task<bool> ExistsAsync(int productId, int pricingTypeId, int? excludeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.ProductPrices.Where(p => p.ProductId == productId && p.PricingTypeId == pricingTypeId);
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<ProductPrice> UpsertAsync(ProductPrice price)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await UpsertInternalAsync(context, price);
    }

    public async Task UpsertManyAsync(IEnumerable<ProductPrice> prices)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        foreach (var price in prices)
            await UpsertInternalAsync(context, price, save: false);
        await context.SaveChangesAsync();
    }

    private async Task<ProductPrice> UpsertInternalAsync(AppDbContext context, ProductPrice price, bool save = true)
    {
        if (price.ProductId <= 0)
            throw new InvalidOperationException("المنتج مطلوب");
        if (price.PricingTypeId <= 0)
            throw new InvalidOperationException("نوع التسعير مطلوب");
        if (price.SalePrice < 0 || price.PurchasePrice < 0
            || price.SalePriceUsd < 0 || price.PurchasePriceUsd < 0)
            throw new InvalidOperationException("السعر لا يمكن أن يكون سالباً");

        ProductPrice? existing = null;
        if (price.Id > 0)
        {
            existing = await context.ProductPrices.FirstOrDefaultAsync(p => p.Id == price.Id);
        }

        existing ??= await context.ProductPrices
            .FirstOrDefaultAsync(p => p.ProductId == price.ProductId && p.PricingTypeId == price.PricingTypeId);

        if (existing is not null && existing.Id != price.Id && price.Id > 0)
            throw new InvalidOperationException("يوجد سعر لهذا المنتج ونوع التسعير مسبقاً");

        if (existing is null)
        {
            var duplicate = await context.ProductPrices
                .AnyAsync(p => p.ProductId == price.ProductId && p.PricingTypeId == price.PricingTypeId);
            if (duplicate)
                throw new InvalidOperationException("لا يمكن تكرار سعر منتج ونوع تسعير لنفس المنتج");

            price.Id = 0;
            price.CreatedBy = _currentUserService.Username;
            price.CreatedAt = DateTime.UtcNow;
            context.ProductPrices.Add(price);
            if (save)
                await context.SaveChangesAsync();
            return price;
        }

        var conflict = await context.ProductPrices.AnyAsync(p =>
            p.Id != existing.Id &&
            p.ProductId == price.ProductId &&
            p.PricingTypeId == price.PricingTypeId);
        if (conflict)
            throw new InvalidOperationException("لا يمكن تكرار سعر منتج ونوع تسعير لنفس المنتج");

        existing.ProductId = price.ProductId;
        existing.PricingTypeId = price.PricingTypeId;
        existing.SalePrice = price.SalePrice;
        existing.SalePriceUsd = price.SalePriceUsd;
        existing.PurchasePrice = price.PurchasePrice;
        existing.PurchasePriceUsd = price.PurchasePriceUsd;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = _currentUserService.Username;
        if (save)
            await context.SaveChangesAsync();
        return existing;
    }

    public async Task<ProductPrice?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductPrices
            .Include(p => p.Product)
            .ThenInclude(p => p.Category)
            .Include(p => p.PricingType)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(IEnumerable<ProductPrice> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        int? productId = null,
        int? pricingTypeId = null,
        int? categoryId = null,
        decimal? minSalePrice = null,
        decimal? maxSalePrice = null,
        decimal? minPurchasePrice = null,
        decimal? maxPurchasePrice = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var visibleProducts = ProductBranchVisibility.WhereVisibleInCurrentScope(
            context.Products.AsNoTracking(),
            context,
            _branchContext);
        var query = context.ProductPrices
            .Include(p => p.Product)
            .ThenInclude(p => p.Category)
            .Include(p => p.PricingType)
            .Where(p => visibleProducts.Any(vp => vp.Id == p.ProductId));

        if (productId.HasValue)
            query = query.Where(p => p.ProductId == productId.Value);
        if (pricingTypeId.HasValue)
            query = query.Where(p => p.PricingTypeId == pricingTypeId.Value);
        if (categoryId.HasValue)
            query = query.Where(p => p.Product.CategoryId == categoryId.Value);
        if (minSalePrice.HasValue)
            query = query.Where(p => p.SalePrice >= minSalePrice.Value);
        if (maxSalePrice.HasValue)
            query = query.Where(p => p.SalePrice <= maxSalePrice.Value);
        if (minPurchasePrice.HasValue)
            query = query.Where(p => p.PurchasePrice >= minPurchasePrice.Value);
        if (maxPurchasePrice.HasValue)
            query = query.Where(p => p.PurchasePrice <= maxPurchasePrice.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(p =>
                p.Product.Name.Contains(term) ||
                (p.Product.Barcode != null && p.Product.Barcode.Contains(term)) ||
                (p.Product.ScientificName != null && p.Product.ScientificName.Contains(term)) ||
                p.PricingType.Name.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Product.Name)
            .ThenBy(p => p.PricingType.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ProductPrices.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("سعر المنتج غير موجود");
        existing.MarkSoftDeleted(_currentUserService.Username);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ProductPrice>> GetByProductIdAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductPrices
            .Include(p => p.PricingType)
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.PricingType.IsDefault)
            .ThenBy(p => p.PricingType.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProductPrice>> GetByProductIdsAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductPrices
            .Include(p => p.PricingType)
            .Where(p => ids.Contains(p.ProductId))
            .OrderByDescending(p => p.PricingType.IsDefault)
            .ThenBy(p => p.PricingType.Name)
            .ToListAsync();
    }

    public async Task UpdatePurchasePriceAsync(int productId, int pricingTypeId, decimal purchasePrice)
        => await UpdatePricesOnPurchaseAsync(productId, pricingTypeId, purchasePrice, salePrice: null);

    public async Task UpdatePricesOnPurchaseAsync(
        int productId,
        int pricingTypeId,
        decimal? purchasePrice,
        decimal? salePrice)
    {
        if (purchasePrice is null && salePrice is null)
            return;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ProductPrices
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.PricingTypeId == pricingTypeId);

        if (existing is null)
        {
            context.ProductPrices.Add(new ProductPrice
            {
                ProductId = productId,
                PricingTypeId = pricingTypeId,
                PurchasePrice = Math.Max(0m, purchasePrice ?? 0m),
                SalePrice = Math.Max(0m, salePrice ?? 0m),
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            if (purchasePrice is decimal pp)
                existing.PurchasePrice = Math.Max(0m, pp);
            if (salePrice is decimal sp)
                existing.SalePrice = Math.Max(0m, sp);
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = _currentUserService.Username;
        }

        await context.SaveChangesAsync();
    }
}
