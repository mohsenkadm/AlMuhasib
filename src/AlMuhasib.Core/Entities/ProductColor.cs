namespace AlMuhasib.Core.Entities;

/// <summary>لون منتج — مشترك مع المنتج على مستوى الشركة.</summary>
public class ProductColor : BaseEntity
{
    public int ProductId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}
