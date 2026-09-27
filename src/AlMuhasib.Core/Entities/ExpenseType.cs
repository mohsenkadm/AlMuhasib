namespace AlMuhasib.Core.Entities;

/// <summary>أنواع المصاريف</summary>
public class ExpenseType : BranchScopedEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Expense> Expenses { get; set; } = [];
}
