namespace AlMuhasib.Core.Entities.Hotel;

public class HotelExpense : BaseEntity
{
    public int HotelExpenseTypeId { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public int? HotelCashBoxId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public HotelExpenseType ExpenseType { get; set; } = null!;
    public HotelCashBox? HotelCashBox { get; set; }
}
