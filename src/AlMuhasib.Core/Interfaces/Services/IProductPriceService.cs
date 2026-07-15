using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductPriceService
{
    Task<ProductPrice> UpsertAsync(ProductPrice price);
    Task UpsertManyAsync(IEnumerable<ProductPrice> prices);
    Task<ProductPrice?> GetByIdAsync(int id);
    Task<(IEnumerable<ProductPrice> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        int? productId = null,
        int? pricingTypeId = null,
        int? categoryId = null,
        decimal? minSalePrice = null,
        decimal? maxSalePrice = null,
        decimal? minPurchasePrice = null,
        decimal? maxPurchasePrice = null);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<ProductPrice>> GetByProductIdAsync(int productId);
    Task<IReadOnlyList<ProductPrice>> GetByProductIdsAsync(IEnumerable<int> productIds);
    Task UpdatePurchasePriceAsync(int productId, int pricingTypeId, decimal purchasePrice);
    Task<bool> ExistsAsync(int productId, int pricingTypeId, int? excludeId = null);
}
