using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.Car;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CarAuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<CarDbContext> _contextFactory;

    public CarAuditLogService(IDbContextFactory<CarDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<AuditLogQueryResult> QueryAsync(
        int? userId = null,
        AuditAction? action = null,
        string? entityName = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);
        if (action.HasValue)
            query = query.Where(a => a.Action == action.Value);
        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);
        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(context.Users.AsNoTracking(), a => a.UserId, u => u.Id, (a, u) => new AuditLogRow
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                Username = u.Username,
                Action = a.Action,
                ActionDisplay = a.Action.ToString(),
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues
            })
            .ToListAsync();

        return new AuditLogQueryResult { TotalCount = total, Rows = rows };
    }

    public async Task<List<string>> GetDistinctEntityNamesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AuditLogs.AsNoTracking()
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }
}
