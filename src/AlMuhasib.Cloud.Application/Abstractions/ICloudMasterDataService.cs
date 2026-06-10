using AlMuhasib.Cloud.Application.Models;

namespace AlMuhasib.Cloud.Application.Abstractions;

public interface ICloudMasterDataService
{
    Task<MasterDataBundle> GetAllAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetCategoriesAsync(CancellationToken ct = default);
    Task<List<ProductLookupItem>> GetProductsAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetCustomersAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetSuppliersAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetWarehousesAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetCashBoxesAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetBankAccountsAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetExpenseTypesAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetInvestorsAsync(CancellationToken ct = default);
    Task<int?> ResolveIdBySyncIdAsync(string entityType, Guid syncId, CancellationToken ct = default);
    Task<Guid?> ResolveSyncIdByIdAsync(string entityType, int id, CancellationToken ct = default);
}
