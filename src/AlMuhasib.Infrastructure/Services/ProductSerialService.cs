using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductSerialService : IProductSerialService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProductSerialService(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

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
        await using var context = await _contextFactory.CreateDbContextAsync();
        var normalized = serialNumbers
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var sn in normalized)
        {
            var exists = await context.ProductSerials.AnyAsync(s => s.SerialNumber == sn);
            if (exists)
                throw new InvalidOperationException($"الرقم التسلسلي موجود مسبقاً: {sn}");

            context.ProductSerials.Add(new ProductSerial
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                SerialNumber = sn,
                IsSold = false
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task MarkSoldAsync(string serialNumber, int productId, int? invoiceItemId)
    {
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
