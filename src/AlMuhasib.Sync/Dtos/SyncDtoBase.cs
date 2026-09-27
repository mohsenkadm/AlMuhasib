namespace AlMuhasib.Sync.Dtos;

public abstract class SyncDtoBase
{
    public Guid SyncId { get; set; }

    /// <summary>معرّف مزامنة الفرع — مطلوب لكل بيانات الأعمال بعد Multi-Branch.</summary>
    public Guid BranchSyncId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public byte[]? RowVersion { get; set; }
}
