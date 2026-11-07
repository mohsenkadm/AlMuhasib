namespace AlMuhasib.Core.Entities;

/// <summary>ربط صنف ↔ فروع يظهر فيها (متعدد إلى متعدد) — يتحكم بظهور الصنف في النظام والكاشير.</summary>
public class CategoryBranch
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int BranchId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Category Category { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
