namespace AlMuhasib.Core.Entities;

/// <summary>مرفقات السند</summary>
public class VoucherAttachment : BranchScopedEntity
{
    public int VoucherId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public Voucher Voucher { get; set; } = null!;
}
