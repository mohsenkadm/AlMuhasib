using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public AuditLogService(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

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
        var query = context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        if (action.HasValue) query = query.Where(a => a.Action == action.Value);
        if (!string.IsNullOrEmpty(entityName)) query = query.Where(a => a.EntityName == entityName);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value.Date.AddDays(1));

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogRow
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                Username = a.User.FullName,
                Action = a.Action,
                ActionDisplay = a.Action == AuditAction.Add ? "إضافة"
                    : a.Action == AuditAction.Edit ? "تعديل"
                    : "حذف",
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues
            })
            .ToListAsync();

        return new AuditLogQueryResult
        {
            TotalCount = totalCount,
            Rows = rows
        };
    }

    public async Task<List<string>> GetDistinctEntityNamesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AuditLogs
            .AsNoTracking()
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }
}
