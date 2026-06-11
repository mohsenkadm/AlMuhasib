namespace AlMuhasib.Core.Interfaces.Services;

public interface IUserLoginLogService
{
    Task LogLoginAsync(int userId, string username);
    Task LogLogoutAsync(int userId);
}
