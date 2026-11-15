using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AlMuhasib.Admin.Services;

/// <summary>
/// حالة قاعدة بيانات السحابة في لوحة الأدمن.
/// اللوحة تكمل العمل عند غياب القاعدة حتى تبقى شاشة تفعيل سطح المكتب متاحة.
/// </summary>
public sealed class CloudDatabaseStatus
{
    public bool IsAvailable { get; private set; }

    public string Message { get; private set; } =
        "قاعدة البيانات غير متصلة. يمكنك استخدام تفعيل سطح المكتب بدونها.";

    public void MarkAvailable()
    {
        IsAvailable = true;
        Message = "";
    }

    public void MarkUnavailable()
    {
        IsAvailable = false;
        Message = "قاعدة البيانات غير متصلة. يمكنك استخدام تفعيل سطح المكتب بدونها.";
    }

    /// <summary>
    /// يفحص الخادم بسرعة. قاعدة ناقصة على خادم يعمل تُعاد true حتى ينشئها الترحيل.
    /// تعذر الوصول للخادم يعيد false دون انتظار محاولات EF الطويلة.
    /// </summary>
    public static async Task<bool> CanReachServerAsync(string? connectionString, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = 5
        };

        try
        {
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (SqlException ex) when (ex.Number == 4060)
        {
            logger.LogInformation("Cloud database is missing on a reachable server. Migrations will create it.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cloud database server is not reachable. Admin will start without it.");
            return false;
        }
    }
}
