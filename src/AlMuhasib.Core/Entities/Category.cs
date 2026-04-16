namespace AlMuhasib.Core.Entities;

/// <summary>أصناف المنتجات</summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Product> Products { get; set; } = [];
}
