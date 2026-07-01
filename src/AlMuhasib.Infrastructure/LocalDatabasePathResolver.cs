using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace AlMuhasib.Infrastructure;

/// <summary>
/// Resolves LocalDB file paths next to the application, with fallback to LocalAppData.
/// </summary>
public static class LocalDatabasePathResolver
{
    private static string? _configuredDataDirectory;

    public static string EnsureDataDirectory()
    {
        var configured = TryGetConfiguredDataDirectory();
        if (!string.IsNullOrWhiteSpace(configured) && TryCreateDirectory(configured))
            return configured;

        var appDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        if (TryCreateDirectory(appDataDir))
            return appDataDir;

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib",
            "Data");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    public static string GetDatabaseFilePath(string databaseName)
    {
        var dataDir = EnsureDataDirectory();
        return Path.Combine(dataDir, $"{databaseName}.mdf");
    }

    public static string BuildLocalDbConnectionString(string databaseName)
    {
        var mdfPath = GetDatabaseFilePath(databaseName);
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = @"(LocalDb)\MSSQLLocalDB",
            InitialCatalog = databaseName,
            AttachDBFilename = mdfPath,
            IntegratedSecurity = true,
            MultipleActiveResultSets = true,
            TrustServerCertificate = true
        };
        return builder.ConnectionString;
    }

    public static bool UsesLocalDb(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.DataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase)
                   || builder.DataSource.Contains("(LocalDb)", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static bool HasAttachDbFilename(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return !string.IsNullOrWhiteSpace(builder.AttachDBFilename);
        }
        catch
        {
            return false;
        }
    }

    private static string? TryGetConfiguredDataDirectory()
    {
        if (_configuredDataDirectory is not null)
            return string.IsNullOrWhiteSpace(_configuredDataDirectory) ? null : _configuredDataDirectory;

        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(settingsPath))
            {
                _configuredDataDirectory = string.Empty;
                return null;
            }

            using var stream = File.OpenRead(settingsPath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.TryGetProperty("Installation", out var installation)
                && installation.TryGetProperty("DataDirectory", out var dataDirectory))
            {
                var value = dataDirectory.GetString()?.Trim();
                _configuredDataDirectory = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch
        {
            // Fall back to default resolution paths.
        }

        _configuredDataDirectory = string.Empty;
        return null;
    }

    private static bool TryCreateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var testFile = Path.Combine(path, $".write-test-{Guid.NewGuid():N}");
            File.WriteAllText(testFile, "ok");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
