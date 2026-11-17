namespace AlMuhasib.Core.Entities;

/// <summary>أنواع الإيرادات الأخرى — مستقلة عن المبيعات وسندات القبض.</summary>
public class OtherIncomeType : BranchScopedEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<OtherIncome> Incomes { get; set; } = [];
}
