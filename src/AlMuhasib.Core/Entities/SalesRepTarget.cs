namespace AlMuhasib.Core.Entities;

/// <summary>هدف مبيعات شهري/فترة لمندوب</summary>
public class SalesRepTarget : BaseEntity
{
    public int SalesRepresentativeId { get; set; }

    /// <summary>بداية الفترة (عادة أول الشهر)</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>نهاية الفترة (عادة آخر الشهر)</summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>هدف المبيعات بالمبلغ</summary>
    public decimal TargetAmount { get; set; }

    public string? Notes { get; set; }

    public SalesRepresentative SalesRepresentative { get; set; } = null!;
}
