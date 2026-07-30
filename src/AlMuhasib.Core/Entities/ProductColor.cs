namespace AlMuhasib.Core.Entities;

/// <summary>لون منتج — يُستخدم مع ميزة محلات الألبسة (كتالوج ألوان بدون مخزون منفصل).</summary>
public class ProductColor : BaseEntity
{
    public int ProductId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}
