using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>قاعدة عمولة لمندوب (نسبة مبيعات / ربح / ثابت / منتج / عميل)</summary>
public class SalesRepCommissionRule : BaseEntity
{
    public int SalesRepresentativeId { get; set; }

    public SalesRepCommissionType CommissionType { get; set; }

    /// <summary>النسبة المئوية (مثلاً 2 = 2%) أو تُستخدم مع ByProduct/ByCustomer</summary>
    public decimal Percentage { get; set; }

    /// <summary>مبلغ ثابت لكل فاتورة أو لكل وحدة حسب النوع</summary>
    public decimal FixedAmount { get; set; }

    /// <summary>عند النوع ByProduct</summary>
    public int? ProductId { get; set; }

    /// <summary>عند النوع ByCustomer</summary>
    public int? CustomerId { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public SalesRepresentative SalesRepresentative { get; set; } = null!;
    public Product? Product { get; set; }
    public Customer? Customer { get; set; }
}
