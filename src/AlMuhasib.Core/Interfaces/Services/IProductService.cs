using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductService
{
    Task<Product> CreateAsync(Product product);
    Task<Product?> GetByIdAsync(int id);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId = null,
        string? searchTerm = null,
        string? sizeName = null,
        string? colorName = null,
        bool? hasBatches = null);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task ApplyDiscountToProductsAsync(
        IEnumerable<int> productIds,
        DiscountType discountType,
        decimal discountValue,
        DateTime? discountExpiresAt);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Product>> SearchByNameAsync(string name);
}
