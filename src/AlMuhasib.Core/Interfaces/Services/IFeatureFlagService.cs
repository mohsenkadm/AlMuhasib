using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

/// <summary>بوابة قراءة ميزات النظام — كل الميزات معطّلة افتراضياً.</summary>
public interface IFeatureFlagService
{
    BusinessFeatureFlags Current { get; }

    bool PurchaseReturns { get; }
    bool WarehouseTransfers { get; }
    bool UnitsOfMeasure { get; }
    bool MenuWeight { get; }
    bool ExpiryTracking { get; }
    bool SerialNumbers { get; }
    bool ProductPricingEnabled { get; }
    bool UpdateProductPriceOnPurchase { get; }
    bool AddMissingProductsOnPurchase { get; }
    bool ProductDiscountEnabled { get; }
    bool LoyaltySystem { get; }
    bool TransportFees { get; }
    bool WarehouseInvoiceAndDriver { get; }
    bool SalesRepresentatives { get; }
    bool TemplateMobileShop { get; }
    bool TemplateClothing { get; }
    bool TemplateConstruction { get; }
    bool TemplatePharmacy { get; }

    /// <summary>أي قالب سوق مفعّل</summary>
    bool AnyMarketTemplateEnabled { get; }

    event EventHandler? FlagsChanged;
    void NotifyFlagsChanged();
}
