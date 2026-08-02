using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldWarehouseService : IGoldWarehouseService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldWarehouseService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<GoldWarehouseListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureDefaultInternalAsync(context, cancellationToken);

        var query = context.GoldWarehouses.AsNoTracking().AsQueryable();
        if (activeOnly == true)
            query = query.Where(w => w.IsActive);
        else if (activeOnly == false)
            query = query.Where(w => !w.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(w => w.Name.Contains(term) || w.Notes.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var warehouses = await query
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var warehouseIds = warehouses.Select(w => w.Id).ToList();
        var stockStats = await context.GoldStockBalances.AsNoTracking()
            .Where(s => warehouseIds.Contains(s.WarehouseId))
            .GroupBy(s => s.WarehouseId)
            .Select(g => new { WarehouseId = g.Key, TotalGrams = g.Sum(x => x.GramsOnHand), Count = g.Count() })
            .ToDictionaryAsync(x => x.WarehouseId, cancellationToken);

        var items = warehouses.Select(w =>
        {
            stockStats.TryGetValue(w.Id, out var stats);
            return new GoldWarehouseListItem
            {
                Id = w.Id,
                Name = w.Name,
                IsDefault = w.IsDefault,
                IsActive = w.IsActive,
                Notes = w.Notes,
                TotalGrams = stats?.TotalGrams ?? 0,
                BalanceRowCount = stats?.Count ?? 0
            };
        }).ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<GoldWarehouse>> GetAllAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureDefaultInternalAsync(context, cancellationToken);

        var query = context.GoldWarehouses.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(w => w.IsActive);

        return await query
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<GoldWarehouse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldWarehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<GoldWarehouse> EnsureDefaultAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await EnsureDefaultInternalAsync(context, cancellationToken);
    }

    public async Task<GoldWarehouse> CreateAsync(GoldWarehouse warehouse, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(warehouse.Name))
            throw new InvalidOperationException("اسم المخزن مطلوب");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (warehouse.IsDefault)
        {
            var others = await context.GoldWarehouses.Where(w => w.IsDefault).ToListAsync(cancellationToken);
            foreach (var other in others)
                other.IsDefault = false;
        }

        warehouse.IsActive = true;
        await context.GoldWarehouses.AddAsync(warehouse, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return warehouse;
    }

    public async Task<GoldWarehouse> UpdateAsync(GoldWarehouse warehouse, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.GoldWarehouses.FirstOrDefaultAsync(w => w.Id == warehouse.Id, cancellationToken)
            ?? throw new InvalidOperationException("المخزن غير موجود");

        if (warehouse.IsDefault)
        {
            var others = await context.GoldWarehouses
                .Where(w => w.Id != warehouse.Id && w.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
                other.IsDefault = false;
        }
        else if (existing.IsDefault && !warehouse.IsDefault)
        {
            throw new InvalidOperationException("لا يمكن إلغاء تعيين المخزن الافتراضي دون تحديد بديل");
        }

        existing.Name = warehouse.Name;
        existing.IsDefault = warehouse.IsDefault;
        existing.IsActive = warehouse.IsActive;
        existing.Notes = warehouse.Notes ?? string.Empty;

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var warehouse = await context.GoldWarehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("المخزن غير موجود");

        if (warehouse.IsDefault)
            throw new InvalidOperationException("لا يمكن حذف المخزن الافتراضي");

        var hasStock = await context.GoldStockBalances.AnyAsync(
            s => s.WarehouseId == id && s.GramsOnHand > 0,
            cancellationToken);
        if (hasStock)
            throw new InvalidOperationException("لا يمكن حذف مخزن يحتوي على رصيد");

        warehouse.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<GoldWarehouseTransfer> TransferAsync(
        GoldTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FromWarehouseId == request.ToWarehouseId)
            throw new InvalidOperationException("يجب أن يختلف مخزن المصدر عن مخزن الوجهة");
        if (request.WeightGrams <= 0)
            throw new InvalidOperationException("الوزن يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var from = await context.GoldWarehouses.FirstOrDefaultAsync(w => w.Id == request.FromWarehouseId, cancellationToken)
                ?? throw new InvalidOperationException("مخزن المصدر غير موجود");
            var to = await context.GoldWarehouses.FirstOrDefaultAsync(w => w.Id == request.ToWarehouseId, cancellationToken)
                ?? throw new InvalidOperationException("مخزن الوجهة غير موجود");

            if (!from.IsActive || !to.IsActive)
                throw new InvalidOperationException("لا يمكن التحويل من/إلى مخزن غير نشط");

            var fromBalance = await context.GoldStockBalances
                .FirstOrDefaultAsync(s => s.WarehouseId == request.FromWarehouseId && s.KaratValue == request.KaratValue, cancellationToken);
            var cost = fromBalance?.AverageCostPerGram;

            await GoldInventoryService.AdjustStockInternalAsync(
                context,
                request.KaratValue,
                -request.WeightGrams,
                null,
                request.FromWarehouseId,
                cancellationToken);

            await GoldInventoryService.AdjustStockInternalAsync(
                context,
                request.KaratValue,
                request.WeightGrams,
                cost,
                request.ToWarehouseId,
                cancellationToken);

            var transfer = new GoldWarehouseTransfer
            {
                TransferDate = request.TransferDate.Date,
                FromWarehouseId = request.FromWarehouseId,
                ToWarehouseId = request.ToWarehouseId,
                KaratValue = request.KaratValue,
                WeightGrams = request.WeightGrams,
                Notes = request.Notes ?? string.Empty
            };

            await context.GoldWarehouseTransfers.AddAsync(transfer, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return await context.GoldWarehouseTransfers.AsNoTracking()
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .FirstAsync(t => t.Id == transfer.Id, cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(IReadOnlyList<GoldWarehouseTransfer> Items, int TotalCount)> GetTransfersPagedAsync(
        int page,
        int pageSize,
        int? warehouseId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldWarehouseTransfers.AsNoTracking()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(t => t.FromWarehouseId == warehouseId.Value || t.ToWarehouseId == warehouseId.Value);
        if (dateFrom.HasValue)
            query = query.Where(t => t.TransferDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(t => t.TransferDate.Date <= dateTo.Value.Date);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.TransferDate)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    internal static async Task<GoldWarehouse> EnsureDefaultInternalAsync(
        GoldDbContext context,
        CancellationToken cancellationToken)
    {
        var existing = await context.GoldWarehouses
            .FirstOrDefaultAsync(w => w.IsDefault, cancellationToken);

        if (existing is not null)
            return existing;

        existing = await context.GoldWarehouses.FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            existing.IsDefault = true;
            existing.IsActive = true;
            await context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        existing = new GoldWarehouse
        {
            Name = "المخزن الرئيسي",
            IsDefault = true,
            IsActive = true,
            Notes = "مستودع افتراضي"
        };
        await context.GoldWarehouses.AddAsync(existing, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    internal static async Task<int> ResolveWarehouseIdAsync(
        GoldDbContext context,
        int? warehouseId,
        CancellationToken cancellationToken)
    {
        if (warehouseId.HasValue && warehouseId.Value > 0)
        {
            var exists = await context.GoldWarehouses.AnyAsync(w => w.Id == warehouseId.Value, cancellationToken);
            if (!exists)
                throw new InvalidOperationException("المخزن غير موجود");
            return warehouseId.Value;
        }

        var def = await EnsureDefaultInternalAsync(context, cancellationToken);
        return def.Id;
    }
}
