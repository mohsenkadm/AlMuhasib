namespace AlMuhasib.Core.Entities;

/// <summary>أنواع التسعير (سعر مفرد، جملة، وكيل...)</summary>
public class PricingType : BranchScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductPrice> ProductPrices { get; set; } = [];
}
