using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductColorService
{
    Task<IReadOnlyList<ProductColor>> GetByProductAsync(int productId);
    Task<bool> HasColorsAsync(int productId);
    Task<ProductColor> SaveAsync(ProductColor color);
    Task DeleteAsync(int colorId);
    Task<IReadOnlyList<string>> GetDistinctColorNamesAsync();
}
