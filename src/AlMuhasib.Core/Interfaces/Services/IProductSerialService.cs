using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductSerialService
{
    Task<IReadOnlyList<ProductSerial>> GetByProductAsync(int productId, bool? sold = null);
    Task<IReadOnlyList<ProductSerial>> GetAvailableAsync(int productId, int? warehouseId = null);
    Task AddRangeAsync(int productId, int? warehouseId, IEnumerable<string> serialNumbers);
    Task MarkSoldAsync(string serialNumber, int productId, int? invoiceItemId);
    Task UnmarkSoldAsync(int invoiceItemId);
    Task<bool> ExistsAsync(string serialNumber);
}
