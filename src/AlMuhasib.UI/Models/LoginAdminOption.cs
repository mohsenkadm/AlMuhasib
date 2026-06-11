using System.Windows.Media;

namespace AlMuhasib.UI.Models;

public sealed class LoginAdminOption
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Initials { get; init; } = "?";
    public string RoleDisplay { get; init; } = "مستخدم";
    public Color AvatarColorStart { get; init; } = Color.FromRgb(0x15, 0x65, 0xC0);
    public Color AvatarColorEnd { get; init; } = Color.FromRgb(0x42, 0xA5, 0xF5);

    public static LoginAdminOption FromUser(Core.Entities.User user, int colorIndex)
    {
        var (start, end) = AvatarPalette[colorIndex % AvatarPalette.Length];
        return new LoginAdminOption
        {
            Id = user.Id,
            Username = user.Username,
            FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName,
            Initials = BuildInitials(user.FullName, user.Username),
            RoleDisplay = user.Role == Core.Enums.UserRole.Admin ? "مدير" : "مستخدم",
            AvatarColorStart = start,
            AvatarColorEnd = end
        };
    }

    private static string BuildInitials(string fullName, string username)
    {
        var source = string.IsNullOrWhiteSpace(fullName) ? username : fullName.Trim();
        if (string.IsNullOrWhiteSpace(source)) return "?";

        var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();

        return source.Length >= 2
            ? source[..2].ToUpperInvariant()
            : source[0].ToString().ToUpperInvariant();
    }

    private static readonly (Color Start, Color End)[] AvatarPalette =
    [
        (Color.FromRgb(0x15, 0x65, 0xC0), Color.FromRgb(0x42, 0xA5, 0xF5)),
        (Color.FromRgb(0x6A, 0x1B, 0x9A), Color.FromRgb(0xAB, 0x47, 0xBC)),
        (Color.FromRgb(0x00, 0x79, 0x6B), Color.FromRgb(0x26, 0xA6, 0x9A)),
        (Color.FromRgb(0xE6, 0x51, 0x00), Color.FromRgb(0xFF, 0x98, 0x00)),
        (Color.FromRgb(0xC6, 0x28, 0x28), Color.FromRgb(0xEF, 0x53, 0x50)),
        (Color.FromRgb(0x45, 0x5A, 0x64), Color.FromRgb(0x78, 0x90, 0x9C)),
    ];
}
