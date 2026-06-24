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

    public bool CanView(string screenName) =>
        screenName == ScreenPermissionRegistry.Dashboard
        || screenName == HotelPermissionRegistry.Dashboard
        || screenName == ScreenPermissionRegistry.DeveloperSystem
        || screenName == ScreenPermissionRegistry.SystemUpdate
        || (Get(screenName)?.CanView ?? false);

    public bool CanAdd(string screenName) => Get(screenName)?.CanAdd ?? false;

    public bool CanEdit(string screenName) => Get(screenName)?.CanEdit ?? false;

    public bool CanDelete(string screenName) => Get(screenName)?.CanDelete ?? false;

    public bool CanPrint(string screenName) => Get(screenName)?.CanPrint ?? false;

    public bool CanExport(string screenName) => Get(screenName)?.CanExport ?? false;

    public bool CanEditPrice(string screenName) => Get(screenName)?.CanEditPrice ?? false;

    public bool IsViewOnly(string screenName) => Get(screenName)?.IsViewOnly ?? false;
}
