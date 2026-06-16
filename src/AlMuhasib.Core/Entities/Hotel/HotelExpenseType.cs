namespace AlMuhasib.Core.Entities.Hotel;

public class HotelExpenseType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public ICollection<HotelExpense> Expenses { get; set; } = [];
}
