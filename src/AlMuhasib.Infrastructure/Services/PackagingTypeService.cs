using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class PackagingTypeService : IPackagingTypeService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public PackagingTypeService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task EnsureDefaultExistsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (await context.PackagingTypes.IgnoreQueryFilters().AnyAsync(t => !t.IsDeleted && t.IsDefault))
            return;

        var existingDefault = await context.PackagingTypes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => !t.IsDeleted && t.Name == "قطعة");

        if (existingDefault is not null)
        {
            existingDefault.IsDefault = true;
            existingDefault.IsActive = true;
            await context.SaveChangesAsync();
            return;
        }

        context.PackagingTypes.Add(new PackagingType
        {
            Name = "قطعة",
            IsDefault = true,
            IsActive = true,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    public async Task<PackagingType> CreateAsync(PackagingType packagingType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        packagingType.Name = packagingType.Name.Trim();
        packagingType.CreatedBy = _currentUserService.Username;
        packagingType.CreatedAt = DateTime.UtcNow;

        if (packagingType.IsDefault)
        {
            var defaults = await context.PackagingTypes.Where(t => t.IsDefault).ToListAsync();
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        context.PackagingTypes.Add(packagingType);
        await context.SaveChangesAsync();
        return packagingType;
    }

    public async Task<PackagingType?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PackagingTypes.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<(IEnumerable<PackagingType> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null, bool? activeOnly = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.PackagingTypes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(t => t.Name.Contains(term));
        }

        if (activeOnly == true)
            query = query.Where(t => t.IsActive);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task UpdateAsync(PackagingType packagingType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PackagingTypes.FirstOrDefaultAsync(t => t.Id == packagingType.Id)
            ?? throw new InvalidOperationException("نوع التعبئة غير موجود");

        if (packagingType.IsDefault && !existing.IsDefault)
        {
            var defaults = await context.PackagingTypes.Where(t => t.IsDefault && t.Id != existing.Id).ToListAsync();
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        existing.Name = packagingType.Name.Trim();
        existing.IsActive = packagingType.IsActive;
        existing.IsDefault = packagingType.IsDefault;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = _currentUserService.Username;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PackagingTypes.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new InvalidOperationException("نوع التعبئة غير موجود");

        if (existing.IsDefault)
            throw new InvalidOperationException("لا يمكن حذف نوع التعبئة الافتراضي");

        var inUse = await context.ProductUnits.AnyAsync(u => u.PackagingTypeId == id);
        if (inUse)
            throw new InvalidOperationException("لا يمكن حذف نوع تعبئة مستخدم في المنتجات");

        existing.MarkSoftDeleted(_currentUserService.Username);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PackagingType>> GetActiveAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PackagingTypes
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }
}
