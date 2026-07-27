namespace AlMuhasib.Core.Entities.RealEstate;

public class RealEstateExpenseType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<RealEstateExpense> Expenses { get; set; } = [];
}
