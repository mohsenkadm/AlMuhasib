using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IBusinessSettingsService
{
    Task<BusinessSettings> GetOrCreateAsync();
    Task SaveAsync(bool productPricingEnabled, bool updateProductPriceOnPurchase, bool multiCurrencyEnabled = false);
    Task SaveAsync(
        bool productPricingEnabled,
        bool updateProductPriceOnPurchase,
        bool periodLockEnabled,
        DateTime? lockedThroughDate,
        bool? multiCurrencyEnabled = null);
    Task SyncFromFeatureFlagsAsync(bool productPricingEnabled, bool updateProductPriceOnPurchase, bool multiCurrencyEnabled = false);
}
