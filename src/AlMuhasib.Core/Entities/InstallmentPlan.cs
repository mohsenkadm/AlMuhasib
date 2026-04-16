namespace AlMuhasib.Core.Entities;

/// <summary>خطة الأقساط</summary>
public class InstallmentPlan : BaseEntity
{
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string? FileNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime StartDate { get; set; }

    // Navigation
    public Invoice Invoice { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<Installment> Installments { get; set; } = [];
}
