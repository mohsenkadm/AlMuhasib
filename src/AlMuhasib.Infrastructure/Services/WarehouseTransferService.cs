using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class WarehouseTransferService : IWarehouseTransferService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public WarehouseTransferService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<WarehouseTransfer> CreateTransferAsync(WarehouseTransfer transfer, IEnumerable<WarehouseTransferItem> items)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var list = items.ToList();
        transfer.TransferNumber = $"TR-{DateTime.Now:yyyyMMddHHmmss}";
        transfer.Items = list;
        context.WarehouseTransfers.Add(transfer);

        foreach (var item in list)
        {
            var fromStock = await context.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == transfer.FromWarehouseId && s.ProductId == item.ProductId);
            if (fromStock is null || fromStock.Quantity < item.Quantity)
                throw new InvalidOperationException("كمية غير كافية في المخزن المصدر");

            fromStock.Quantity -= item.Quantity;
            var toStock = await context.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == transfer.ToWarehouseId && s.ProductId == item.ProductId);
            if (toStock is null)
            {
                context.WarehouseStocks.Add(new WarehouseStock
                {
                    WarehouseId = transfer.ToWarehouseId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }
            else
            {
                toStock.Quantity += item.Quantity;
            }
        }

        await context.SaveChangesAsync();
        return transfer;
    }

    public async Task<IReadOnlyList<WarehouseTransfer>> GetRecentAsync(int count = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WarehouseTransfers.AsNoTracking()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .OrderByDescending(t => t.Date)
            .Take(count)
            .ToListAsync();
    }
}
