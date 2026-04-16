using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _context;

    public BackupService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> BackupDatabaseAsync(string destinationPath)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var databaseName = _context.Database.GetDbConnection().Database;

        // Use parameterized approach - SQL Server BACKUP doesn't support parameters for paths,
        // so we validate the inputs to prevent injection
        if (string.IsNullOrWhiteSpace(databaseName) || databaseName.Contains('\'') || databaseName.Contains(';'))
            throw new ArgumentException("Invalid database name.");

        if (string.IsNullOrWhiteSpace(destinationPath) || destinationPath.Contains('\'') || destinationPath.Contains(';'))
            throw new ArgumentException("Invalid backup path.");

        var sql = $"BACKUP DATABASE [{databaseName}] TO DISK = N'{destinationPath}' WITH FORMAT, INIT, COMPRESSION, STATS = 10";

        await _context.Database.ExecuteSqlRawAsync(sql);

        return destinationPath;
    }

    public async Task RestoreDatabaseAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود.", backupFilePath);

        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string not available.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName) || databaseName.Contains('\'') || databaseName.Contains(';'))
            throw new ArgumentException("Invalid database name.");

        if (string.IsNullOrWhiteSpace(backupFilePath) || backupFilePath.Contains('\'') || backupFilePath.Contains(';'))
            throw new ArgumentException("Invalid backup file path.");

        // Switch to master database for restore
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        // Set database to single user mode to disconnect all users
        var setSingleUser = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
        await using (var cmd = new SqlCommand(setSingleUser, connection))
        {
            cmd.CommandTimeout = 120;
            await cmd.ExecuteNonQueryAsync();
        }

        try
        {
            // Restore the database
            var restoreSql = $"RESTORE DATABASE [{databaseName}] FROM DISK = N'{backupFilePath}' WITH REPLACE";
            await using (var cmd = new SqlCommand(restoreSql, connection))
            {
                cmd.CommandTimeout = 600;
                await cmd.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            // Always set back to multi user mode
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
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var backupDir = Path.Combine(appData, "AlMuhasib", "Backups");
        if (!Directory.Exists(backupDir))
            Directory.CreateDirectory(backupDir);
        return backupDir;
    }
}
