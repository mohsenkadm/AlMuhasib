using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductBatchService : IProductBatchService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProductBatchService(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<ProductBatch>> GetByProductAsync(int productId, int? warehouseId = null, bool inStockOnly = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var q = context.ProductBatches.AsNoTracking().Where(b => b.ProductId == productId);
        if (warehouseId.HasValue)
            q = q.Where(b => b.WarehouseId == warehouseId.Value);
        if (inStockOnly)
            q = q.Where(b => b.Quantity > 0);
        return await q
            .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
            .ThenBy(b => b.BatchNumber)
            .ToListAsync();
    }

    public async Task<ProductBatch> UpsertAsync(int productId, int warehouseId, string? batchNumber, DateTime? expiryDate, decimal quantityDelta)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var normalized = string.IsNullOrWhiteSpace(batchNumber) ? null : batchNumber.Trim();

        var batch = await context.ProductBatches.FirstOrDefaultAsync(b =>
            b.ProductId == productId
            && b.WarehouseId == warehouseId
            && b.BatchNumber == normalized
            && b.ExpiryDate == expiryDate);

        if (batch is null)
        {
            batch = new ProductBatch
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                BatchNumber = normalized,
                ExpiryDate = expiryDate,
                Quantity = quantityDelta
            };
            context.ProductBatches.Add(batch);
        }
        else
        {
            batch.Quantity += quantityDelta;
            if (batch.Quantity < 0)
                throw new InvalidOperationException($"كمية الدفعة غير كافية: {normalized ?? "بدون رقم"}");
        }

        await context.SaveChangesAsync();
        return batch;
    }

    public async Task DeductAsync(int batchId, decimal quantity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var batch = await context.ProductBatches.FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new InvalidOperationException("الدفعة غير موجودة");
        if (batch.Quantity < quantity)
            throw new InvalidOperationException($"كمية الدفعة غير كافية (متاح {batch.Quantity:N0})");
        batch.Quantity -= quantity;
        await context.SaveChangesAsync();
    }

    public async Task<ProductBatch?> FindFifoAsync(int productId, int warehouseId, decimal requiredQty)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductBatches.AsNoTracking()
            .Where(b => b.ProductId == productId && b.WarehouseId == warehouseId && b.Quantity >= requiredQty)
            .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
            .ThenBy(b => b.Id)
            .FirstOrDefaultAsync();
    }
}
