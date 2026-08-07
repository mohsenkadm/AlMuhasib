using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IUserActivityProfileService
{
    Task<UserActivityProfileInfo?> GetUserInfoAsync(int userId);
    Task<UserActivityStats> GetStatsAsync(string username, DateTime? from, DateTime? to);
    Task<(IReadOnlyList<EntityChangeRow> Items, int TotalCount)> GetInvoiceModificationsAsync(
        string username, DateTime? from, DateTime? to, string? search, int page, int pageSize);
    Task<(IReadOnlyList<UserDeletedActivityRow> Items, int TotalCount)> GetDeletedActivitiesAsync(
        string username, DateTime? from, DateTime? to, string? search, string? entityKind, int page, int pageSize);
    Task<Invoice?> GetInvoiceIncludingDeletedAsync(int invoiceId);

    Task<UserPerformanceResult> GetPerformanceAsync(
        int userId,
        DateTime? from,
        DateTime? to,
        string? search,
        string? entityName,
        AuditAction? action,
        int page,
        int pageSize);
}

public class UserPerformanceResult
{
    public int AddCount { get; set; }
    public int EditCount { get; set; }
    public int DeleteCount { get; set; }
    public int TotalOperations { get; set; }
    public int LoginCount { get; set; }
    public List<UserPerformanceEntityStat> ByEntity { get; set; } = [];
    public int TotalRows { get; set; }
    public List<AuditLogRow> Rows { get; set; } = [];
}

public class UserPerformanceEntityStat
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityDisplay { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UserActivityProfileInfo
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleDisplay { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginMachine { get; set; }
}

public class UserActivityStats
{
    public int InvoiceModificationsCount { get; set; }
    public int DeletedRecordsCount { get; set; }
    public int DeletedInvoicesCount { get; set; }
    public int TotalActivityCount => InvoiceModificationsCount + DeletedRecordsCount;
}

public class UserDeletedActivityRow
{
    public string EntityKind { get; set; } = string.Empty;
    public string EntityKindDisplay { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public DateTime? EntityDate { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string DetailsSummary { get; set; } = string.Empty;
    public InvoiceType? InvoiceType { get; set; }
    public bool IsInvoice => EntityKind == "Invoice";
}
