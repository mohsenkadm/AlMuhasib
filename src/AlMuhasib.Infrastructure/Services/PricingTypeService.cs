using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class PricingTypeService : IPricingTypeService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public PricingTypeService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task EnsureDefaultExistsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (await context.PricingTypes.IgnoreQueryFilters().AnyAsync(t => !t.IsDeleted && t.IsDefault))
            return;

        var existingDefault = await context.PricingTypes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => !t.IsDeleted && t.Name == "سعر مفرد");

        if (existingDefault is not null)
        {
            existingDefault.IsDefault = true;
            existingDefault.IsActive = true;
            await context.SaveChangesAsync();
            return;
        }

        context.PricingTypes.Add(new PricingType
        {
            Name = "سعر مفرد",
            IsDefault = true,
            IsActive = true,
            SyncId = AlMuhasib.Sync.ProductPricingSyncIds.DefaultPricingType,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    public async Task<PricingType> CreateAsync(PricingType pricingType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        pricingType.Name = pricingType.Name.Trim();
        pricingType.CreatedBy = _currentUserService.Username;
        pricingType.CreatedAt = DateTime.UtcNow;

        if (pricingType.IsDefault)
        {
            var defaults = await context.PricingTypes.Where(t => t.IsDefault).ToListAsync();
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        context.PricingTypes.Add(pricingType);
        await context.SaveChangesAsync();
        return pricingType;
    }

    public async Task<PricingType?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PricingTypes.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<(IEnumerable<PricingType> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null, bool? activeOnly = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.PricingTypes.AsQueryable();

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

    public async Task UpdateAsync(PricingType pricingType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PricingTypes.FirstOrDefaultAsync(t => t.Id == pricingType.Id)
            ?? throw new InvalidOperationException("نوع التسعير غير موجود");

        if (pricingType.IsDefault && !existing.IsDefault)
        {
            var defaults = await context.PricingTypes.Where(t => t.IsDefault && t.Id != existing.Id).ToListAsync();
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        existing.Name = pricingType.Name.Trim();
        existing.IsActive = pricingType.IsActive;
        existing.IsDefault = pricingType.IsDefault;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = _currentUserService.Username;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PricingTypes.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new InvalidOperationException("نوع التسعير غير موجود");

        if (existing.IsDefault)
            throw new InvalidOperationException("لا يمكن حذف نوع التسعير الافتراضي");

        var inUse = await context.ProductPrices.AnyAsync(p => p.PricingTypeId == id);
        if (inUse)
            throw new InvalidOperationException("لا يمكن حذف نوع تسعير مستخدم في أسعار المنتجات");

        existing.MarkSoftDeleted(_currentUserService.Username);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PricingType>> GetActiveAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PricingTypes
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }
}
