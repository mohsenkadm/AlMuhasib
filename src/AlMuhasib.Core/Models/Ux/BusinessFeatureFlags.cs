namespace AlMuhasib.Core.Models.Ux;

/// <summary>تفعيل/إلغاء الميزات المحاسبية وقوالب السوق — يُخزَّن في user-preferences.json</summary>
public class BusinessFeatureFlags
{
    public bool PurchaseReturns { get; set; }
    public bool WarehouseTransfers { get; set; }
    public bool UnitsOfMeasure { get; set; }

    /// <summary>وزن المادة على المنتج وكارد وزن الفاتورة — معطّل افتراضياً.</summary>
    public bool MenuWeight { get; set; }

    public bool ExpiryTracking { get; set; }
    public bool SerialNumbers { get; set; }

    /// <summary>عرض سعر المنتجات — معطّل افتراضياً (بدون سعر).</summary>
    public bool ProductPricingEnabled { get; set; }

    /// <summary>تحديث سعر المنتج من فاتورة مشتريات عند الإنشاء — معطّل افتراضياً.</summary>
    public bool UpdateProductPriceOnPurchase { get; set; }

    public bool TemplateMobileShop { get; set; }
    public bool TemplateClothing { get; set; }
    public bool TemplateConstruction { get; set; }
    public bool TemplatePharmacy { get; set; }
}
