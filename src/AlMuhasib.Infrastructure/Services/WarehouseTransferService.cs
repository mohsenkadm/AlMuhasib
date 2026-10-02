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
        context.BypassBranchFilter = true;

        var fromWh = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == transfer.FromWarehouseId)
            ?? throw new InvalidOperationException("مخزن المصدر غير موجود");
        var toWh = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == transfer.ToWarehouseId)
            ?? throw new InvalidOperationException("مخزن الوجهة غير موجود");

        if (fromWh.Id == toWh.Id)
            throw new InvalidOperationException("اختر مخزنين مختلفين");

        var list = items.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("أضف بنود النقل");

        transfer.TransferNumber = $"TR-{DateTime.Now:yyyyMMddHHmmss}";
        transfer.BranchId = fromWh.BranchId;
        transfer.Items = list;
        if (transfer.RowVersion is null || transfer.RowVersion.Length == 0)
            transfer.RowVersion = new byte[] { 1 };
        if (string.IsNullOrEmpty(transfer.CreatedBy))
            transfer.CreatedBy = "system";
        foreach (var item in list)
        {
            item.BranchId = fromWh.BranchId;
            if (item.RowVersion is null || item.RowVersion.Length == 0)
                item.RowVersion = new byte[] { 1 };
            if (string.IsNullOrEmpty(item.CreatedBy))
                item.CreatedBy = "system";
        }

        context.WarehouseTransfers.Add(transfer);

        foreach (var item in list)
        {
            var fromStock = await context.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == transfer.FromWarehouseId && s.ProductId == item.ProductId && !s.IsDeleted);
            if (fromStock is null || fromStock.Quantity < item.Quantity)
                throw new InvalidOperationException("كمية غير كافية في المخزن المصدر");

            fromStock.Quantity -= item.Quantity;

            var toStock = await context.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == transfer.ToWarehouseId && s.ProductId == item.ProductId && !s.IsDeleted);
            if (toStock is null)
            {
                context.WarehouseStocks.Add(new WarehouseStock
                {
                    WarehouseId = transfer.ToWarehouseId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    BranchId = toWh.BranchId,
                    CreatedBy = "system",
                    RowVersion = new byte[] { 1 }
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
        context.BypassBranchFilter = true;
        return await context.WarehouseTransfers.AsNoTracking()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .OrderByDescending(t => t.Date)
            .Take(count)
            .ToListAsync();
    }

    public async Task<WarehouseTransfer?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.BypassBranchFilter = true;
        return await context.WarehouseTransfers.AsNoTracking()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }
}
