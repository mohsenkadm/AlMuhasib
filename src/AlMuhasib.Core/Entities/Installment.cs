using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>الأقساط الفردية</summary>
public class Installment : BaseEntity
{
    public int InstallmentPlanId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;
    public DateTime? PaymentDate { get; set; }
    public int? CashBoxId { get; set; }

    // Navigation
    public InstallmentPlan InstallmentPlan { get; set; } = null!;
    public CashBox? CashBox { get; set; }
}
