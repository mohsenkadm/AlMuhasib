using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.Customs;

public class CustomsExpense : BaseEntity
{
    public int InvoiceId { get; set; }
    public CustomsInvoice Invoice { get; set; } = null!;

    public int ExpenseTypeId { get; set; }
    public CustomsExpenseType ExpenseType { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public decimal FxRate { get; set; } = 1m;
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public string Recipient { get; set; } = string.Empty;
}
