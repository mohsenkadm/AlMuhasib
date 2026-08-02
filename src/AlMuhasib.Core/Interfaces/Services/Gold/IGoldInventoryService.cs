using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldInventoryService
{
    Task<(IReadOnlyList<GoldItem> Items, int TotalCount)> GetItemsPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        int? karatValue = null,
        GoldItemStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<GoldItem?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoldItem?> GetItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<GoldItem> CreateItemAsync(GoldItem item, CancellationToken cancellationToken = default);
    Task<GoldItem> UpdateItemAsync(GoldItem item, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldStockRow>> GetStockBalancesAsync(
        int? warehouseId = null,
        CancellationToken cancellationToken = default);

    Task<GoldStockBalance?> GetStockBalanceByKaratAsync(
        int karatValue,
        int? warehouseId = null,
        CancellationToken cancellationToken = default);

    Task AdjustStockAsync(
        int karatValue,
        decimal gramsDelta,
        decimal? costPerGram = null,
        string? notes = null,
        int? warehouseId = null,
        CancellationToken cancellationToken = default);
}
