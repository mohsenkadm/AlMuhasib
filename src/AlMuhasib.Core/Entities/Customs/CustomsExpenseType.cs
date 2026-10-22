namespace AlMuhasib.Core.Entities.Customs;

public class CustomsExpenseType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<CustomsExpense> Expenses { get; set; } = [];
}
