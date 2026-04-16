namespace AlMuhasib.Core.Entities;

/// <summary>المصاريف</summary>
public class Expense : BaseEntity
{
    public int ExpenseTypeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public int CashBoxId { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ExpenseType ExpenseType { get; set; } = null!;
    public CashBox CashBox { get; set; } = null!;
}
