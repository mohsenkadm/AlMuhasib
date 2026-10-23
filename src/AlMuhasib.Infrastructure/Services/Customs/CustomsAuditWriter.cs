using System.Text.Json;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Infrastructure.Data.Customs;

namespace AlMuhasib.Infrastructure.Services.Customs;

/// <summary>
/// Semantic customs audit entries (Approve / Cancel / Receipt / Permissions)
/// with filterable <see cref="AuditLog.EntityName"/> values beyond auto SaveChanges diffs.
/// </summary>
public static class CustomsAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static Task WriteAsync(
        CustomsDbContext db,
        AuditAction action,
        string entityName,
        int entityId,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = db.CurrentUserService;
        if (user?.UserId is not int userId)
            return Task.CompletedTask;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = Serialize(oldValues),
            NewValues = Serialize(newValues),
            Timestamp = DateTime.UtcNow,
            CreatedBy = user.Username ?? "System"
        });

        return Task.CompletedTask;
    }

    private static string? Serialize(object? values)
    {
        if (values is null) return null;
        if (values is string s) return s;
        return JsonSerializer.Serialize(values, JsonOptions);
    }
}
