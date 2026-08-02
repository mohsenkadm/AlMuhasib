using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldWarehouseService
{
    Task<(IReadOnlyList<GoldWarehouseListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldWarehouse>> GetAllAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<GoldWarehouse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoldWarehouse> EnsureDefaultAsync(CancellationToken cancellationToken = default);
    Task<GoldWarehouse> CreateAsync(GoldWarehouse warehouse, CancellationToken cancellationToken = default);
    Task<GoldWarehouse> UpdateAsync(GoldWarehouse warehouse, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<GoldWarehouseTransfer> TransferAsync(
        GoldTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GoldWarehouseTransfer> Items, int TotalCount)> GetTransfersPagedAsync(
        int page,
        int pageSize,
        int? warehouseId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);
}
