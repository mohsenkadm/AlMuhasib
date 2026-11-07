using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IBranchContext _branchContext;

    public CategoryService(
        IDbContextFactory<AppDbContext> contextFactory,
        IBranchContext branchContext)
    {
        _contextFactory = contextFactory;
        _branchContext = branchContext;
    }

    public async Task<IReadOnlyList<int>> GetBranchIdsForCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.CategoryBranches.AsNoTracking()
            .Where(cb => cb.CategoryId == categoryId)
            .Select(cb => cb.BranchId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetBranchNamesByCategoryIdsAsync(
        IEnumerable<int> categoryIds,
        CancellationToken ct = default)
    {
        var ids = categoryIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var rows = await context.CategoryBranches.AsNoTracking()
            .Where(cb => ids.Contains(cb.CategoryId))
            .Select(cb => new { cb.CategoryId, cb.Branch.Name, cb.Branch.IsMain })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.CategoryId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(" · ", g.OrderByDescending(x => x.IsMain).ThenBy(x => x.Name).Select(x => x.Name)));
    }

    public async Task SetCategoryBranchesAsync(
        int categoryId,
        IEnumerable<int> branchIds,
        CancellationToken ct = default)
    {
        var ids = branchIds.Distinct().Where(id => id > 0).ToList();
        if (ids.Count == 0)
            throw new InvalidOperationException("يجب اختيار فرع واحد على الأقل للصنف");

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var categoryExists = await context.Categories.AnyAsync(c => c.Id == categoryId, ct);
        if (!categoryExists)
            throw new InvalidOperationException("الصنف غير موجود");

        var validBranchIds = await context.Branches.AsNoTracking()
            .Where(b => ids.Contains(b.Id) && b.IsActive)
            .Select(b => b.Id)
            .ToListAsync(ct);
        if (validBranchIds.Count == 0)
            throw new InvalidOperationException("لا يوجد فرع نشط صالح من الفروع المختارة");

        var existing = await context.CategoryBranches.Where(cb => cb.CategoryId == categoryId).ToListAsync(ct);
        context.CategoryBranches.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var branchId in validBranchIds)
        {
            context.CategoryBranches.Add(new CategoryBranch
            {
                CategoryId = categoryId,
                BranchId = branchId,
                CreatedAt = now
            });
        }

        // Touch category so Sync includes VisibleBranchSyncIds on next push.
        var category = await context.Categories.FirstAsync(c => c.Id == categoryId, ct);
        category.UpdatedAt = now;

        await context.SaveChangesAsync(ct);
    }

    public async Task EnsureDefaultCategoryBranchAsync(int categoryId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        await EnsureDefaultCategoryBranchCoreAsync(context, categoryId, ct);
    }

    private static async Task EnsureDefaultCategoryBranchCoreAsync(
        AppDbContext context, int categoryId, CancellationToken ct = default)
    {
        if (await context.CategoryBranches.AnyAsync(cb => cb.CategoryId == categoryId, ct))
            return;

        var mainId = await context.Branches.AsNoTracking()
            .Where(b => b.IsMain && b.IsActive)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync(ct);
        if (mainId is null)
        {
            mainId = await context.Branches.AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Id)
                .Select(b => (int?)b.Id)
                .FirstOrDefaultAsync(ct);
        }
        if (mainId is null) return;

        context.CategoryBranches.Add(new CategoryBranch
        {
            CategoryId = categoryId,
            BranchId = mainId.Value,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Category>> GetVisibleInBranchAsync(int branchId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await ApplyVisibleInBranchFilter(context.Categories, context, branchId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    /// <summary>أصناف مربوطة بالفرع، أو بلا أي ربط (توافق قديم).</summary>
    private static IQueryable<Category> ApplyVisibleInBranchFilter(
        IQueryable<Category> query,
        AppDbContext context,
        int branchId) =>
        query.Where(c =>
            context.CategoryBranches.Any(cb => cb.CategoryId == c.Id && cb.BranchId == branchId)
            || !context.CategoryBranches.Any(cb => cb.CategoryId == c.Id));

    public async Task<IReadOnlyList<Category>> GetVisibleInCurrentBranchAsync(CancellationToken ct = default)
    {
        if (_branchContext.CurrentBranchId is int bid)
            return await GetVisibleInBranchAsync(bid, ct);

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<bool> IsVisibleInBranchAsync(int categoryId, int branchId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var hasAny = await context.CategoryBranches.AnyAsync(cb => cb.CategoryId == categoryId, ct);
        if (!hasAny) return true;
        return await context.CategoryBranches.AnyAsync(
            cb => cb.CategoryId == categoryId && cb.BranchId == branchId, ct);
    }
}
