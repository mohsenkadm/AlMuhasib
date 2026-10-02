namespace AlMuhasib.Core.Entities;

/// <summary>أنواع التسعير — مشتركة على مستوى الشركة (كل الفروع).</summary>
public class PricingType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductPrice> ProductPrices { get; set; } = [];
}
