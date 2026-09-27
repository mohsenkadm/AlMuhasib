namespace AlMuhasib.Core.Entities;

/// <summary>أصناف المنتجات</summary>
public class Category : BranchScopedEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Product> Products { get; set; } = [];
}
