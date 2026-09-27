using AlMuhasib.Core.Interfaces;

namespace AlMuhasib.Core.Entities;

/// <summary>أساس الكيانات المعزولة حسب الفرع داخل قاعدة الشركة الواحدة.</summary>
public abstract class BranchScopedEntity : BaseEntity, IBranchEntity
{
    public int BranchId { get; set; }

    public Branch? Branch { get; set; }
}
