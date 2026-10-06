namespace AlMuhasib.Core.Entities;

/// <summary>ربط منتج ↔ فروع يظهر فيها (متعدد إلى متعدد) — يتحكم بظهور المنتج في النظام والفواتير.</summary>
public class ProductBranch
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
