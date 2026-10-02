namespace AlMuhasib.Core.Entities;

/// <summary>قياس منتج — مشترك مع المنتج على مستوى الشركة.</summary>
public class ProductSize : BaseEntity
{
    public int ProductId { get; set; }
    public string SizeName { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductSizeStock> Stocks { get; set; } = [];
}
