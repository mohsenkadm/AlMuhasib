using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public BackupService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<string> BackupDatabaseAsync(string destinationPath)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var databaseName = context.Database.GetDbConnection().Database;

        if (string.IsNullOrWhiteSpace(databaseName) || databaseName.Contains('\'') || databaseName.Contains(';'))
            throw new ArgumentException("Invalid database name.");

        if (string.IsNullOrWhiteSpace(destinationPath) || destinationPath.Contains('\'') || destinationPath.Contains(';'))
            throw new ArgumentException("Invalid backup path.");

        // بدون COMPRESSION — غير مدعوم في SQL Server Express
        var sql = $"BACKUP DATABASE [{databaseName}] TO DISK = N'{destinationPath.Replace("'", "''")}' WITH FORMAT, INIT, STATS = 10";

        await context.Database.ExecuteSqlRawAsync(sql);

        return destinationPath;
    }

    public async Task RestoreDatabaseAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود.", backupFilePath);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string not available.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName) || databaseName.Contains('\'') || databaseName.Contains(';'))
            throw new ArgumentException("Invalid database name.");

        if (string.IsNullOrWhiteSpace(backupFilePath) || backupFilePath.Contains('\'') || backupFilePath.Contains(';'))
            throw new ArgumentException("Invalid backup file path.");

        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var setSingleUser = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
        await using (var cmd = new SqlCommand(setSingleUser, connection))
        {
            cmd.CommandTimeout = 120;
            await cmd.ExecuteNonQueryAsync();
        }

        try
        {
            var restoreSql = $"RESTORE DATABASE [{databaseName}] FROM DISK = N'{backupFilePath.Replace("'", "''")}' WITH REPLACE";
            await using var cmd = new SqlCommand(restoreSql, connection);
            cmd.CommandTimeout = 600;
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            try
            {
                var setMultiUser = $"ALTER DATABASE [{databaseName}] SET MULTI_USER";
                await using var cmd = new SqlCommand(setMultiUser, connection);
                cmd.CommandTimeout = 30;
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best effort
            }
        }
    }

    public string GetDefaultBackupDirectory()
    {
        var backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib",
            "Backups");
        if (!Directory.Exists(backupDir))
            Directory.CreateDirectory(backupDir);
        return backupDir;
    }
}
