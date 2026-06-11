namespace AlMuhasib.Core.Entities;

/// <summary>سجل دخول المستخدمين.</summary>
public class UserLoginLog : BaseEntity
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime LoginAt { get; set; }
    public DateTime? LogoutAt { get; set; }
    public string? MachineName { get; set; }

    public User User { get; set; } = null!;
}
