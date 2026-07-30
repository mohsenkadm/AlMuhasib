namespace AlMuhasib.Core.Entities;

/// <summary>أنواع التعبئة (قطعة، كارتون، متر، لتر...)</summary>
public class PackagingType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductUnit> ProductUnits { get; set; } = [];
}
