using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>مهمة شخصية للمستخدم</summary>
public class UserTask : BaseEntity
{
    /// <summary>المستخدم المُسندة إليه المهمة</summary>
    public int UserId { get; set; }
    /// <summary>المستخدم الذي أسند المهمة</summary>
    public int AssignedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public UserTaskStatus Status { get; set; } = UserTaskStatus.Pending;
    public DateTime? DueDate { get; set; }

    public User User { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
}
