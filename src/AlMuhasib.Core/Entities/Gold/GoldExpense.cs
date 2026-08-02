using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

public class GoldExpense : BaseEntity
{
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public int ExpenseTypeId { get; set; }
    public GoldExpenseType? ExpenseType { get; set; }
    public decimal Amount { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public int CashBoxId { get; set; }
    public GoldCashBox? CashBox { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int? WarehouseId { get; set; }
    public GoldWarehouse? Warehouse { get; set; }
}
