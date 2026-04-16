using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<bool> HasPermissionAsync(int userId, string screenName, string action);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task EnsureAdminAccountAsync();

    // User management
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(string username, string password, string fullName, Core.Enums.UserRole role);
    Task UpdateUserAsync(int userId, string fullName, Core.Enums.UserRole role);
    Task ResetPasswordAsync(int userId, string newPassword);
    Task SetUserActiveAsync(int userId, bool isActive);

    // Permissions
    Task<List<Permission>> GetUserPermissionsAsync(int userId);
    Task SaveUserPermissionsAsync(int userId, List<Permission> permissions);
}

public class AuthResult
{
    public bool Success { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public User? User { get; init; }
    public bool MustChangePassword { get; init; }

    public static AuthResult Failed(string message) => new() { Success = false, ErrorMessage = message };
    public static AuthResult Succeeded(User user, bool mustChangePassword = false) =>
        new() { Success = true, User = user, MustChangePassword = mustChangePassword };
}
