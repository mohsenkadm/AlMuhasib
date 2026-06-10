namespace AlMuhasib.Core.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }

    /// <summary>معرّف المزامنة السحابية — ثابت عبر الأنظمة</summary>
    public Guid SyncId { get; set; } = Guid.NewGuid();

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public byte[] RowVersion { get; set; } = default!;

    public void MarkSoftDeleted(string deletedBy)
    {
        var now = DateTime.UtcNow;
        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }
}
