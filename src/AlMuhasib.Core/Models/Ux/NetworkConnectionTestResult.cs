namespace AlMuhasib.Core.Models.Ux;

public class NetworkConnectionTestResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? LatencyMs { get; init; }
    public string? ServerVersion { get; init; }
    public int? MigrationCount { get; init; }

    public static NetworkConnectionTestResult Ok(string message, int? latencyMs = null, string? serverVersion = null, int? migrationCount = null) =>
        new() { Success = true, Message = message, LatencyMs = latencyMs, ServerVersion = serverVersion, MigrationCount = migrationCount };

    public static NetworkConnectionTestResult Fail(string message) =>
        new() { Success = false, Message = message };
}
