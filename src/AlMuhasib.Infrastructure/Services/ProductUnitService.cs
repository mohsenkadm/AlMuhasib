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
            .Include(u => u.PackagingType)
            .Where(u => u.ProductId == productId)
            .OrderByDescending(u => u.IsDefault)
            .ThenBy(u => u.UnitName)
            .ToListAsync();
    }

    public async Task<ProductUnit> SaveAsync(ProductUnit unit)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (unit.ConversionFactor <= 0)
            throw new InvalidOperationException("كمية التعبئة يجب أن تكون أكبر من صفر");

        if (unit.PackagingTypeId is int packagingTypeId)
        {
            var packaging = await context.PackagingTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == packagingTypeId && t.IsActive)
                ?? throw new InvalidOperationException("نوع التعبئة غير موجود أو غير نشط");
            unit.UnitName = packaging.Name;
        }

        if (string.IsNullOrWhiteSpace(unit.UnitName))
            throw new InvalidOperationException("اسم نوع التعبئة مطلوب");

        unit.UnitName = unit.UnitName.Trim();

        if (unit.Id == 0)
        {
            if (unit.PackagingTypeId is int newPackagingTypeId)
            {
                var duplicate = await context.ProductUnits.AnyAsync(u =>
                    u.ProductId == unit.ProductId && u.PackagingTypeId == newPackagingTypeId);
                if (duplicate)
                    throw new InvalidOperationException("نوع التعبئة مضاف مسبقاً لهذا المنتج");
            }

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
                ?? throw new InvalidOperationException("التعبئة غير موجودة");

            if (unit.PackagingTypeId is int updatedPackagingTypeId)
            {
                var duplicate = await context.ProductUnits.AnyAsync(u =>
                    u.ProductId == existing.ProductId
                    && u.PackagingTypeId == updatedPackagingTypeId
                    && u.Id != existing.Id);
                if (duplicate)
                    throw new InvalidOperationException("نوع التعبئة مضاف مسبقاً لهذا المنتج");
            }

            existing.UnitName = unit.UnitName;
            existing.ConversionFactor = unit.ConversionFactor;
            existing.PackagingTypeId = unit.PackagingTypeId;
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
