using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class UserLoginLogService : IUserLoginLogService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UserLoginLogService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task LogLoginAsync(int userId, string username)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.UserLoginLogs.Add(new Core.Entities.UserLoginLog
        {
            UserId = userId,
            Username = username,
            LoginAt = DateTime.Now,
            MachineName = Environment.MachineName
        });
        await ctx.SaveChangesAsync();
    }

    public async Task LogLogoutAsync(int userId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var log = await ctx.UserLoginLogs
            .Where(l => l.UserId == userId && l.LogoutAt == null)
            .OrderByDescending(l => l.LoginAt)
            .FirstOrDefaultAsync();
        if (log is null) return;
        log.LogoutAt = DateTime.Now;
        await ctx.SaveChangesAsync();
    }
}
