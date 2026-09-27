namespace AlMuhasib.Core.Entities;

/// <summary>ربط مستخدم ↔ فروع مسموحة (متعدد إلى متعدد).</summary>
public class UserBranch
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BranchId { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
