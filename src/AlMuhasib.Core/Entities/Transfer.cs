using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>التحويلات</summary>
public class Transfer : BaseEntity
{
    public TransferAccountType FromType { get; set; }
    public int FromId { get; set; }
    public TransferAccountType ToType { get; set; }
    public int ToId { get; set; }

    /// <summary>عملة التحويل — المصدر والوجهة بنفس العملة.</summary>
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;

    /// <summary>سعر الصرف وقت الحفظ (دولار→دينار).</summary>
    public decimal FxRate { get; set; } = 1m;

    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
