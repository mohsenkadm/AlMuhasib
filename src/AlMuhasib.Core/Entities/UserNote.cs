namespace AlMuhasib.Core.Entities;

/// <summary>ملاحظة شخصية للمستخدم</summary>
public class UserNote : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime LastEditedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
