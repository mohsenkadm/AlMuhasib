using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;

namespace AlMuhasib.UI.Services;

public class CurrentUserService : ICurrentUserService
{
    public string Username { get; set; } = "System";
    public int? UserId { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsAdmin => Role == UserRole.Admin;

    private readonly List<Permission> _permissions = [];

    public void SetPermissions(IEnumerable<Permission> permissions)
    {
        _permissions.Clear();
        _permissions.AddRange(permissions);
    }

    public void Clear()
    {
        Username = "System";
        UserId = null;
        Role = UserRole.User;
        _permissions.Clear();
    }

    private Permission? Get(string screenName) =>
        _permissions.FirstOrDefault(p => p.ScreenName == screenName);

    public bool CanView(string screenName) => IsAdmin || (Get(screenName)?.CanView ?? false);
    public bool CanAdd(string screenName) => IsAdmin || (Get(screenName)?.CanAdd ?? false);
    public bool CanEdit(string screenName) => IsAdmin || (Get(screenName)?.CanEdit ?? false);
    public bool CanDelete(string screenName) => IsAdmin || (Get(screenName)?.CanDelete ?? false);
    public bool CanPrint(string screenName) => IsAdmin || (Get(screenName)?.CanPrint ?? false);
    public bool CanExport(string screenName) => IsAdmin || (Get(screenName)?.CanExport ?? false);
}
