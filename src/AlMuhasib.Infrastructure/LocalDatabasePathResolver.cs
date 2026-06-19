using Microsoft.Data.SqlClient;

namespace AlMuhasib.Infrastructure;

/// <summary>
/// Resolves LocalDB file paths next to the application, with fallback to LocalAppData.
/// </summary>
public static class LocalDatabasePathResolver
{
    public static string EnsureDataDirectory()
    {
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
