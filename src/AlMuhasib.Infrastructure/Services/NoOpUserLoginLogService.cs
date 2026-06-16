using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Infrastructure.Services;

public sealed class NoOpUserLoginLogService : IUserLoginLogService
{
    public Task LogLoginAsync(int userId, string username) => Task.CompletedTask;
    public Task LogLogoutAsync(int userId) => Task.CompletedTask;
}
