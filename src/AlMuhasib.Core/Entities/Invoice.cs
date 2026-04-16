using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>الفواتير</summary>
public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public RoundingType RoundingType { get; set; }
    public int CashBoxId { get; set; }
    public DateTime Date { get; set; }
    /// <summary>تاريخ استحقاق التسديد — يُعبأ فقط عند الدفع الآجل</summary>
    public DateTime? CreditDueDate { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
    public Supplier? Supplier { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public CashBox CashBox { get; set; } = null!;
    public ICollection<InvoiceItem> Items { get; set; } = [];
    public ICollection<InstallmentPlan> InstallmentPlans { get; set; } = [];
}
