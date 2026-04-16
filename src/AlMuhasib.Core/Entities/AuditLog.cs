using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>سجل العمليات</summary>
public class AuditLog : BaseEntity
{
    public int UserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
