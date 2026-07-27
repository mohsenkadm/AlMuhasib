namespace AlMuhasib.Core.Entities.RealEstate;

public class RealEstateExpense : BaseEntity
{
    public int ExpenseTypeId { get; set; }
    public RealEstateExpenseType ExpenseType { get; set; } = null!;

    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    /// <summary>ربط اختياري بعقد (مثل عمولة أو رسوم متعلقة بعقد محدد).</summary>
    public int? RelatedContractId { get; set; }
    public RealEstateContract? RelatedContract { get; set; }
}
