using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace AlMuhasib.Infrastructure;

/// <summary>
/// Ensures the LocalDB automatic instance is created and running before SQL connections.
/// No-op for non-LocalDB servers (Express / full SQL) so existing customers are unaffected.
/// </summary>
public static class LocalDbInstanceBootstrapper
{
    private static readonly string[] SqlLocalDbRelativePaths =
    [
        @"Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe",
        @"Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe",
        @"Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe",
        @"Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe",
        @"Microsoft SQL Server\130\Tools\Binn\SqlLocalDB.exe",
        @"Microsoft SQL Server\120\Tools\Binn\SqlLocalDB.exe",
        @"Microsoft SQL Server\110\Tools\Binn\SqlLocalDB.exe"
    ];

    public const string DefaultInstanceName = "MSSQLLocalDB";

    public static bool UsesLocalDb(string? connectionStringOrDataSource)
    {
        if (string.IsNullOrWhiteSpace(connectionStringOrDataSource))
            return false;

        try
        {
            if (connectionStringOrDataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase))
                return true;

            var builder = new SqlConnectionStringBuilder(connectionStringOrDataSource);
            return builder.DataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return connectionStringOrDataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static string? ExtractInstanceName(string? connectionStringOrDataSource)
    {
        if (string.IsNullOrWhiteSpace(connectionStringOrDataSource))
            return DefaultInstanceName;

        try
        {
            var dataSource = connectionStringOrDataSource.Contains('=', StringComparison.Ordinal)
                ? new SqlConnectionStringBuilder(connectionStringOrDataSource).DataSource
                : connectionStringOrDataSource;

            var trimmed = dataSource.Trim();
            var slash = trimmed.LastIndexOf('\\');
            if (slash >= 0 && slash < trimmed.Length - 1)
                return trimmed[(slash + 1)..].Trim();
        }
        catch
        {
            // fall through
        }

        return DefaultInstanceName;
    }

    /// <summary>
    /// Creates (if needed) and starts the LocalDB instance. Safe to call repeatedly.
    /// </summary>
    public static LocalDbBootstrapResult EnsureRunning(string? connectionStringOrDataSource = null)
    {
        if (!UsesLocalDb(connectionStringOrDataSource ?? $@"(localdb)\{DefaultInstanceName}"))
        {
            return LocalDbBootstrapResult.Skip("not-localdb");
        }

        var instance = ExtractInstanceName(connectionStringOrDataSource) ?? DefaultInstanceName;
        var exe2017 = FindSqlLocalDbExeAtMajor(140);
        var exe = exe2017 ?? FindSqlLocalDbExe();
        if (exe is null)
        {
            return LocalDbBootstrapResult.Failed(
                "لم يتم العثور على SqlLocalDB.exe. ثبّت SQL Server 2017 LocalDB من مثبت قيد، ثم أعد تشغيل الجهاز.");
        }

        // Prefer SQL 2017 (14.0) for brand-new data folders when 2017 tools exist.
        // Existing customers with LocalDB 2022 + database files keep their instance as-is.
        // Never force 14.0 when only newer tools exist.
        var prefer2017Create = exe2017 is not null
                               && string.Equals(instance, DefaultInstanceName, StringComparison.OrdinalIgnoreCase)
                               && !HasExistingAppDatabaseFiles();
        var activeExe = prefer2017Create ? exe2017! : (exe2017 ?? exe);
        var createArgs = prefer2017Create
            ? $"create \"{instance}\" 14.0"
            : $"create \"{instance}\"";

        // create is idempotent when instance already exists (non-zero exit is OK).
        RunSqlLocalDb(activeExe, createArgs, out var createOutput, out var createCode);
        RunSqlLocalDb(activeExe, $"start \"{instance}\"", out var startOutput, out var startCode);

        if (startCode == 0 || IsAlreadyRunning(startOutput))
        {
            return LocalDbBootstrapResult.Ok(activeExe, instance, createCode, startCode);
        }

        // Soft recovery for brand-new machines only: recreate automatic instance when start fails
        // and no application database files exist yet. Prefer SQL 2017 (14.0) when available.
        // Never delete customer .mdf files — only recreate the empty automatic instance.
        if (string.Equals(instance, DefaultInstanceName, StringComparison.OrdinalIgnoreCase)
            && !HasExistingAppDatabaseFiles())
        {
            var recoveryExe = prefer2017Create ? exe2017! : (exe2017 ?? exe);
            var recoveryCreate = prefer2017Create
                ? $"create \"{instance}\" 14.0"
                : $"create \"{instance}\"";
            RunSqlLocalDb(recoveryExe, $"stop \"{instance}\" -k", out _, out _);
            RunSqlLocalDb(recoveryExe, $"delete \"{instance}\"", out _, out _);
            RunSqlLocalDb(recoveryExe, recoveryCreate, out createOutput, out createCode);
            RunSqlLocalDb(recoveryExe, $"start \"{instance}\"", out startOutput, out startCode);
            if (startCode == 0 || IsAlreadyRunning(startOutput))
                return LocalDbBootstrapResult.Ok(recoveryExe, instance, createCode, startCode);
        }

        var detail = string.IsNullOrWhiteSpace(startOutput) ? createOutput : startOutput;
        return LocalDbBootstrapResult.Failed(
            $"تعذر تشغيل مثيل LocalDB '{instance}' (create={createCode}, start={startCode}). {detail}".Trim(),
            activeExe,
            instance,
            createCode,
            startCode);
    }

    public static async Task<bool> TryOpenAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasExistingAppDatabaseFiles()
    {
        try
        {
            var dataDir = LocalDatabasePathResolver.EnsureDataDirectory();
            return Directory.EnumerateFiles(dataDir, "*.mdf", SearchOption.TopDirectoryOnly).Any();
        }
        catch
        {
            return true; // be conservative — do not recreate instance
        }
    }

    private static bool IsAlreadyRunning(string output) =>
        output.Contains("already running", StringComparison.OrdinalIgnoreCase)
        || output.Contains("يعمل بالفعل", StringComparison.OrdinalIgnoreCase);

    public static string? FindSqlLocalDbExeAtMajor(int majorFolder)
    {
        var relative = $@"Microsoft SQL Server\{majorFolder}\Tools\Binn\SqlLocalDB.exe";
        foreach (var root in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                 })
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;
            var full = Path.Combine(root, relative);
            if (File.Exists(full))
                return full;
        }

