using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>المصاريف</summary>
public class Expense : BaseEntity
{
    public int ExpenseTypeId { get; set; }

    /// <summary>عملة المصروف — يجب أن تطابق عملة القاصة.</summary>
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;

    /// <summary>سعر الصرف وقت الحفظ (دولار→دينار).</summary>
    public decimal FxRate { get; set; } = 1m;

    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public int CashBoxId { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ExpenseType ExpenseType { get; set; } = null!;
    public CashBox CashBox { get; set; } = null!;
}
