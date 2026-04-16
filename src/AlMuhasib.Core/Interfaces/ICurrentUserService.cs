using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces;

public interface ICurrentUserService
{
    string Username { get; }
    int? UserId { get; }
    UserRole Role { get; }
    bool IsAdmin { get; }
    bool CanView(string screenName);
    bool CanAdd(string screenName);
    bool CanEdit(string screenName);
    bool CanDelete(string screenName);
    bool CanPrint(string screenName);
    bool CanExport(string screenName);
}
