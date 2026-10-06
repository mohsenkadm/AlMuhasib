namespace AlMuhasib.Core.Entities;

/// <summary>فرع تنظيمي داخل الشركة (قاعدة بيانات واحدة — بيانات منفصلة لكل فرع).</summary>
public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsMain { get; set; }

    /// <summary>رمز ثابت للفرع الرئيسي الافتراضي بعد الترقية.</summary>
    public const string MainBranchCode = "MAIN";

    public ICollection<UserBranch> UserBranches { get; set; } = [];
    public ICollection<ProductBranch> ProductBranches { get; set; } = [];
}
