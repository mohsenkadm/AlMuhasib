using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IBusinessSettingsService
{
    Task<BusinessSettings> GetOrCreateAsync();
    Task SaveAsync(bool productPricingEnabled, bool updateProductPriceOnPurchase);
    Task SyncFromFeatureFlagsAsync(bool productPricingEnabled, bool updateProductPriceOnPurchase);
}
