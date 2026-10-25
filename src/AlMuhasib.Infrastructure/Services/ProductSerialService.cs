using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductSerialService : IProductSerialService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IBranchContext _branchContext;

    public ProductSerialService(
        IDbContextFactory<AppDbContext> contextFactory,
        IBranchContext branchContext)
    {
        _contextFactory = contextFactory;
        _branchContext = branchContext;
    }

    public async Task<IReadOnlyList<ProductSerial>> GetByProductAsync(int productId, bool? sold = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var q = context.ProductSerials.AsNoTracking().Where(s => s.ProductId == productId);
        if (sold.HasValue)
            q = q.Where(s => s.IsSold == sold.Value);
        return await q.OrderBy(s => s.SerialNumber).ToListAsync();
    }

    public async Task<IReadOnlyList<ProductSerial>> GetAvailableAsync(int productId, int? warehouseId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var q = context.ProductSerials.AsNoTracking()
            .Where(s => s.ProductId == productId && !s.IsSold);
        if (warehouseId.HasValue)
            q = q.Where(s => s.WarehouseId == null || s.WarehouseId == warehouseId);
        return await q.OrderBy(s => s.SerialNumber).ToListAsync();
    }

    public async Task AddRangeAsync(int productId, int? warehouseId, IEnumerable<string> serialNumbers)
    {
        var writeBranchId = _branchContext.RequireWriteBranchId();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var normalized = serialNumbers
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
            return;

        foreach (var sn in normalized)
        {
            var exists = await context.ProductSerials
                .IgnoreQueryFilters()
                .AnyAsync(s => s.SerialNumber == sn
                               && s.BranchId == writeBranchId
                               && !s.IsDeleted);
            if (exists)
                throw new InvalidOperationException($"الرقم التسلسلي موجود مسبقاً: {sn}");

            context.ProductSerials.Add(new ProductSerial
            {
                BranchId = writeBranchId,
                ProductId = productId,
                WarehouseId = warehouseId,
                SerialNumber = sn,
                IsSold = false
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int serialId)
    {
        _ = _branchContext.RequireWriteBranchId();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var serial = await context.ProductSerials.FirstOrDefaultAsync(s => s.Id == serialId)
            ?? throw new InvalidOperationException("السيريال غير موجود");
        if (serial.IsSold)
            throw new InvalidOperationException("لا يمكن حذف سيريال مباع");
        context.ProductSerials.Remove(serial);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyDictionary<int, int>> GetAvailableCountsByProductIdsAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, int>();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var rows = await context.ProductSerials.AsNoTracking()
            .Where(s => ids.Contains(s.ProductId) && !s.IsSold)
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToListAsync();
        return rows.ToDictionary(x => x.ProductId, x => x.Count);
    }

    public async Task MarkSoldAsync(string serialNumber, int productId, int? invoiceItemId)
    {
        _ = _branchContext.RequireWriteBranchId();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var serial = await context.ProductSerials.FirstOrDefaultAsync(s =>
            s.SerialNumber == serialNumber.Trim() && s.ProductId == productId)
            ?? throw new InvalidOperationException($"الرقم التسلسلي غير موجود: {serialNumber}");

        if (serial.IsSold)
            throw new InvalidOperationException($"الرقم التسلسلي مباع مسبقاً: {serialNumber}");

        serial.IsSold = true;
        serial.InvoiceItemId = invoiceItemId;
        await context.SaveChangesAsync();
    }

    public async Task UnmarkSoldAsync(int invoiceItemId)
    {
        _ = _branchContext.RequireWriteBranchId();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var serials = await context.ProductSerials.Where(s => s.InvoiceItemId == invoiceItemId).ToListAsync();
        foreach (var s in serials)
        {
            s.IsSold = false;
            s.InvoiceItemId = null;
        }
        if (serials.Count > 0)
            await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string serialNumber)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductSerials.AnyAsync(s => s.SerialNumber == serialNumber.Trim());
    }
}