        return null;
    }

    public static string? FindSqlLocalDbExe()
    {
        foreach (var root in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                 })
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;

            foreach (var relative in SqlLocalDbRelativePaths)
            {
                var full = Path.Combine(root, relative);
                if (File.Exists(full))
                    return full;
            }
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "sqllocaldb.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null)
                return null;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            var first = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first) && File.Exists(first))
                return first;
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static void RunSqlLocalDb(string exe, string arguments, out string output, out int exitCode)
    {
        output = string.Empty;
        exitCode = -1;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null)
                return;

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(15000);
            exitCode = process.ExitCode;
            output = string.Join(Environment.NewLine, new[] { stdout, stderr }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }
        catch (Exception ex)
        {
            output = ex.Message;
            exitCode = -1;
        }
    }
}

public sealed record LocalDbBootstrapResult(
    bool Success,
    bool WasSkipped,
    string? Message,
    string? ExePath,
    string? InstanceName,
    int CreateExitCode,
    int StartExitCode)
{
    public static LocalDbBootstrapResult Ok(string exe, string instance, int createCode, int startCode) =>
        new(true, false, null, exe, instance, createCode, startCode);

    public static LocalDbBootstrapResult Skip(string reason) =>
        new(true, true, reason, null, null, 0, 0);

    public static LocalDbBootstrapResult Failed(
        string message,
        string? exe = null,
        string? instance = null,
        int createCode = -1,
        int startCode = -1) =>
        new(false, false, message, exe, instance, createCode, startCode);
}
