namespace AlMuhasib.Core.Entities;

/// <summary>سعر منتج لنوع تسعير — مشترك على مستوى الشركة (كل الفروع).</summary>
public class ProductPrice : BaseEntity
{
    public int ProductId { get; set; }
    public int PricingTypeId { get; set; }
    /// <summary>سعر البيع بالدينار.</summary>
    public decimal SalePrice { get; set; }
    /// <summary>سعر البيع بالدولار — صفر يعني غير معرّف لهذه العملة.</summary>
    public decimal SalePriceUsd { get; set; }
    /// <summary>سعر الشراء بالدينار.</summary>
    public decimal PurchasePrice { get; set; }
    /// <summary>سعر الشراء بالدولار — صفر يعني غير معرّف لهذه العملة.</summary>
    public decimal PurchasePriceUsd { get; set; }

    public Product Product { get; set; } = null!;
    public PricingType PricingType { get; set; } = null!;
}
