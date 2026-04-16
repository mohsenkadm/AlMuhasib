using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>التحويلات</summary>
public class Transfer : BaseEntity
{
    public TransferAccountType FromType { get; set; }
    public int FromId { get; set; }
    public TransferAccountType ToType { get; set; }
    public int ToId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
