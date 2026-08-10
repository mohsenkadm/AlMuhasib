using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class BusinessSettingsService : IBusinessSettingsService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPricingTypeService _pricingTypeService;

    public BusinessSettingsService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IPricingTypeService pricingTypeService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _pricingTypeService = pricingTypeService;
    }

    public async Task<BusinessSettings> GetOrCreateAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.BusinessSettings
            .IgnoreQueryFilters()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(s => !s.IsDeleted);

        if (existing is null)
        {
            context.BusinessSettings.Add(new BusinessSettings
            {
                ProductPricingEnabled = false,
                UpdateProductPriceOnPurchase = false,
                PeriodLockEnabled = false,
                LockedThroughDate = null,
                SyncId = AlMuhasib.Sync.ProductPricingSyncIds.BusinessSettings,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            existing = await context.BusinessSettings.OrderBy(s => s.Id).FirstAsync();
        }

        return existing;
    }

    public Task SyncFromFeatureFlagsAsync(bool productPricingEnabled, bool updateProductPriceOnPurchase) =>
        SaveAsync(productPricingEnabled, updateProductPriceOnPurchase);

    public Task SaveAsync(bool productPricingEnabled, bool updateProductPriceOnPurchase) =>
        SaveAsync(productPricingEnabled, updateProductPriceOnPurchase, periodLockEnabled: null, lockedThroughDate: null);

    public Task SaveAsync(
        bool productPricingEnabled,
        bool updateProductPriceOnPurchase,
        bool periodLockEnabled,
        DateTime? lockedThroughDate) =>
        SaveAsync(productPricingEnabled, updateProductPriceOnPurchase, (bool?)periodLockEnabled, lockedThroughDate);

    private async Task SaveAsync(
        bool productPricingEnabled,
        bool updateProductPriceOnPurchase,
        bool? periodLockEnabled,
        DateTime? lockedThroughDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.BusinessSettings
            .IgnoreQueryFilters()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(s => !s.IsDeleted);

        if (existing is null)
        {
            context.BusinessSettings.Add(new BusinessSettings
            {
                ProductPricingEnabled = productPricingEnabled,
                UpdateProductPriceOnPurchase = updateProductPriceOnPurchase,
                PeriodLockEnabled = periodLockEnabled ?? false,
                LockedThroughDate = lockedThroughDate?.Date,
                SyncId = AlMuhasib.Sync.ProductPricingSyncIds.BusinessSettings,
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.ProductPricingEnabled = productPricingEnabled;
            existing.UpdateProductPriceOnPurchase = updateProductPriceOnPurchase;
            if (periodLockEnabled.HasValue)
            {
                existing.PeriodLockEnabled = periodLockEnabled.Value;
                existing.LockedThroughDate = existing.PeriodLockEnabled ? lockedThroughDate?.Date : null;
            }
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = _currentUserService.Username;
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
        }

        await context.SaveChangesAsync();

        if (productPricingEnabled)
            await _pricingTypeService.EnsureDefaultExistsAsync();
    }
}
