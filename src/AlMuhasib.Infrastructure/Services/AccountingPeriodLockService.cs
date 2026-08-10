using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class AccountingPeriodLockService : IAccountingPeriodLockService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public AccountingPeriodLockService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task EnsureDateAllowedAsync(DateTime documentDate, CancellationToken cancellationToken = default)
    {
        var (isLocked, lockedThrough) = await GetLockInfoAsync(cancellationToken);
        if (!isLocked || lockedThrough is null)
            return;

        if (documentDate.Date <= lockedThrough.Value.Date)
        {
            throw new InvalidOperationException(
                $"الفترة المحاسبية مقفلة حتى {lockedThrough.Value:yyyy/MM/dd}. لا يمكن حفظ مستند بهذا التاريخ.");
        }
    }

    public async Task<(bool IsLocked, DateTime? LockedThroughDate)> GetLockInfoAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await context.BusinessSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null || !settings.PeriodLockEnabled || settings.LockedThroughDate is null)
            return (false, null);

        return (true, settings.LockedThroughDate.Value.Date);
    }
}
