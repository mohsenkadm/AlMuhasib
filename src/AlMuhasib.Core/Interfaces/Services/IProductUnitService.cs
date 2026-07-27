using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductUnitService
{
    Task<IReadOnlyList<ProductUnit>> GetByProductAsync(int productId);
    Task<ProductUnit> SaveAsync(ProductUnit unit);
    Task DeleteAsync(int unitId);
    Task SetDefaultAsync(int productId, int unitId);
}
