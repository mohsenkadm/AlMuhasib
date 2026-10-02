namespace AlMuhasib.Core.Entities;

/// <summary>أصناف المنتجات — مشتركة على مستوى الشركة (كل الفروع).</summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Product> Products { get; set; } = [];
}
