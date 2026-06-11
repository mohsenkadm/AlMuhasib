using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IWarehouseTransferService
{
    Task<WarehouseTransfer> CreateTransferAsync(WarehouseTransfer transfer, IEnumerable<WarehouseTransferItem> items);
    Task<IReadOnlyList<WarehouseTransfer>> GetRecentAsync(int count = 50);
}
