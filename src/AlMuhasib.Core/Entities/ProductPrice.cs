namespace AlMuhasib.Core.Entities;

/// <summary>سعر منتج لنوع تسعير محدد (بيع + شراء)</summary>
public class ProductPrice : BaseEntity
{
    public int ProductId { get; set; }
    public int PricingTypeId { get; set; }
    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }

    public Product Product { get; set; } = null!;
    public PricingType PricingType { get; set; } = null!;
}
