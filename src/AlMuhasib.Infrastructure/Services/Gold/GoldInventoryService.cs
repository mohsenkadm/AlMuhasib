using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldInventoryService : IGoldInventoryService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldInventoryService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<GoldItem> Items, int TotalCount)> GetItemsPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        int? karatValue = null,
        GoldItemStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.Name.Contains(term) ||
                i.Barcode.Contains(term) ||
                i.Category.Contains(term) ||
                i.Notes.Contains(term));
        }

        if (karatValue.HasValue)
            query = query.Where(i => i.KaratValue == karatValue.Value);
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<GoldItem?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<GoldItem?> GetItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var term = barcode.Trim();
        return await context.GoldItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Barcode == term, cancellationToken);
    }

    public async Task<GoldItem> CreateItemAsync(GoldItem item, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new InvalidOperationException("اسم القطعة مطلوب");
        if (item.WeightGrams <= 0)
            throw new InvalidOperationException("وزن القطعة يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(item.Barcode))
        {
            var exists = await context.GoldItems.AnyAsync(i => i.Barcode == item.Barcode, cancellationToken);
            if (exists)
                throw new InvalidOperationException("الباركود مستخدم مسبقاً");
        }

        var warehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(context, null, cancellationToken);

        item.Status = GoldItemStatus.InStock;
        await context.GoldItems.AddAsync(item, cancellationToken);

        await AdjustStockInternalAsync(
            context,
            item.KaratValue,
            item.WeightGrams,
            item.CostPerGram > 0 ? item.CostPerGram : null,
            warehouseId,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<GoldItem> UpdateItemAsync(GoldItem item, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.GoldItems.FirstOrDefaultAsync(i => i.Id == item.Id, cancellationToken)
            ?? throw new InvalidOperationException("القطعة غير موجودة");

        if (!string.IsNullOrWhiteSpace(item.Barcode) &&
            !string.Equals(existing.Barcode, item.Barcode, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await context.GoldItems.AnyAsync(
                i => i.Id != item.Id && i.Barcode == item.Barcode,
                cancellationToken);
            if (exists)
                throw new InvalidOperationException("الباركود مستخدم مسبقاً");
        }

        var warehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(context, null, cancellationToken);
        var weightDelta = item.WeightGrams - existing.WeightGrams;
        var karatChanged = existing.KaratValue != item.KaratValue;

        if (existing.Status == GoldItemStatus.InStock)
        {
            if (karatChanged)
            {
                await AdjustStockInternalAsync(context, existing.KaratValue, -existing.WeightGrams, null, warehouseId, cancellationToken);
                await AdjustStockInternalAsync(
                    context,
                    item.KaratValue,
                    item.WeightGrams,
                    item.CostPerGram > 0 ? item.CostPerGram : null,
                    warehouseId,
                    cancellationToken);
            }
            else if (weightDelta != 0)
            {
                await AdjustStockInternalAsync(
                    context,
                    existing.KaratValue,
                    weightDelta,
                    weightDelta > 0 && item.CostPerGram > 0 ? item.CostPerGram : null,
                    warehouseId,
                    cancellationToken);
            }
        }

        existing.Name = item.Name;
        existing.Barcode = item.Barcode ?? string.Empty;
        existing.Category = item.Category ?? string.Empty;
        existing.Notes = item.Notes ?? string.Empty;
        existing.KaratValue = item.KaratValue;
        existing.WeightGrams = item.WeightGrams;
        existing.SuggestedMakingCharge = item.SuggestedMakingCharge;
        existing.MakingChargeCurrency = item.MakingChargeCurrency;
        existing.CostPerGram = item.CostPerGram;
        existing.Status = item.Status;
        existing.TrackAsPiece = item.TrackAsPiece;

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteItemAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await context.GoldItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("القطعة غير موجودة");

        if (item.Status == GoldItemStatus.InStock)
        {
            var warehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(context, null, cancellationToken);
            await AdjustStockInternalAsync(context, item.KaratValue, -item.WeightGrams, null, warehouseId, cancellationToken);
        }

        item.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoldStockRow>> GetStockBalancesAsync(
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await BuildStockRowsAsync(context, warehouseId, cancellationToken);
    }

    public async Task<GoldStockBalance?> GetStockBalanceByKaratAsync(
        int karatValue,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var resolvedWarehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(context, warehouseId, cancellationToken);
        return await context.GoldStockBalances.AsNoTracking()
            .FirstOrDefaultAsync(s => s.KaratValue == karatValue && s.WarehouseId == resolvedWarehouseId, cancellationToken);
    }

    public async Task AdjustStockAsync(
        int karatValue,
        decimal gramsDelta,
        decimal? costPerGram = null,
        string? notes = null,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        if (gramsDelta == 0)
            return;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var resolvedWarehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(context, warehouseId, cancellationToken);
        await AdjustStockInternalAsync(context, karatValue, gramsDelta, costPerGram, resolvedWarehouseId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    internal static async Task<GoldStockBalance> AdjustStockInternalAsync(
        GoldDbContext context,
        int karatValue,
        decimal gramsDelta,
        decimal? costPerGram,
        int warehouseId,
        CancellationToken cancellationToken)
    {
        var balance = await context.GoldStockBalances
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                s => s.KaratValue == karatValue && s.WarehouseId == warehouseId,
                cancellationToken);

        if (balance is { IsDeleted: true })
            balance.RestoreFromSoftDelete("System");

        if (balance is null)
        {
            balance = new GoldStockBalance
            {
                WarehouseId = warehouseId,
                KaratValue = karatValue,
                GramsOnHand = 0,
                AverageCostPerGram = costPerGram ?? 0
            };
            await context.GoldStockBalances.AddAsync(balance, cancellationToken);
        }

        if (gramsDelta < 0 && balance.GramsOnHand + gramsDelta < -0.0001m)
            throw new InvalidOperationException($"المخزون غير كافٍ للعيار {karatValue}");

        GoldCurrencyHelper.ApplyStockDelta(balance, gramsDelta, costPerGram);
        return balance;
    }

    internal static async Task<List<GoldStockRow>> BuildStockRowsAsync(
        GoldDbContext context,
        int? warehouseId,
        CancellationToken cancellationToken)
    {
        var settings = await context.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        var lowThreshold = settings?.LowStockAlertGrams ?? 10m;

        var balanceQuery = context.GoldStockBalances.AsNoTracking().AsQueryable();
        if (warehouseId.HasValue)
            balanceQuery = balanceQuery.Where(s => s.WarehouseId == warehouseId.Value);

        var balances = await balanceQuery.ToListAsync(cancellationToken);
        var karats = await context.GoldKarats.AsNoTracking()
            .ToDictionaryAsync(k => k.KaratValue, k => k.Name, cancellationToken);
        var warehouses = await context.GoldWarehouses.AsNoTracking()
            .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);

        var pieceCounts = await context.GoldItems.AsNoTracking()
            .Where(i => i.Status == GoldItemStatus.InStock && i.TrackAsPiece)
            .GroupBy(i => i.KaratValue)
            .Select(g => new { KaratValue = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.KaratValue, x => x.Count, cancellationToken);

        return balances
            .OrderBy(b => b.WarehouseId)
            .ThenBy(b => b.KaratValue)
            .Select(b =>
            {
                var value = GoldCurrencyHelper.Round(b.GramsOnHand * b.AverageCostPerGram);
                return new GoldStockRow
                {
                    WarehouseId = b.WarehouseId,
                    WarehouseName = warehouses.TryGetValue(b.WarehouseId, out var whName) ? whName : $"مخزن #{b.WarehouseId}",
                    KaratValue = b.KaratValue,
                    KaratName = karats.TryGetValue(b.KaratValue, out var name) ? name : $"عيار {b.KaratValue}",
                    GramsOnHand = b.GramsOnHand,
                    AverageCostPerGram = b.AverageCostPerGram,
                    StockValue = value,
                    PieceCount = pieceCounts.GetValueOrDefault(b.KaratValue),
                    IsLowStock = b.GramsOnHand < lowThreshold
                };
            })
            .ToList();
    }
}
