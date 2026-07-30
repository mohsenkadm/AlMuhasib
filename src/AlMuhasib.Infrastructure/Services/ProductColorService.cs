using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductColorService : IProductColorService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProductColorService(IDbContextFactory<AppDbContext> contextFactory) =>
        _contextFactory = contextFactory;

    public async Task<IReadOnlyList<ProductColor>> GetByProductAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductColors.AsNoTracking()
            .Where(c => c.ProductId == productId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.ColorName)
            .ToListAsync();
    }

    public async Task<bool> HasColorsAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductColors.AsNoTracking().AnyAsync(c => c.ProductId == productId);
    }

    public async Task<ProductColor> SaveAsync(ProductColor color)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var name = (color.ColorName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("اسم اللون مطلوب");

        if (color.Id == 0)
        {
            var duplicate = await context.ProductColors.AnyAsync(c =>
                c.ProductId == color.ProductId && c.ColorName == name);
            if (duplicate)
                throw new InvalidOperationException($"اللون «{name}» موجود مسبقاً لهذا المنتج");

            if (color.SortOrder <= 0)
            {
                var maxOrder = await context.ProductColors
                    .Where(c => c.ProductId == color.ProductId)
                    .Select(c => (int?)c.SortOrder)
                    .MaxAsync() ?? 0;
                color.SortOrder = maxOrder + 1;
            }

            color.ColorName = name;
            context.ProductColors.Add(color);
        }
        else
        {
            var existing = await context.ProductColors.FirstOrDefaultAsync(c => c.Id == color.Id)
                ?? throw new InvalidOperationException("اللون غير موجود");

            var duplicate = await context.ProductColors.AnyAsync(c =>
                c.ProductId == existing.ProductId && c.ColorName == name && c.Id != existing.Id);
            if (duplicate)
                throw new InvalidOperationException($"اللون «{name}» موجود مسبقاً لهذا المنتج");

            existing.ColorName = name;
            existing.SortOrder = color.SortOrder;
            color = existing;
        }

        await context.SaveChangesAsync();
        return color;
    }

    public async Task DeleteAsync(int colorId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var color = await context.ProductColors.FirstOrDefaultAsync(c => c.Id == colorId);
        if (color is null) return;
        context.ProductColors.Remove(color);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<string>> GetDistinctColorNamesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductColors.AsNoTracking()
            .Select(c => c.ColorName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }
}
