using System.Text.Json;
using System.Text.Json.Nodes;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using Microsoft.Data.SqlClient;

namespace AlMuhasib.Infrastructure.Services;

public sealed class AppSettingsConnectionStore : IAppSettingsConnectionStore
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    public string AppSettingsPath { get; }

    public AppSettingsConnectionStore(string? appSettingsPath = null)
    {
        AppSettingsPath = appSettingsPath
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }

    public string BuildDefaultConnectionString(SqlServerInstanceInfo instance, string databaseName = "AlMuhasibDb") =>
        BuildDefaultConnectionString(instance.DataSource, instance.IsLocalDb, databaseName);

    public string BuildDefaultConnectionString(string dataSource, bool isLocalDb, string databaseName = "AlMuhasibDb")
    {
        if (string.IsNullOrWhiteSpace(dataSource))
            throw new ArgumentException("Data source is required.", nameof(dataSource));

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource.Trim(),
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        };

        // LocalDB file attachment is resolved at runtime via LocalDatabasePathResolver
        // when UsesLocalDb(DefaultConnection) is true.
        _ = isLocalDb;
        return builder.ConnectionString;
    }

    public void SaveSelectedInstance(SqlServerInstanceInfo instance, string databaseName = "AlMuhasibDb") =>
        SaveDefaultConnection(BuildDefaultConnectionString(instance, databaseName));

    public void SaveDefaultConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        JsonObject root;
        if (File.Exists(AppSettingsPath))
        {
            var existing = File.ReadAllText(AppSettingsPath);
            root = JsonNode.Parse(string.IsNullOrWhiteSpace(existing) ? "{}" : existing) as JsonObject
                   ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        if (root["Installation"] is not JsonObject)
        {
            root["Installation"] = new JsonObject
            {
                ["DataDirectory"] = string.Empty
            };
        }

        if (root["Updates"] is not JsonObject)
        {
            root["Updates"] = new JsonObject
            {
                ["Enabled"] = true,
                ["ManifestUrl"] = "https://raw.githubusercontent.com/mohsenkadm/AlMuhasib/master/version.json",
                ["CheckOnStartup"] = true,
                ["CheckIntervalHours"] = 6,
                ["DownloadTimeoutMinutes"] = 30
            };
        }

        var connectionStrings = root["ConnectionStrings"] as JsonObject ?? new JsonObject();
        connectionStrings["DefaultConnection"] = connectionString;
        root["ConnectionStrings"] = connectionStrings;

        var directory = Path.GetDirectoryName(AppSettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(AppSettingsPath, root.ToJsonString(WriteOptions));
    }
}
