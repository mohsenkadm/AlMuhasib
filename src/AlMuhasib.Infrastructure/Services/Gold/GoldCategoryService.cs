using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldCategoryService : IGoldCategoryService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldCategoryService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<GoldCategory>> GetAllAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldCategories.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(c => c.IsActive);
        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task<GoldCategory> CreateAsync(GoldCategory category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            throw new InvalidOperationException("اسم التصنيف مطلوب");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var name = category.Name.Trim();
        if (await context.GoldCategories.AnyAsync(c => c.Name == name, cancellationToken))
            throw new InvalidOperationException("التصنيف موجود مسبقاً");

        category.Name = name;
        await context.GoldCategories.AddAsync(category, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<GoldCategory> UpdateAsync(GoldCategory category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            throw new InvalidOperationException("اسم التصنيف مطلوب");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.GoldCategories.FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken)
            ?? throw new InvalidOperationException("التصنيف غير موجود");

        var name = category.Name.Trim();
        if (await context.GoldCategories.AnyAsync(c => c.Id != category.Id && c.Name == name, cancellationToken))
            throw new InvalidOperationException("التصنيف موجود مسبقاً");

        existing.Name = name;
        existing.IsActive = category.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.GoldCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("التصنيف غير موجود");
        existing.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }
}
