using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductUnitService : IProductUnitService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProductUnitService(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<ProductUnit>> GetByProductAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductUnits.AsNoTracking()
            .Where(u => u.ProductId == productId)
            .OrderByDescending(u => u.IsDefault)
            .ThenBy(u => u.UnitName)
            .ToListAsync();
    }

    public async Task<ProductUnit> SaveAsync(ProductUnit unit)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (unit.ConversionFactor <= 0)
            throw new InvalidOperationException("معامل التحويل يجب أن يكون أكبر من صفر");

        if (unit.Id == 0)
        {
            var hasAny = await context.ProductUnits.AnyAsync(u => u.ProductId == unit.ProductId);
            if (!hasAny)
                unit.IsDefault = true;

            if (unit.IsDefault)
                await ClearDefaultAsync(context, unit.ProductId);

            context.ProductUnits.Add(unit);
        }
        else
        {
            var existing = await context.ProductUnits.FirstOrDefaultAsync(u => u.Id == unit.Id)
                ?? throw new InvalidOperationException("الوحدة غير موجودة");

            existing.UnitName = unit.UnitName;
            existing.ConversionFactor = unit.ConversionFactor;
            if (unit.IsDefault && !existing.IsDefault)
            {
                await ClearDefaultAsync(context, existing.ProductId);
                existing.IsDefault = true;
            }
            else
            {
                existing.IsDefault = unit.IsDefault;
            }
            unit = existing;
        }

        await context.SaveChangesAsync();
        return unit;
    }

    public async Task DeleteAsync(int unitId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var unit = await context.ProductUnits.FirstOrDefaultAsync(u => u.Id == unitId);
        if (unit is null) return;

        var productId = unit.ProductId;
        var wasDefault = unit.IsDefault;
        context.ProductUnits.Remove(unit);
        await context.SaveChangesAsync();

        if (wasDefault)
        {
            var next = await context.ProductUnits.FirstOrDefaultAsync(u => u.ProductId == productId);
            if (next is not null)
            {
                next.IsDefault = true;
                await context.SaveChangesAsync();
            }
        }
    }

    public async Task SetDefaultAsync(int productId, int unitId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await ClearDefaultAsync(context, productId);
        var unit = await context.ProductUnits.FirstOrDefaultAsync(u => u.Id == unitId && u.ProductId == productId)
            ?? throw new InvalidOperationException("الوحدة غير موجودة");
        unit.IsDefault = true;
        await context.SaveChangesAsync();
    }

    private static async Task ClearDefaultAsync(AppDbContext context, int productId)
    {
        var defaults = await context.ProductUnits.Where(u => u.ProductId == productId && u.IsDefault).ToListAsync();
        foreach (var d in defaults)
            d.IsDefault = false;
    }
}
