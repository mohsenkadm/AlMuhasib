using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductSerialService
{
    Task<IReadOnlyList<ProductSerial>> GetByProductAsync(int productId, bool? sold = null);
    Task<IReadOnlyList<ProductSerial>> GetAvailableAsync(int productId, int? warehouseId = null);

    /// <summary>إضافة سيريال واحد أو أكثر لنفس المنتج (مفصولة بفاصلة عند الإدخال من الواجهة).</summary>
    Task AddRangeAsync(int productId, int? warehouseId, IEnumerable<string> serialNumbers);

    Task DeleteAsync(int serialId);
    Task MarkSoldAsync(string serialNumber, int productId, int? invoiceItemId);
    Task UnmarkSoldAsync(int invoiceItemId);
    Task<bool> ExistsAsync(string serialNumber);

    /// <summary>عدد السيريالات المتاحة (غير المباعة) لكل منتج.</summary>
    Task<IReadOnlyDictionary<int, int>> GetAvailableCountsByProductIdsAsync(IEnumerable<int> productIds);
}
