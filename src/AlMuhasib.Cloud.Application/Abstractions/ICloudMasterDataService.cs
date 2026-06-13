using AlMuhasib.Cloud.Application.Models;

namespace AlMuhasib.Cloud.Application.Abstractions;

public interface ICloudMasterDataService
{
    Task<MasterDataBundle> GetAllAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetCategoriesAsync(string? search = null, CancellationToken ct = default);
    Task<List<ProductLookupItem>> GetProductsAsync(string? search = null, Guid? categorySyncId = null, string? barcode = null, CancellationToken ct = default);
    Task<List<LookupItem>> GetCustomersAsync(string? search = null, CancellationToken ct = default);
    Task<List<LookupItem>> GetSuppliersAsync(string? search = null, CancellationToken ct = default);
    Task<List<LookupItem>> GetWarehousesAsync(string? search = null, CancellationToken ct = default);
    Task<List<LookupItem>> GetCashBoxesAsync(string? search = null, CancellationToken ct = default);
    Task<List<LookupItem>> GetBankAccountsAsync(string? search = null, CancellationToken ct = default);
    Task<List<LookupItem>> GetExpenseTypesAsync(string? search = null, CancellationToken ct = default);
    Task<List<LookupItem>> GetInvestorsAsync(string? search = null, CancellationToken ct = default);
    Task<int?> ResolveIdBySyncIdAsync(string entityType, Guid syncId, CancellationToken ct = default);
    Task<Guid?> ResolveSyncIdByIdAsync(string entityType, int id, CancellationToken ct = default);
}
