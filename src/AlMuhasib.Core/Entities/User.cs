using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>المستخدمون</summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }

    // Navigation
    public ICollection<Permission> Permissions { get; set; } = [];
}
