namespace AlMuhasib.Core.Entities;

/// <summary>إعدادات عمل قابلة للمزامنة (صف واحد — Id = 1).</summary>
public class BusinessSettings : BaseEntity
{
    public const int SingletonId = 1;

    /// <summary>عرض السعر — عند التعطيل يبقى النظام بدون سعر كما سابقاً.</summary>
    public bool ProductPricingEnabled { get; set; }

    /// <summary>تحديث سعر الشراء من فاتورة مشتريات عند الإنشاء فقط.</summary>
    public bool UpdateProductPriceOnPurchase { get; set; }
}
