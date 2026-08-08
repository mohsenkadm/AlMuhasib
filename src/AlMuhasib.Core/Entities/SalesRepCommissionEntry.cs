using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>عمولة محسوبة على فاتورة لمندوب</summary>
public class SalesRepCommissionEntry : BaseEntity
{
    public int SalesRepresentativeId { get; set; }
    public int InvoiceId { get; set; }
    public int? CustomerId { get; set; }

    public DateTime InvoiceDate { get; set; }

    /// <summary>أساس الحساب (مبيعات / ربح / ثابت...)</summary>
    public SalesRepCommissionType CommissionType { get; set; }

    public decimal BaseAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public SalesRepCommissionStatus Status { get; set; } = SalesRepCommissionStatus.Unpaid;

    public string? Notes { get; set; }

    public SalesRepresentative SalesRepresentative { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
    public Customer? Customer { get; set; }

    public decimal UnpaidAmount => Math.Max(0, CommissionAmount - PaidAmount);
}
