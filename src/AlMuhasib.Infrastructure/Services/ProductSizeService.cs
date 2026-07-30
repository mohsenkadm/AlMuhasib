using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductSizeService : IProductSizeService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProductSizeService(IDbContextFactory<AppDbContext> contextFactory) =>
        _contextFactory = contextFactory;

    public async Task<IReadOnlyList<ProductSize>> GetByProductAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductSizes.AsNoTracking()
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.SizeName)
            .ToListAsync();
    }

    public async Task<bool> HasSizesAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductSizes.AsNoTracking().AnyAsync(s => s.ProductId == productId);
    }

    public async Task<ProductSize> SaveAsync(ProductSize size)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var name = (size.SizeName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("اسم القياس مطلوب");

        if (size.Id == 0)
        {
            var duplicate = await context.ProductSizes.AnyAsync(s =>
                s.ProductId == size.ProductId && s.SizeName == name);
            if (duplicate)
                throw new InvalidOperationException($"القياس «{name}» موجود مسبقاً لهذا المنتج");

            if (size.SortOrder <= 0)
            {
                var maxOrder = await context.ProductSizes
                    .Where(s => s.ProductId == size.ProductId)
                    .Select(s => (int?)s.SortOrder)
                    .MaxAsync() ?? 0;
                size.SortOrder = maxOrder + 1;
            }

            size.SizeName = name;
            context.ProductSizes.Add(size);
        }
        else
        {
            var existing = await context.ProductSizes.FirstOrDefaultAsync(s => s.Id == size.Id)
                ?? throw new InvalidOperationException("القياس غير موجود");

            var duplicate = await context.ProductSizes.AnyAsync(s =>
                s.ProductId == existing.ProductId && s.SizeName == name && s.Id != existing.Id);
            if (duplicate)
                throw new InvalidOperationException($"القياس «{name}» موجود مسبقاً لهذا المنتج");

            existing.SizeName = name;
            existing.SortOrder = size.SortOrder;
            size = existing;
        }

        await context.SaveChangesAsync();
        return size;
    }

    public async Task DeleteAsync(int sizeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var size = await context.ProductSizes.FirstOrDefaultAsync(s => s.Id == sizeId);
        if (size is null) return;

        var hasStock = await context.ProductSizeStocks.AnyAsync(s =>
            s.ProductSizeId == sizeId && s.Quantity != 0);
        if (hasStock)
            throw new InvalidOperationException("لا يمكن حذف قياس له رصيد في المخازن");

        var emptyStocks = await context.ProductSizeStocks
            .Where(s => s.ProductSizeId == sizeId)
            .ToListAsync();
        context.ProductSizeStocks.RemoveRange(emptyStocks);
        context.ProductSizes.Remove(size);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ProductSizeStock>> GetStocksAsync(int productId, int? warehouseId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var q = context.ProductSizeStocks.AsNoTracking()
            .Include(s => s.ProductSize)
            .Where(s => s.ProductId == productId);
        if (warehouseId.HasValue)
            q = q.Where(s => s.WarehouseId == warehouseId.Value);
        return await q
            .OrderBy(s => s.ProductSize.SortOrder)
            .ThenBy(s => s.ProductSize.SizeName)
            .ToListAsync();
    }

    public async Task AdjustStockAsync(int productId, int productSizeId, int warehouseId, decimal quantityDelta)
    {
        if (quantityDelta == 0) return;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var stock = await context.ProductSizeStocks.FirstOrDefaultAsync(s =>
            s.ProductId == productId
            && s.ProductSizeId == productSizeId
            && s.WarehouseId == warehouseId);

        if (stock is null)
        {
            if (quantityDelta < 0)
                throw new InvalidOperationException("رصيد القياس غير كافٍ");

            context.ProductSizeStocks.Add(new ProductSizeStock
            {
                ProductId = productId,
                ProductSizeId = productSizeId,
                WarehouseId = warehouseId,
                Quantity = quantityDelta
            });
        }
        else
        {
            stock.Quantity += quantityDelta;
            if (stock.Quantity < 0)
                throw new InvalidOperationException(
                    $"رصيد القياس غير كافٍ (متاح {stock.Quantity - quantityDelta:N0})");
        }

        await context.SaveChangesAsync();
    }

    public async Task DeductStockAsync(int productId, int productSizeId, int warehouseId, decimal quantity)
    {
        if (quantity <= 0) return;
        await AdjustStockAsync(productId, productSizeId, warehouseId, -quantity);
    }

    public async Task<IReadOnlyList<string>> GetDistinctSizeNamesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductSizes.AsNoTracking()
            .Select(s => s.SizeName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }
}
