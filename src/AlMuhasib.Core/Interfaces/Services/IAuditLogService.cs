using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IAuditLogService
{
    Task<AuditLogQueryResult> QueryAsync(
        int? userId = null,
        AuditAction? action = null,
        string? entityName = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50);

    Task<List<string>> GetDistinctEntityNamesAsync();
}

public class AuditLogQueryResult
{
    public int TotalCount { get; set; }
    public List<AuditLogRow> Rows { get; set; } = [];
}

public class AuditLogRow
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ActionDisplay { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
