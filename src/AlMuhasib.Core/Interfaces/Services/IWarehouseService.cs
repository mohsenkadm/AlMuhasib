using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IWarehouseService
{
    Task<Warehouse> CreateAsync(Warehouse warehouse);
    Task<Warehouse?> GetByIdAsync(int id);
    Task<IEnumerable<Warehouse>> GetAllAsync();
    Task UpdateAsync(Warehouse warehouse);
    Task DeleteAsync(int id);
    Task<WarehouseStock?> GetStockAsync(int warehouseId, int productId);
    Task UpdateStockAsync(int warehouseId, int productId, decimal quantityChange);
    Task<IEnumerable<WarehouseStock>> GetStockByWarehouseAsync(int warehouseId);
    Task<IEnumerable<WarehouseStock>> GetStockByProductAsync(int productId);
}
