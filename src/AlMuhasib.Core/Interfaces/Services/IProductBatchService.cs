using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductBatchService
{
    Task<IReadOnlyList<ProductBatch>> GetByProductAsync(int productId, int? warehouseId = null, bool inStockOnly = false);
    Task<ProductBatch> UpsertAsync(int productId, int warehouseId, string? batchNumber, DateTime? expiryDate, decimal quantityDelta);
    Task DeductAsync(int batchId, decimal quantity);
    Task<ProductBatch?> FindFifoAsync(int productId, int warehouseId, decimal requiredQty);
}
