using System.Data;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AlMuhasib.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ISystemProfileService _systemProfile;

    public BackupService(IConfiguration configuration, ISystemProfileService systemProfile)
    {
        _configuration = configuration;
        _systemProfile = systemProfile;
    }

    private string GetActiveConnectionString() =>
        SystemConnectionStrings.Build(_configuration, _systemProfile.ActiveSystem);

    public string GetDefaultBackupDirectory()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib",
            "Backups",
            _systemProfile.ActiveDatabaseName);
        Directory.CreateDirectory(path);
        return path;
    }

    public async Task<string> BackupDatabaseAsync(string destinationPath)
    {
        var connectionString = GetActiveConnectionString();
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Database name not found in connection string.");

        Directory.CreateDirectory(destinationPath);
        var fileName = $"{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var fullPath = Path.Combine(destinationPath, fileName);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = $"BACKUP DATABASE [{databaseName}] TO DISK = @path WITH FORMAT, INIT, NAME = N'AlMuhasib Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";
        await using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 300 };
        cmd.Parameters.AddWithValue("@path", fullPath);
        await cmd.ExecuteNonQueryAsync();

        return fullPath;
    }

    public async Task RestoreDatabaseAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("ملف النسخ الاحتياطي غير موجود.", backupFilePath);

        var connectionString = GetActiveConnectionString();
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Database name not found in connection string.");

        var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync();

        var killSql = $@"
            DECLARE @sql NVARCHAR(MAX) = N'';
            SELECT @sql += N'KILL ' + CAST(session_id AS NVARCHAR(10)) + N';'
            FROM sys.dm_exec_sessions
            WHERE database_id = DB_ID(N'{databaseName}') AND session_id <> @@SPID;
            EXEC sp_executesql @sql;";

        await using (var killCmd = new SqlCommand(killSql, connection) { CommandTimeout = 60 })
            await killCmd.ExecuteNonQueryAsync();

        var restoreSql = $@"
            ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            RESTORE DATABASE [{databaseName}] FROM DISK = @path WITH REPLACE;
            ALTER DATABASE [{databaseName}] SET MULTI_USER;";

        await using var restoreCmd = new SqlCommand(restoreSql, connection) { CommandTimeout = 600 };
        restoreCmd.Parameters.AddWithValue("@path", backupFilePath);
        await restoreCmd.ExecuteNonQueryAsync();
    }
}
