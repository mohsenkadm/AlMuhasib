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
        // Startup / pre-login seed must not require a user branch session.
        context.BypassBranchFilter = true;

        var existing = await context.BusinessSettings
            .IgnoreQueryFilters()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(s => !s.IsDeleted);

        if (existing is null)
        {
            var mainBranchId = await context.Branches.AsNoTracking()
                .Where(b => b.IsMain && !b.IsDeleted)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();
            if (mainBranchId <= 0)
            {
                mainBranchId = await context.Branches.AsNoTracking()
                    .Where(b => b.Code == Branch.MainBranchCode && !b.IsDeleted)
                    .Select(b => b.Id)
                    .FirstOrDefaultAsync();
            }

            context.BusinessSettings.Add(new BusinessSettings
            {
                BranchId = mainBranchId > 0 ? mainBranchId : 1,
                ProductPricingEnabled = false,
                UpdateProductPriceOnPurchase = false,
                MultiCurrencyEnabled = false,
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

    public Task SyncFromFeatureFlagsAsync(bool productPricingEnabled, bool updateProductPriceOnPurchase, bool multiCurrencyEnabled = false) =>
        SaveAsync(productPricingEnabled, updateProductPriceOnPurchase, multiCurrencyEnabled);

    public Task SaveAsync(bool productPricingEnabled, bool updateProductPriceOnPurchase, bool multiCurrencyEnabled = false) =>
        SaveAsync(productPricingEnabled, updateProductPriceOnPurchase, periodLockEnabled: null, lockedThroughDate: null, multiCurrencyEnabled);

    public Task SaveAsync(
        bool productPricingEnabled,
        bool updateProductPriceOnPurchase,
        bool periodLockEnabled,
        DateTime? lockedThroughDate,
        bool? multiCurrencyEnabled = null) =>
        SaveAsync(productPricingEnabled, updateProductPriceOnPurchase, (bool?)periodLockEnabled, lockedThroughDate, multiCurrencyEnabled);

    private async Task SaveAsync(
        bool productPricingEnabled,
        bool updateProductPriceOnPurchase,
        bool? periodLockEnabled,
        DateTime? lockedThroughDate,
        bool? multiCurrencyEnabled)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // إعدادات الشركة موحّدة عبر الفروع — يجب تجاوز حارس الكتابة وإلا يفشل
        // معالج النقل عند الفرع الحالي ≠ BranchId المسجّل على BusinessSettings.
        context.BypassBranchFilter = true;

        var existing = await context.BusinessSettings
            .IgnoreQueryFilters()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(s => !s.IsDeleted);

        if (existing is null)
        {
            var mainBranchId = await context.Branches.AsNoTracking()
                .Where(b => b.IsMain && !b.IsDeleted)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();
            if (mainBranchId <= 0)
            {
                mainBranchId = await context.Branches.AsNoTracking()
                    .Where(b => b.Code == Branch.MainBranchCode && !b.IsDeleted)
                    .Select(b => b.Id)
                    .FirstOrDefaultAsync();
            }

            context.BusinessSettings.Add(new BusinessSettings
            {
                BranchId = mainBranchId > 0 ? mainBranchId : 1,
                ProductPricingEnabled = productPricingEnabled,
                UpdateProductPriceOnPurchase = updateProductPriceOnPurchase,
                MultiCurrencyEnabled = multiCurrencyEnabled ?? false,
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
            if (multiCurrencyEnabled.HasValue)
                existing.MultiCurrencyEnabled = multiCurrencyEnabled.Value;
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

            // وحّد علم تعدد العملات على كل صفوف الإعدادات إن وُجدت نسخ لكل فرع
            if (multiCurrencyEnabled.HasValue)
            {
                var siblings = await context.BusinessSettings.IgnoreQueryFilters()
                    .Where(s => !s.IsDeleted && s.Id != existing.Id)
                    .ToListAsync();
                foreach (var sibling in siblings)
                    sibling.MultiCurrencyEnabled = multiCurrencyEnabled.Value;
            }
        }

        await context.SaveChangesAsync();

        if (productPricingEnabled)
            await _pricingTypeService.EnsureDefaultExistsAsync();
    }
}
