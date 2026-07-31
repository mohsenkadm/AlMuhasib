using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Services;

public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IUserPreferencesService _preferences;

    public FeatureFlagService(IUserPreferencesService preferences) => _preferences = preferences;

    public BusinessFeatureFlags Current => _preferences.Current.FeatureFlags;

    public bool PurchaseReturns => Current.PurchaseReturns;
    public bool WarehouseTransfers => Current.WarehouseTransfers;
    public bool UnitsOfMeasure => Current.UnitsOfMeasure;
    public bool MenuWeight => Current.MenuWeight;
    public bool ExpiryTracking => Current.ExpiryTracking;
    public bool SerialNumbers => Current.SerialNumbers;
    public bool ProductPricingEnabled => Current.ProductPricingEnabled;
    public bool UpdateProductPriceOnPurchase => Current.UpdateProductPriceOnPurchase;
    public bool ProductDiscountEnabled => Current.ProductDiscountEnabled;
    public bool TransportFees => Current.TransportFees;
    public bool WarehouseInvoiceAndDriver => Current.WarehouseInvoiceAndDriver;
    public bool TemplateMobileShop => Current.TemplateMobileShop;
    public bool TemplateClothing => Current.TemplateClothing;
    public bool TemplateConstruction => Current.TemplateConstruction;
    public bool TemplatePharmacy => Current.TemplatePharmacy;

    public bool AnyMarketTemplateEnabled =>
        TemplateMobileShop || TemplateClothing || TemplateConstruction || TemplatePharmacy;

    public event EventHandler? FlagsChanged;

    public void NotifyFlagsChanged() =>
        FeatureUiRefresh.Invoke(() => FlagsChanged?.Invoke(this, EventArgs.Empty));
}
