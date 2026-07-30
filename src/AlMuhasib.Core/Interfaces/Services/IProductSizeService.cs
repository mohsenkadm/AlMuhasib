using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductSizeService
{
    Task<IReadOnlyList<ProductSize>> GetByProductAsync(int productId);
    Task<bool> HasSizesAsync(int productId);
    Task<ProductSize> SaveAsync(ProductSize size);
    Task DeleteAsync(int sizeId);
    Task<IReadOnlyList<ProductSizeStock>> GetStocksAsync(int productId, int? warehouseId = null);
    Task AdjustStockAsync(int productId, int productSizeId, int warehouseId, decimal quantityDelta);
    Task DeductStockAsync(int productId, int productSizeId, int warehouseId, decimal quantity);
}
