using System.Diagnostics;
using System.Runtime.Versioning;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace AlMuhasib.Infrastructure.Services;

public sealed class SqlServerInstanceDiscoveryService : ISqlServerInstanceDiscoveryService
{
    public async Task<IReadOnlyList<SqlServerInstanceInfo>> DiscoverLocalInstancesAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, SqlServerInstanceInfo>(StringComparer.OrdinalIgnoreCase);

        void Add(SqlServerInstanceInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.DataSource))
                return;
            results[info.DataSource] = info;
        }

        foreach (var localDb in DiscoverLocalDbInstances())
            Add(localDb);

        if (OperatingSystem.IsWindows())
        {
            foreach (var instance in DiscoverRegistryInstances())
                Add(instance);
        }

        // Common fallbacks when registry/SQL Browser is unavailable.
        Add(new SqlServerInstanceInfo
        {
            DataSource = @"(localdb)\MSSQLLocalDB",
            DisplayName = "(localdb)\\MSSQLLocalDB — SQL LocalDB",
            InstanceName = "MSSQLLocalDB",
            IsLocalDb = true,
            Source = "Fallback"
        });

        Add(new SqlServerInstanceInfo
        {
            DataSource = @".\SQLEXPRESS",
            DisplayName = ".\\SQLEXPRESS — SQL Server Express",
            InstanceName = "SQLEXPRESS",
            Source = "Fallback"
        });

        Add(new SqlServerInstanceInfo
        {
            DataSource = ".",
            DisplayName = ". — SQL Server (المثيل الافتراضي)",
            IsDefaultInstance = true,
            Source = "Fallback"
        });

        Add(new SqlServerInstanceInfo
        {
            DataSource = "localhost",
            DisplayName = "localhost — SQL Server",
            IsDefaultInstance = true,
            Source = "Fallback"
        });

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        // Detected instances first, then LocalDB, then common Express/default fallbacks.
        return results.Values
            .OrderBy(i => i.Source == "Detected" ? 0 : 1)
            .ThenBy(i => i.IsLocalDb ? 0 : 1)
            .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<NetworkConnectionTestResult> TestLocalConnectionAsync(
        string dataSource,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dataSource))
            return NetworkConnectionTestResult.Fail("يرجى اختيار أو إدخال اسم السيرفر.");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource.Trim(),
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 8,
            MultipleActiveResultSets = true
        };

        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            sw.Stop();

            var version = connection.ServerVersion;
            return NetworkConnectionTestResult.Ok(
                $"تم الاتصال بنجاح بـ {dataSource.Trim()} (إصدار {version}).",
                (int)sw.ElapsedMilliseconds,
                version);
        }
        catch (Exception ex)
        {
            return NetworkConnectionTestResult.Fail(
                $"تعذر الاتصال بـ {dataSource.Trim()}: {ex.Message}");
        }
    }

    private static IEnumerable<SqlServerInstanceInfo> DiscoverLocalDbInstances()
    {
        var instances = new List<SqlServerInstanceInfo>();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sqllocaldb",
                Arguments = "info",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
                return instances;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            if (process.ExitCode != 0)
                return instances;

            foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var dataSource = $@"(localdb)\{line}";
                instances.Add(new SqlServerInstanceInfo
                {
                    DataSource = dataSource,
                    DisplayName = $"{dataSource} — SQL LocalDB",
                    InstanceName = line,
                    IsLocalDb = true,
                    Source = "Detected"
                });
            }
        }
        catch
        {
            // LocalDB tools may be missing on some machines.
        }

        return instances;
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<SqlServerInstanceInfo> DiscoverRegistryInstances()
    {
        var instances = new List<SqlServerInstanceInfo>();
        var paths = new[]
        {
            @"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL",
            @"SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL"
        };

        foreach (var path in paths)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(path);
                if (key is null)
                    continue;

                foreach (var valueName in key.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(valueName))
                        continue;

                    var isDefault = string.Equals(valueName, "MSSQLSERVER", StringComparison.OrdinalIgnoreCase);
                    var dataSource = isDefault ? "." : $@".\{valueName}";
                    var label = isDefault
                        ? ". — SQL Server (المثيل الافتراضي)"
                        : $"{dataSource} — SQL Server";

                    instances.Add(new SqlServerInstanceInfo
                    {
                        DataSource = dataSource,
                        DisplayName = label,
                        InstanceName = isDefault ? null : valueName,
                        IsDefaultInstance = isDefault,
                        Source = "Detected"
                    });

                    // Also expose MachineName\Instance for clarity on multi-PC networks.
                    var machineSource = isDefault
                        ? Environment.MachineName
                        : $@"{Environment.MachineName}\{valueName}";
                    instances.Add(new SqlServerInstanceInfo
                    {
                        DataSource = machineSource,
                        DisplayName = $"{machineSource} — SQL Server",
                        InstanceName = isDefault ? null : valueName,
                        IsDefaultInstance = isDefault,
                        Source = "Detected"
                    });
                }
            }
            catch
            {
                // Ignore registry access failures (permissions / non-Windows).
            }
        }

        return instances;
    }

}
