namespace AlMuhasib.Core.Models;

/// <summary>صف كشف حساب موحّد لصندوق أو مصرف.</summary>
public class AccountStatementEntry
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public decimal Credit { get; set; }
    public decimal Debit { get; set; }
    public decimal RunningBalance { get; set; }
    public string Reference { get; set; } = string.Empty;

    /// <summary>Voucher | Invoice | Expense | Transfer | Installment | Adjustment</summary>
    public string SourceType { get; set; } = string.Empty;
    public int? SourceId { get; set; }
    public int? VoucherId { get; set; }
    public bool IsReconciled { get; set; }
    public bool CanReverse { get; set; }
}

/// <summary>عرض تحويل بأسماء الحسابات.</summary>
public class TransferDisplayItem
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string FromTypeLabel { get; set; } = string.Empty;
    public string ToTypeLabel { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public AlMuhasib.Core.Enums.TransferAccountType FromType { get; set; }
    public AlMuhasib.Core.Enums.TransferAccountType ToType { get; set; }
    public int FromId { get; set; }
    public int ToId { get; set; }
}
