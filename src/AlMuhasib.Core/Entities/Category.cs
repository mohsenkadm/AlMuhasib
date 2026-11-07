using System.ComponentModel.DataAnnotations.Schema;

namespace AlMuhasib.Core.Entities;

/// <summary>أصناف المنتجات — مشتركة على مستوى الشركة؛ الظهور يُضبط عبر CategoryBranches.</summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>عرض أسماء الفروع المرتبطة (للواجهة فقط — غير مخزّن).</summary>
    [NotMapped]
    public string BranchesDisplay { get; set; } = "—";

    // Navigation
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<CategoryBranch> CategoryBranches { get; set; } = [];
}
